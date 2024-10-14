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

[UpdateAfter(typeof(MovingSystem))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct MortarTargetingSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<RandomComponent>();
    }

    [BurstCompile(CompileSynchronously = true)]
    private void OnUpdate(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        new MortarTargetingJob
        {
            CreepsLocator = SystemAPI.GetSingleton<CreepsLocator>(),
            ECB = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            DeltaTime = SystemAPI.Time.DeltaTime,
            RandomComponent = SystemAPI.GetSingleton<RandomComponent>(),
        }.ScheduleParallel();
    }

    [BurstCompile(CompileSynchronously = true)]
    private partial struct MortarTargetingJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public CreepsLocator CreepsLocator;
        public RandomComponent RandomComponent;
        public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;

        // private const int projectileIndex = 30000;

        private void Execute(ref AttackerComponent attackerComponent, ref PositionComponent towerPosition, ref AttackerStatisticComponent statistics, ref MortarStatsComponent mortarStats, in PowerableComponent power,DestroyComponent destroyComponent,
            [ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, Entity towerEntity)
        {
            if(destroyComponent.IsNeedToDestroy)
                return;
            
            if (!power.IsTurnedOn)
                return;

            NativeList<CreepInfo> possibleTargets = new(Allocator.Temp);

            float range = attackerComponent.AttackStats.AimingStats.Range;
            float rotationSpeed = attackerComponent.AttackStats.AimingStats.RotationSpeed;
            float attackAngle = attackerComponent.AttackStats.AimingStats.AttackAngle * math.TORADIANS;

            bool shouldAttack = true;
            if (!IsOnBurstAttack(attackerComponent))
            {
                float2 targetPos = float2.zero;

                if (!CreepsLocator.CreepHashMap.TryGetValue(attackerComponent.Target, out CreepInfo creep) || !CanAttack(creep.Position, towerPosition.Position, range))
                {
                    attackerComponent.Target = Entity.Null;

                    CreepsLocator.LocateNearestCreeps(towerPosition.Position, range, ref possibleTargets, default,20);

                    if (possibleTargets.Length > 0)
                    {
                        //TODO: Pick best Target(lowest angle)
                        attackerComponent.Target = possibleTargets[0].Entity;
                        targetPos = creep.Position;
                    }
                }
                else
                    targetPos = creep.Position;

                mortarStats.LastTargetPosition = targetPos;

                if (attackerComponent.Target != Entity.Null && attackerComponent.Bullets > 0 && attackerComponent.ReloadTimer <= 0)
                    RotateToTarget(targetPos, DeltaTime, ref towerPosition, rotationSpeed);

                shouldAttack = ShouldAttack(attackerComponent, towerPosition, attackAngle, targetPos);
            }
            
            float currentDeltaTime = DeltaTime;
            bool filteredCreeps = false;

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
                        for (int i = 0; i < possibleTargets.Length; i++)
                        {
                            if (!WithinAttackAngle(towerPosition.Direction, possibleTargets[i].Position - towerPosition.Position, attackAngle))
                            {
                                possibleTargets.RemoveAt(i);
                                i--;
                            }
                        }

                        filteredCreeps = true;
                    }

                    if (possibleTargets.Length == 0)
                    {
                        CreepInfo creepInfo = new() {Position = mortarStats.LastTargetPosition, Velocity = mortarStats.LastTargetVelocity};
                        possibleTargets.Add(creepInfo);
                    }

                    attackerComponent.Bullets--;

                    Random random = RandomComponent.GetRandom(JobsUtility.ThreadIndex);
                    int targetIndex = random.NextInt(0, possibleTargets.Length);
                    
                    mortarStats.LastTargetPosition = possibleTargets[targetIndex].Position;
                    mortarStats.LastTargetVelocity = possibleTargets[targetIndex].Velocity;
                    
                    var targetAheadPosition = possibleTargets[targetIndex].Position + possibleTargets[targetIndex].Velocity * mortarStats.ArrivalTime;

                    Shoot(towerEntity, ref random, targetAheadPosition, chunkIndex * 128 + indexInChunk, attackerComponent, mortarStats, towerPosition);
                    statistics.Shoots++;

                    RandomComponent.SetRandom(random, JobsUtility.ThreadIndex);
                }

                ResetTimers(ref attackerComponent);
            }

            possibleTargets.Dispose();
        }

        private void Shoot(Entity towerEntity, ref Random random, float2 targetPosition, int sortKey, AttackerComponent attacker, MortarStatsComponent mortarStatsComponent,
            PositionComponent attackerPosition)
        {
            if (towerEntity == Entity.Null)
                Debug.Log("entity is null");

            int createProjectileSortIndex = sortKey;

            float2 shootPosition = attackerPosition.Position + (attackerPosition.Direction * attacker.StartOffset);
            DynamicBuffer<EntitiesBuffer> dynamicBuffer = CreateShootEvent(ECB, attacker, towerEntity, sortKey, attackerPosition.Direction, shootPosition);
            
            for (int i = 0; i < attacker.AttackStats.ShootingStats.ProjectilesPerShot; i++)
            {
                Utilities.GetGaussian(ref random, 0, mortarStatsComponent.ScatterDistance, out float result, out _);
                float2 position = targetPosition + result * random.NextFloat2Direction();
                PositionComponent positionComponent = new(position, Vector2.right);

                CreateProjectile(createProjectileSortIndex, ECB, positionComponent,dynamicBuffer,attacker.TowerType, out Entity projectileEntity);
                ECB.SetName(createProjectileSortIndex,projectileEntity,$"{nameof(MortarProjectile)}");

                bool isLastBullet = attacker.Bullets == 0;
                MortarProjectile mortarProjectileComponent = new(towerEntity, attacker.AttackStats.DamagePerBullet, targetPosition, mortarStatsComponent.ArrivalTime, mortarStatsComponent.AOE, isLastBullet, false);
                ECB.AddComponent(createProjectileSortIndex, projectileEntity, mortarProjectileComponent);

                createProjectileSortIndex += 1;
            }
        }

        private static bool CanAttack(float2 targetPosition, float2 towerPosition, float maxRange)
        {
            return WithinAttackRange(targetPosition, towerPosition, 0, maxRange);
        }
    }
}