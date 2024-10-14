using ECSTest.Aspects;
using ECSTest.Components;
using CardTD.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System;

namespace ECSTest.Systems
{
    [UpdateBefore(typeof(BRGSystem))]
    public partial struct AnimationSystem : ISystem, ISystemStartStop
    {
        private const float rotationSpeed = 150 * math.TORADIANS;

        private NativeArray<int> muzzleIndexes;
        private NativeArray<int> impactIndexes;
        private NativeArray<float> muzzleTimeBetweenFrames;
        private NativeArray<float> impactTimeBetweenFrames;
        private int maxTowerIdCount;

        public void OnStartRunning(ref SystemState state)
        {
            var renderDataHolder = GameServices.Instance.RenderDataHolder;
            var muzzlesRenderStats = renderDataHolder.MuzzlesRenderStats;
            muzzleIndexes = new NativeArray<int>(muzzlesRenderStats.MuzzleTowerIndexes.ToArray(), Allocator.Persistent);
            impactIndexes = new NativeArray<int>(muzzlesRenderStats.ImpactTowerIndexes.ToArray(), Allocator.Persistent);
            muzzleTimeBetweenFrames = new NativeArray<float>(muzzlesRenderStats.GetTimeBetweenFrames(true).ToArray(), Allocator.Persistent);
            impactTimeBetweenFrames = new NativeArray<float>(muzzlesRenderStats.GetTimeBetweenFrames(false).ToArray(), Allocator.Persistent);
            maxTowerIdCount = Enum.GetNames(typeof(AllEnums.TowerId)).Length;
        }

