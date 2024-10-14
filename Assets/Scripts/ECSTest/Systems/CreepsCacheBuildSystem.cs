using ECSTest.Components;
using ECSTest.Systems;
using NativeTrees;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(MovingSystem))]
public partial struct CreepsCacheBuildSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CreepsLocatorCache>();
    }

    public static void Init(World world, float2 botLeftCorner, float2 topRightCorner)
    {
        AABB2D bounds = new(botLeftCorner, topRightCorner);

        //TODO: check if singleton exists and if it does replace it with new one
        CreepsLocatorCache creepsLocatorCache = new(bounds);

        world.EntityManager.CreateSingleton(creepsLocatorCache);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        JobHandle depend = new ClearJob
        {
            CreepsLocatorCache = SystemAPI.GetSingletonRW<CreepsLocatorCache>().ValueRW
        }.Schedule(state.Dependency);

        state.Dependency = new CalculateCrepsCache
        {
            CreepsLocatorCache = SystemAPI.GetSingletonRW<CreepsLocatorCache>().ValueRW
        }
        .Schedule(depend);
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct ClearJob : IJob
    {
        public CreepsLocatorCache CreepsLocatorCache;

        public void Execute()
        {
            CreepsLocatorCache.CreepsTree.Clear();
            CreepsLocatorCache.CreepEntities.Clear();
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    private partial struct CalculateCrepsCache : IJobEntity
    {
        public CreepsLocatorCache CreepsLocatorCache;

        [BurstCompile(CompileSynchronously = true)]
        public void Execute(in PositionComponent position, in CreepComponent creepComponent, in SharedCreepData sharedData, in Movable movable, in DestroyComponent destroy, Entity entity)
        {
            if (destroy.IsNeedToDestroy)
                return;

            CreepInfo creepInfo = new()
            {
                Entity = entity,
                CollisionRange = sharedData.CollisionRange,
                Position = position.Position,
                Velocity = position.Direction,
                NormVelocity = math.normalizesafe(position.Direction),
                Mass = creepComponent.Mass,
                KineticEnergy = math.lengthsq(position.Direction) * creepComponent.Mass,
                IsGoingIn = movable.IsGoingIn,
                ObstacleType = sharedData.ObstacleType,
                FleshType = sharedData.FleshType,
                ArmorType = sharedData.ArmorType
            };
            CreepsLocatorCache.CreepsTree.Insert(creepInfo, creepInfo.GetBounds);
            CreepsLocatorCache.CreepEntities.Add(entity, creepInfo);
        }
    }

    /// <summary>
    /// Singleton Used store Build new Tree in background
    /// </summary>
    public struct CreepsLocatorCache : IComponentData, IDisposable
    {
        public NativeQuadtree<CreepInfo> CreepsTree;
        public NativeHashMap<Entity, CreepInfo> CreepEntities;

        public void Dispose()
        {
            CreepsTree.Dispose();
            CreepEntities.Dispose();
        }

        public CreepsLocatorCache(AABB2D bounds)
        {
            CreepsTree = new NativeQuadtree<CreepInfo>(bounds, Allocator.Persistent);
            CreepEntities = new NativeHashMap<Entity, CreepInfo>(20, Allocator.Persistent);
        }
    }
}