using CardTD.Utilities;
using ECSTest.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using static AllEnums;
using Random = Unity.Mathematics.Random;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(MovingSystem))]
    public partial struct TargetingSystemBase : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CreepsLocator>();
            state.RequireForUpdate<RandomComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();

            new TargetingJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                EntityCommandBuffer = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                CreepsLocator = SystemAPI.GetSingleton<CreepsLocator>(),
                ObstaclesLocator = SystemAPI.GetSingleton<ObstaclesLocator>(),
                RandomComponent = SystemAPI.GetSingletonRW<RandomComponent>().ValueRW,
            }.ScheduleParallel();
        }


        [BurstCompile(CompileSynchronously = true)]
        public partial struct TargetingJob : IJobEntity
        {
            public const int ShootIndex = 10000;
            private const int projectileIndex = 20000;
            private const float twinBarrelWidthOffset = .1f;

            [NativeDisableParallelForRestriction] public CreepsLocator CreepsLocator;
            [NativeDisableParallelForRestriction] public ObstaclesLocator ObstaclesLocator;
            internal EntityCommandBuffer.ParallelWriter EntityCommandBuffer;
            public float DeltaTime;
            public RandomComponent RandomComponent;
            private void Execute(ref AttackerComponent attackerComponent, ref PositionComponent towerPosition, ref AttackerStatisticComponent statistics, in GunStatsComponent gunStats, in PowerableComponent power,in DestroyComponent destroyComponent,
                [ChunkIndexInQuery] int chunkIndex, Entity attackerEntity)
            {
                if(destroyComponent.IsNeedToDestroy)
                    return;
                
                if (!power.IsTurnedOn)
                    return;

                int sortKey = chunkIndex;

                float range = attackerComponent.AttackStats.AimingStats.Range;
                float rotationSpeed = attackerComponent.AttackStats.AimingStats.RotationSpeed;
                float attackAngle = attackerComponent.AttackStats.AimingStats.AttackAngle * math.TORADIANS;

                float2 targetPos = float2.zero;

                attackerComponent.CurrentDeviation = gunStats.Deviation + gunStats.Control * (attackerComponent.CurrentDeviation - gunStats.Deviation);
                attackerComponent.CurrentDeviation = math.clamp(attackerComponent.CurrentDeviation, gunStats.Deviation, gunStats.Deviation * 2);

                bool shouldAttack = true;
                if (!IsOnBurstAttack(attackerComponent))
                {
                    if (!CreepsLocator.CreepHashMap.TryGetValue(attackerComponent.Target, out CreepInfo creep) || !CanAttack(towerPosition.Position, creep, range))
                    {
                        attackerComponent.Target = Entity.Null;
                        NativeList<CreepInfo> creeps = new(Allocator.Temp);
                        CreepsLocator.LocateNearestCreeps(towerPosition.Position, range, ref creeps,20);
                        //TODO: get only if can attack
                        int creepIndex = -1;
                        float minRotation = 3.14f;
                        float minDistance = range;

                        for (int i = 0; i < creeps.Length; i++)
                        {
                            if (!HasLineOfSight(creeps[i], towerPosition.Position))
                                continue;

                            float angle = math.abs(Utilities.SignedAngleBetween(towerPosition.Direction, creeps[i].Position - towerPosition.Position));

                            if (minRotation < angle)
                                continue;

                            float distance = math.length(creeps[i].Position - towerPosition.Position);

                            if (minDistance < distance)
                                continue;

                            minRotation = angle;
                            minDistance = distance;
                            creepIndex = i;
                        }

                        if (creepIndex > -1)
                        {
                            attackerComponent.Target = creeps[creepIndex].Entity;
                            targetPos = CalculateTargetAhead(creeps[creepIndex].Position, creeps[creepIndex].Velocity, attackerComponent.AttackStats.ProjectileSpeed, towerPosition.Position);
                        }

                        creeps.Dispose();
                    }
                    else
                        targetPos = CalculateTargetAhead(creep.Position, creep.Velocity, attackerComponent.AttackStats.ProjectileSpeed, towerPosition.Position);

                    if (attackerComponent.Target != Entity.Null && attackerComponent.Bullets > 0 && attackerComponent.ReloadTimer <= 0)
                        RotateToTarget(targetPos, DeltaTime, ref towerPosition, rotationSpeed);

                    shouldAttack = ShouldAttack(attackerComponent, towerPosition, attackAngle, targetPos);
                }

                float currentDeltaTime = DeltaTime;

                while (currentDeltaTime > 0)
                {
                    if (RunTimers(CreepsLocator, ref attackerComponent, ref towerPosition, ref currentDeltaTime, shouldAttack))
                    {
                        attackerComponent.IdleTimer += currentDeltaTime;
                        break;
                    }

                    //Check AimingAngle
                    attackerComponent.Bullets--;
                    Shoot(ref attackerComponent, attackerEntity, towerPosition, gunStats, currentDeltaTime, sortKey);
                    statistics.Shoots++;

                    ResetTimers(ref attackerComponent);
                }
            }

            private bool CanAttack(float2 towerPosition, CreepInfo creepInfo, float range)
            {
                float distanceSq = math.distancesq(towerPosition, creepInfo.Position);
                if (range * range < distanceSq)
                    return false;
                return HasLineOfSight(creepInfo, towerPosition);
            }

            private bool HasLineOfSight(CreepInfo creepInfo, float2 towerPosition)
            {
                bool hasCollision = ObstaclesLocator.RaycastObstacle(towerPosition, creepInfo.Position, out _, default);
                return !hasCollision;
            }

            private void Shoot(ref AttackerComponent attacker, Entity attackerEntity, PositionComponent positionComponent,
                GunStatsComponent gunStats, float currentDeltaTime, int sortIndex)
            {
                int createProjectileSortIndex = sortIndex + projectileIndex;

                float2 shootPosition = positionComponent.Position + (positionComponent.Direction * attacker.StartOffset);
                DynamicBuffer<EntitiesBuffer> dynamicBuffer = CreateShootEvent(EntityCommandBuffer, attacker, attackerEntity, sortIndex, positionComponent.Direction, shootPosition); 

                Random random = RandomComponent.GetRandom(JobsUtility.ThreadIndex);

                bool randomizeSpawnPosition = attacker.AttackStats.ShootingStats.ProjectilesPerShot > 2;

                for (int i = 0; i < attacker.AttackStats.ShootingStats.ProjectilesPerShot; i++)
                {
                    var startPoint = positionComponent.Position;
                    if (attacker.TowerType == TowerId.TwinGun)
                        startPoint += i % 2 == 0
                            ? positionComponent.Direction.GetNormal() * twinBarrelWidthOffset
                            : positionComponent.Direction.GetNormal() * -twinBarrelWidthOffset;

                    PositionComponent projectilePosition = GetStartPosition(ref random, startPoint, positionComponent.Direction, attacker.CurrentDeviation);

                    CreateProjectile(createProjectileSortIndex, EntityCommandBuffer, projectilePosition, dynamicBuffer,attacker.TowerType, out Entity projectile);

                    EntityCommandBuffer.AddComponent(createProjectileSortIndex, projectile, new ProjectileFlyComponent { LastEmitDistance = 0 });

                    EntityCommandBuffer.SetName(createProjectileSortIndex, projectile, $"{nameof(Projectile)}");
                    float extraFlyTime = currentDeltaTime;
                    if (randomizeSpawnPosition)
                    {
                        Utilities.GetGaussian(ref random, 0, 1 / 30f, out float reuslt1, out _);
                        extraFlyTime += reuslt1;
                    }

                    bool isLastBullet = attacker.Bullets == 0;
                    EntityCommandBuffer.AddComponent(createProjectileSortIndex, projectile, new ProjectileComponent(attackerEntity, attacker, gunStats, extraFlyTime, isLastBullet, false,projectilePosition.Position));
                    attacker.CurrentDeviation += gunStats.Recoil;
                    createProjectileSortIndex += 1;
                }

                RandomComponent.SetRandom(random, JobsUtility.ThreadIndex);
            }
        }

        public static PositionComponent GetStartPosition(ref Random random, float2 startPoint, float2 dir, float deviation)
        {
            Utilities.GetGaussian(ref random, 0, deviation / 2, out float randomAngle, out _);
            dir = dir.GetRotated(randomAngle * math.TORADIANS);
            return new PositionComponent() { Position = startPoint, Direction = dir };
        }
        public static float2 CalculateTargetAhead(float2 currentTargetPosition, float2 targetVelocity, float projectileSpeed, float2 attackerPos)
        {
            float projSpeed = projectileSpeed;
            float distance = math.distance(attackerPos, currentTargetPosition);
            float eta = projSpeed != 0 ? distance / projSpeed : 0;

            return currentTargetPosition + targetVelocity * eta;
        }
        
        public static bool IsOnBurstAttack(AttackerComponent attackerComponent) => attackerComponent.AttackPattern == AttackPattern.Burst && attackerComponent.ShootTimer != 0;

        public static void CreateProjectile(int sortIndex, EntityCommandBuffer.ParallelWriter ecb, PositionComponent positionComponent, DynamicBuffer<EntitiesBuffer> dynamicBuffer,
            TowerId towerType,out Entity projectile)
        {
            projectile = ecb.CreateEntity(sortIndex);
            ecb.AddComponent(sortIndex, projectile, positionComponent);
            ecb.AddComponent(sortIndex, projectile, new DestroyComponent { IsNeedToDestroy = false });
            dynamicBuffer.Add(projectile);
            
            if (towerType == TowerId.Laser)
                ecb.AddBuffer<Float2Buffer>(sortIndex, projectile);
        }

        public static void CreateProjectile(EntityCommandBuffer ecb, PositionComponent positionComponent,TowerId towerType, out Entity projectile)
        {
            projectile = ecb.CreateEntity();
            ecb.AddComponent(projectile, positionComponent);
            ecb.AddComponent(projectile, new DestroyComponent { IsNeedToDestroy = false });
            if (towerType == TowerId.Laser)
                ecb.AddBuffer<Float2Buffer>(projectile);
        }

        public static void RotateToTarget(float2 targetPosition, float deltaTime, ref PositionComponent positionComponent, float rotationSpeed)
        {
            //TODO: simplify

            Quaternion from = Utilities.Direction2DToQuaternion(positionComponent.Direction);
            float2 forward = targetPosition - positionComponent.Position;
            Quaternion to = Utilities.Direction2DToQuaternion(forward);
            Vector3 newDirection = Quaternion.RotateTowards(from, to, rotationSpeed * deltaTime) * Vector3.forward;

            positionComponent.Direction = new float2(newDirection.x, newDirection.y);
        }

        public static void ResetTimers(ref AttackerComponent attackerComponent)
        {
            switch (attackerComponent.AttackPattern)
            {
                case AttackPattern.Single:
                    attackerComponent.BurstTimer += attackerComponent.AttackStats.ShootingStats.BurstDelay;
                    break;
                case AttackPattern.Burst:
                    attackerComponent.BulletLeftInCurrentBurst--;
                    if (attackerComponent.BulletLeftInCurrentBurst <= 0)
                    {
                        attackerComponent.BurstTimer += attackerComponent.AttackStats.ShootingStats.BurstDelay;
                        attackerComponent.BulletLeftInCurrentBurst = attackerComponent.AttackStats.ShootingStats.ShotsPerBurst;
                    }
                    else
                    {
                        attackerComponent.ShootTimer += attackerComponent.AttackStats.ShootingStats.ShotDelay;
                    }

                    break;
                case AttackPattern.Auto:
                    attackerComponent.ShootTimer += attackerComponent.AttackStats.ShootingStats.ShotDelay;
                    break;
                default:
                    throw new System.Exception("Invalid AttackPattern");
            }
            attackerComponent.IdleTimer = 0;
        }

        public static DynamicBuffer<EntitiesBuffer> CreateShootEvent(EntityCommandBuffer.ParallelWriter entityCommandBuffer, AttackerComponent attacker, Entity attackerEntity, int shootSortIndex,
            float2 shootDirection,
            float2 shootPosition)
        {
            shootSortIndex += TargetingJob.ShootIndex;
            Entity shootEntity = entityCommandBuffer.CreateEntity(shootSortIndex);
            entityCommandBuffer.SetName(shootSortIndex, shootEntity, "Shoot");
            MuzzleTimedEvent shootEvent = new() { TowerId = attacker.TowerType, Tower = attackerEntity, Direction = shootDirection, Position = shootPosition, IsEnhanced = false, CurrentFrame = 0, AnimationTimer = 0};
            entityCommandBuffer.AddComponent(shootSortIndex, shootEntity, shootEvent);
			
            return entityCommandBuffer.AddBuffer<EntitiesBuffer>(shootSortIndex, shootEntity);
        }

        /// <summary>
        /// Run all timers
        /// </summary>
        /// <returns>true if at least one timer != 0</returns>
        public static bool RunTimers(CreepsLocator creepsLocator, ref AttackerComponent attackerComponent
            , ref PositionComponent positionComponent, ref float currentDeltaTime, bool shouldAttack)
        {
            if (attackerComponent.Bullets <= 0)
                return true;

            attackerComponent.ReloadTimer = RunTimer(attackerComponent.ReloadTimer, out bool timerFinished, ref currentDeltaTime);
            if (!timerFinished)
                return true;

            attackerComponent.BurstTimer = RunTimer(attackerComponent.BurstTimer, out timerFinished, ref currentDeltaTime);
            if (!timerFinished)
                return true;

            attackerComponent.ShootTimer = RunTimer(attackerComponent.ShootTimer, out timerFinished, ref currentDeltaTime);
            if (!timerFinished)
                return true;

            // if burst => we should finish shooting the bullets even if no Target
            if (attackerComponent.AttackPattern != AttackPattern.Burst && !creepsLocator.HasEntity(attackerComponent.Target))
                return true;

            if (attackerComponent.Target != Entity.Null)
            {
                attackerComponent.WindUpTimer = RunTimer(attackerComponent.WindUpTimer, out timerFinished, ref currentDeltaTime);
                if (!timerFinished)
                    return true;
            }
            else
            {
                if (attackerComponent.IdleTimer >= attackerComponent.AttackStats.ShootingStats.WindUpTime)
                    attackerComponent.WindUpTimer = attackerComponent.AttackStats.ShootingStats.WindUpTime;
            }

            //We should break outer loop if we should not attack (otherwise infinite loop)
            return !shouldAttack;
        }

        public static bool WithinAttackAngle(float2 dir, float2 dirToTarget, float maxAngle)
        {
            float angle = math.abs(Utilities.SignedAngleBetween(dir, dirToTarget));
            return angle < maxAngle;
        }

        public static bool ShouldAttack(AttackerComponent attackerComponent, PositionComponent towerPosition, float attackAngle, float2 targetPos)
        {
            return attackerComponent.Target != Entity.Null
                   && attackerComponent.AttackPattern != AttackPattern.Off
                   && WithinAttackAngle(towerPosition.Direction, targetPos - towerPosition.Position, attackAngle);
        }

        private static float RunTimer(float timer, out bool timerFinished, ref float currentDeltaTime)
        {
            if (timer <= 0)
            {
                timerFinished = true;
                return 0;
            }

            if (timer <= currentDeltaTime)
            {
                currentDeltaTime -= timer;
                timerFinished = true;
                return 0;
            }

            //timer -= currentDeltaTime;
            timerFinished = false;
            return timer - currentDeltaTime;
        }

        public static bool WithinAttackRange(float2 creepPos, float2 position, float minRange, float maxRange)
        {
            float sqrDistance = math.lengthsq(creepPos - position);
            return (minRange * minRange) < sqrDistance && sqrDistance < maxRange * maxRange;
        }
    }
}