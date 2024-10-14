using CardTD.Utilities;
using ECSTest.Components;
using ECSTest.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using static ECSTest.Systems.TargetingSystemBase;
using static AllEnums;

[UpdateAfter(typeof(MovingSystem))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct RocketTargetingSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<RandomComponent>();
    }

    public struct ShootData
    {
        public Entity Tower;
        public float2 Origin;
        public float2 Direction;
        public float Damage;
        public float AOE;
        public float ProjectileSpeed;
        public float InitialFlyTime;
    }


    private const float offsetDistance = 15f;
    private const float offsetRotationAngle = 40 * math.TORADIANS;

    private const float minRange = 4;
    private const float barrelWidth = .4f;


    public static float MinRocketRange(float range) => minRange;


    [BurstCompile(CompileSynchronously = true)]
    private void OnUpdate(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        new RocketTargetingJob
        {
            RandomComponent = SystemAPI.GetSingleton<RandomComponent>(),
            CreepsLocator = SystemAPI.GetSingleton<CreepsLocator>(),
            ECB = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.ScheduleParallel();
    }

    [BurstCompile(CompileSynchronously = true)]
    private partial struct RocketTargetingJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public CreepsLocator CreepsLocator;
        public RandomComponent RandomComponent;
        public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;

        // private const int projectileIndex = 40000;

        private void Execute(ref AttackerComponent attackerComponent, ref PositionComponent towerPosition, ref AttackerStatisticComponent statistics, ref RocketStatsComponent rocketStats, in PowerableComponent power,DestroyComponent destroyComponent,
            [ChunkIndexInQuery] int chunkIndex, Entity thisEntity)
        {
            if(destroyComponent.IsNeedToDestroy)
                return;
            
            if (!power.IsTurnedOn)
                return;

            // If we are ready to shoot => check if we have Targets in our attack Range=> if yes then shoot

            NativeList<CreepInfo> possibleTargets = new(Allocator.Temp);

            float range = attackerComponent.AttackStats.AimingStats.Range;
            float rotationSpeed = attackerComponent.AttackStats.AimingStats.RotationSpeed;
            float attackAngle = attackerComponent.AttackStats.AimingStats.AttackAngle * math.TORADIANS;

            float2 targetPos = float2.zero;

            bool shouldAttack = true;
            if (!IsOnBurstAttack(attackerComponent))
            {
                if (!CreepsLocator.CreepHashMap.TryGetValue(attackerComponent.Target, out CreepInfo creep) || !CanAttack(creep.Position, towerPosition.Position, range))
                {
                    attackerComponent.Target = Entity.Null;

                    CreepsLocator.LocateNearestCreeps(towerPosition.Position, range, ref possibleTargets, default,20);
                    float minRangeSqr = MinRocketRange(range) * MinRocketRange(range);
                    for (int i = 0; i < possibleTargets.Length; i++)
                    {
                        if (minRangeSqr > math.distancesq(towerPosition.Position, possibleTargets[i].Position))
                        {
                            possibleTargets.RemoveAt(i);
                            i--;
                        }
                    }

                    if (possibleTargets.Length > 0)
                    {
                        //TODO: Pick best Target(lowest angle)
                        attackerComponent.Target = possibleTargets[0].Entity;
                        targetPos = possibleTargets[0].Position;
                    }
                }
                else
                    targetPos = creep.Position;

                rocketStats.LastTargetPosition = targetPos;

                if (attackerComponent.Target != Entity.Null && attackerComponent.Bullets > 0 && attackerComponent.ReloadTimer <= 0)
                    RotateToTarget(targetPos, DeltaTime, ref towerPosition, rotationSpeed);

                shouldAttack = ShouldAttack(attackerComponent, towerPosition, attackAngle, targetPos);
            }

            bool filteredCreeps = false;
            float currentDeltaTime = DeltaTime;

            while (currentDeltaTime > 0)
            {
                if (RunTimers(CreepsLocator, ref attackerComponent, ref towerPosition, ref currentDeltaTime, shouldAttack))
                {
                    attackerComponent.IdleTimer += currentDeltaTime;
                    break;
                }

                if (shouldAttack)
                {
                    if (possibleTargets.Length == 0)
                        CreepsLocator.LocateNearestCreeps(towerPosition.Position, range, ref possibleTargets, default,20);

                    if (!filteredCreeps)
                    {
                        float minRangeSqr = MinRocketRange(range) * MinRocketRange(range);
                        for (int i = 0; i < possibleTargets.Length; i++)
                        {
                            if (minRangeSqr > math.distancesq(towerPosition.Position, possibleTargets[i].Position)
                                || !WithinAttackAngle(towerPosition.Direction, possibleTargets[i].Position - towerPosition.Position, attackAngle))
                            {
                                possibleTargets.RemoveAt(i);
                                i--;
                                continue;
                            }
                        }

                        filteredCreeps = true;
                    }

                    if (possibleTargets.Length == 0)
                    {
                        CreepInfo lastCreepInfo = new() {Position = rocketStats.LastTargetPosition, Velocity = rocketStats.LastTargetVelocity};
                        possibleTargets.Add(lastCreepInfo);
                    }

                    attackerComponent.Bullets--;

                    Random random = RandomComponent.GetRandom(JobsUtility.ThreadIndex);
                    int targetIndex = random.NextInt(0, possibleTargets.Length);

                    int sortKey = chunkIndex;

                    ShootData shootData = new();
                    shootData.Tower = thisEntity;

                    rocketStats.LastTargetPosition = possibleTargets[targetIndex].Position;
                    rocketStats.LastTargetVelocity = possibleTargets[targetIndex].Velocity;
                    
                    var targetAheadPosition = CalculateTargetAhead(possibleTargets[targetIndex].Position, possibleTargets[targetIndex].Velocity, attackerComponent.AttackStats.ProjectileSpeed, towerPosition.Position);

                    shootData.Direction = math.normalize(targetAheadPosition - towerPosition.Position);
                    shootData.Origin = towerPosition.Position + towerPosition.Direction * attackerComponent.StartOffset;
                    shootData.Damage = attackerComponent.AttackStats.DamagePerBullet;
                    shootData.AOE = rocketStats.AOE;
                    shootData.ProjectileSpeed = attackerComponent.AttackStats.ProjectileSpeed;
                    shootData.InitialFlyTime = currentDeltaTime;
                    //is rocket flying left or right initially
                    int dirRandomizeMultiplier = attackerComponent.Bullets % 2 == 0 ? 1 : -1;
                    Shoot(shootData, targetAheadPosition, rocketStats.ScatterDistance, dirRandomizeMultiplier, sortKey, ref random, attackerComponent);
                    statistics.Shoots++;

                    RandomComponent.SetRandom(random, JobsUtility.ThreadIndex);
                }

                ResetTimers(ref attackerComponent);
            }

            possibleTargets.Dispose();
        }


        private static bool CanAttack(float2 targetPosition, float2 towerPosition, float maxRange)
        {
            return WithinAttackRange(targetPosition, towerPosition, MinRocketRange(maxRange), maxRange);
        }


        private void Shoot(ShootData shootData, float2 targetPosition, float scatterDistance, int dirRandomizeMultiplier, int sortKey, ref Random random, AttackerComponent attacker)
        {
            shootData.Origin += Utilities.GetNormal(shootData.Direction) * dirRandomizeMultiplier * barrelWidth;

            float2 normalDirection = Utilities.GetNormal(shootData.Direction.GetRotated(offsetRotationAngle * dirRandomizeMultiplier));
            float2 offsetPoint = shootData.Origin + normalDirection * dirRandomizeMultiplier * random.NextFloat2(offsetDistance);

            int createProjectileSortIndex = sortKey;

            DynamicBuffer<EntitiesBuffer> dynamicBuffer = CreateShootEvent(ECB, attacker, shootData.Tower, sortKey, shootData.Direction, shootData.Origin);

            for (int i = 0; i < attacker.AttackStats.ShootingStats.ProjectilesPerShot; i++)
            {
                Utilities.GetGaussian(ref random, 0, scatterDistance, out float result, out _);
                var position = targetPosition + result * random.NextFloat2Direction();
                
                bool isLastBullet = attacker.Bullets == 0;
                RocketProjectile rocketData = new(shootData, offsetPoint, position, isLastBullet, false);
                float2 pos = rocketData.GetPosition();

                var positionComponent = new PositionComponent(pos, math.normalize(shootData.Origin - pos));

                CreateProjectile(createProjectileSortIndex, ECB, positionComponent, dynamicBuffer,attacker.TowerType, out Entity projectile);
                ECB.SetName(createProjectileSortIndex, projectile, $"{nameof(RocketProjectile)}");
                ECB.AddComponent(createProjectileSortIndex, projectile, rocketData);
                
                createProjectileSortIndex += i;
            }
        }
    }
}