        public void OnStopRunning(ref SystemState state)
        {
            if(muzzleIndexes.IsCreated)
                muzzleIndexes.Dispose();

            if(muzzleTimeBetweenFrames.IsCreated)
                muzzleTimeBetweenFrames.Dispose();

            if(impactIndexes.IsCreated)
                impactIndexes.Dispose();

            if(impactTimeBetweenFrames.IsCreated)
                impactTimeBetweenFrames.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new CalculateRenderData()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel();

            var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();

            new CalculateMuzzleRenderData()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                MaxTowerIdCount = maxTowerIdCount,
                MuzzleIndexes = muzzleIndexes,
                TimeBetweenFrames = muzzleTimeBetweenFrames,
                EntityCommandBuffer = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();

            new CalculateImpactRenderData()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                MaxTowerIdCount = maxTowerIdCount,
                ImpactIndexes = impactIndexes,
                TimeBetweenFrames = impactTimeBetweenFrames,
                EntityCommandBuffer = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        private partial struct CalculateRenderData : IJobEntity
        {
            [ReadOnly]
            public float DeltaTime;

            public void Execute(CreepAnimationAspect creepAnimationAspect, in SharedRenderData sharedBatchData, in DestroyComponent destroyComponent, in PositionComponent positionComponent, in SharedCreepData creepData)
            {
                if ((creepAnimationAspect.AnimationComponent.ValueRO.AnimationState == AllEnums.AnimationState.Run) && (creepAnimationAspect.CreepComponent.ValueRO.Escaped == true))
                {
                    InitDeathAnimation(creepAnimationAspect, sharedBatchData);
                    creepAnimationAspect.AnimationComponent.ValueRW.FrameNumber = (byte)(sharedBatchData.DieFrames - 1);
                    return;
                }

                if (destroyComponent.IsNeedToDestroy)
                {
                    creepAnimationAspect.AnimationComponent.ValueRW.AnimationTimer += DeltaTime;

                    if (creepAnimationAspect.AnimationComponent.ValueRO.AnimationState == AllEnums.AnimationState.Run)
                    {
                        // init death animation here
                        InitDeathAnimation(creepAnimationAspect, sharedBatchData);
                        return;
                    }

                    if (creepAnimationAspect.AnimationComponent.ValueRO.AnimationTimer > sharedBatchData.TimeBetweenDieFrames)
                    {
                        creepAnimationAspect.AnimationComponent.ValueRW.AnimationTimer -= sharedBatchData.TimeBetweenDieFrames;
                        byte frameNumber = creepAnimationAspect.AnimationComponent.ValueRO.FrameNumber;
                        frameNumber = (byte)math.min(frameNumber + 1, sharedBatchData.DieFrames - 1);
                        creepAnimationAspect.AnimationComponent.ValueRW.FrameNumber = frameNumber;
                    }
                }
                else
                {
                    creepAnimationAspect.AnimationComponent.ValueRW.AnimationTimer += DeltaTime * positionComponent.Direction.GetMagnitude() / creepData.Speed;

                    float angle = Utilities.SignedAngleBetween(creepAnimationAspect.AnimationComponent.ValueRO.Direction, positionComponent.Direction);

                    float maxAnglePerFrame = rotationSpeed * DeltaTime;

                    if (math.abs(angle) < maxAnglePerFrame)
                        creepAnimationAspect.AnimationComponent.ValueRW.Direction = positionComponent.Direction;
                    else
                        creepAnimationAspect.AnimationComponent.ValueRW.Direction = creepAnimationAspect.AnimationComponent.ValueRW.Direction.GetRotated(math.clamp(angle, -maxAnglePerFrame, maxAnglePerFrame));

                    if (creepAnimationAspect.AnimationComponent.ValueRO.AnimationTimer > sharedBatchData.TimeBetweenRunFrames)
                    {
                        creepAnimationAspect.AnimationComponent.ValueRW.AnimationTimer -= sharedBatchData.TimeBetweenRunFrames;
                        byte frameNumber = creepAnimationAspect.AnimationComponent.ValueRO.FrameNumber;
                        frameNumber = (byte)((frameNumber + 1) % sharedBatchData.RunFrames);
                        creepAnimationAspect.AnimationComponent.ValueRW.FrameNumber = frameNumber;
                    }

                    if (creepAnimationAspect.AnimationComponent.ValueRO.DamageTimer > 0)
                        creepAnimationAspect.AnimationComponent.ValueRW.DamageTimer -= DeltaTime;

                    if (creepAnimationAspect.AnimationComponent.ValueRO.DamageTaken)
                    {
                        creepAnimationAspect.AnimationComponent.ValueRW.DamageTaken = false;
                        creepAnimationAspect.AnimationComponent.ValueRW.DamageTimer = .1f;
                    }

                    bool isSlowed = creepAnimationAspect.SlowComponent.ValueRO.Time > 0;
                    bool isRadiated = creepAnimationAspect.RadiationComponent.ValueRO.Time > 0;

                    creepAnimationAspect.AnimationComponent.ValueRW.Color = (isSlowed, isRadiated) switch
                    {
                        { isSlowed: true, isRadiated: true } => new float4(0.56862745098039215686274509803922f, 0.25490196078431372549019607843137f, 1, 1),
                        { isSlowed: true, isRadiated: false } => new float4(0, 0.44705882352941176470588235294118f, 1, 1),
                        { isSlowed: false, isRadiated: true } => new float4(0.62068965517241379310344827586207f, 1, 0, 1),
                        _ => new float4(1, 1, 1, 1)
                    };
                }
            }

            private static void InitDeathAnimation(CreepAnimationAspect creepAnimationAspect, SharedRenderData sharedBatchData)
            {
                creepAnimationAspect.AnimationComponent.ValueRW.AnimationState = AllEnums.AnimationState.Death;
                creepAnimationAspect.AnimationComponent.ValueRW.AnimationTimer = 0;
                creepAnimationAspect.AnimationComponent.ValueRW.FrameNumber = 0;
                creepAnimationAspect.AnimationComponent.ValueRW.Color = new float4(sharedBatchData.DeathColor, 1);
                creepAnimationAspect.AnimationComponent.ValueRW.DamageTimer = 0;
                creepAnimationAspect.AnimationComponent.ValueRW.DamageTaken = false;
                creepAnimationAspect.AnimationComponent.ValueRW.IsOutline = false;
            }
        }

        [BurstCompile]
        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        private partial struct CalculateMuzzleRenderData : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public NativeArray<float> TimeBetweenFrames;
            [ReadOnly] public int MaxTowerIdCount;
            [ReadOnly] public NativeArray<int> MuzzleIndexes;
            public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

            public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, ref MuzzleTimedEvent muzzleAnimationComponent)
            {
                int towerIndex = Utilities.TowerIdToInt(muzzleAnimationComponent.TowerId) - 1;


                if (muzzleAnimationComponent.AnimationTimer > TimeBetweenFrames[towerIndex])
                {
                    if (CheckIsNextIndexVaild(ref muzzleAnimationComponent, towerIndex))
                    {
                        EntityCommandBuffer.DestroyEntity(chunkIndex * 128 + indexInChunk, entity);
                        return;
                    }

                    muzzleAnimationComponent.CurrentFrame++;
                    muzzleAnimationComponent.AnimationTimer = 0;
                }
                muzzleAnimationComponent.AnimationTimer += DeltaTime;
            }

            private bool CheckIsNextIndexVaild(ref MuzzleTimedEvent muzzleAnimationComponent, int towerIndex)
            {
                int nextTowerIndex = 2 * (towerIndex + 1);
                nextTowerIndex += muzzleAnimationComponent.IsEnhanced ? 2 * MaxTowerIdCount : 0;
                
                int currentTowerIndex = 2 * towerIndex;
                currentTowerIndex += muzzleAnimationComponent.IsEnhanced ? 2 * MaxTowerIdCount : 0;
                int currentIndex = MuzzleIndexes[currentTowerIndex] + muzzleAnimationComponent.CurrentFrame;
                int lastIndex;

                if (nextTowerIndex < 4 * MaxTowerIdCount)
                    lastIndex = MuzzleIndexes[nextTowerIndex] - 1;
                else
                    lastIndex = MuzzleIndexes[^1];

                return currentIndex + 1 > lastIndex;
            }
        }


        [BurstCompile]
        private partial struct CalculateImpactRenderData : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public NativeArray<float> TimeBetweenFrames;
            [ReadOnly] public int MaxTowerIdCount;
            [ReadOnly] public NativeArray<int> ImpactIndexes;
            public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

            public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, ref ImpactTimedEvent impactAnimationComponent)
            {
                int towerIndex = Utilities.TowerIdToInt(impactAnimationComponent.TowerId) - 1;


                if (impactAnimationComponent.AnimationTimer > TimeBetweenFrames[towerIndex])
                {
                    if (CheckIsNextIndexVaild(ref impactAnimationComponent, towerIndex))
                    {
                        EntityCommandBuffer.DestroyEntity(chunkIndex * 128 + indexInChunk, entity);
                        return;
                    }

                    impactAnimationComponent.CurrentFrame++;
                    impactAnimationComponent.AnimationTimer = 0;
                }
                impactAnimationComponent.AnimationTimer += DeltaTime;
            }

            private bool CheckIsNextIndexVaild(ref ImpactTimedEvent impactAnimationComponent, int towerIndex)
            {
                int nextTowerIndex = 2 * (towerIndex + 1);
                //nextTowerIndex += impactAnimationComponent.IsEnhanced ? 2 * MaxTowerIdCount : 0;

                int currentTowerIndex = 2 * towerIndex;
                //currentTowerIndex += impactAnimationComponent.IsEnhanced ? 2 * MaxTowerIdCount : 0;
                int currentIndex = ImpactIndexes[currentTowerIndex] + impactAnimationComponent.CurrentFrame;
                int lastIndex;

                if (nextTowerIndex < 2 * MaxTowerIdCount)
                    lastIndex = ImpactIndexes[nextTowerIndex] - 1;
                else
                    lastIndex = ImpactIndexes[^1];

                return currentIndex + 1 > lastIndex;
            }
        }
    }
}
