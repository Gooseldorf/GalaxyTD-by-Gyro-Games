using ECSTest.Components;
using NativeTrees;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct ObstaclesCacheBuildSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ObstaclesLocatorCache>();
        }

        public static void Init(World world, float2 botLeftCorner, float2 topRightCorner)
        {
            AABB2D bounds = new(botLeftCorner, topRightCorner);
            world.EntityManager.CreateSingleton(new ObstaclesLocatorCache(bounds));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            JobHandle depend = new ClearJob
            {
                ObstaclesLocatorCache = SystemAPI.GetSingletonRW<ObstaclesLocatorCache>().ValueRW
            }.Schedule(state.Dependency);

            state.Dependency = new CalculateObstaclesCache
            {
                ObstaclesLocatorCache = SystemAPI.GetSingletonRW<ObstaclesLocatorCache>().ValueRW
            }.Schedule(depend);
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct ClearJob : IJob
        {
            public ObstaclesLocatorCache ObstaclesLocatorCache;

            public void Execute()
            {
                ObstaclesLocatorCache.ObstaclesTree.Clear();
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        public partial struct CalculateObstaclesCache : IJobEntity
        {
            public ObstaclesLocatorCache ObstaclesLocatorCache;

            private void Execute(in SquareObstacle squareObstacle, Entity entity)
            {
                ObstacleInfo obstacle = new();
                obstacle.Entity = entity;
                obstacle.ObstacleType = squareObstacle.ObstacleType;
                obstacle.BotLeftPoint = squareObstacle.BotLeftPoint;
                obstacle.TopLeftPoint = squareObstacle.TopLeftPoint;
                obstacle.TopRightPoint = squareObstacle.TopRightPoint;
                obstacle.BotRightPoint = squareObstacle.BotRightPoint;
                ObstaclesLocatorCache.ObstaclesTree.Insert(obstacle, obstacle.GetBounds);
            }
        }
    }

    public struct ObstaclesLocatorCache : IComponentData, IDisposable
    {
        public NativeQuadtree<ObstacleInfo> ObstaclesTree;

        public ObstaclesLocatorCache(AABB2D bounds) => ObstaclesTree = new NativeQuadtree<ObstacleInfo>(bounds, Allocator.Persistent);

        public void Dispose() => ObstaclesTree.Dispose();
    }
}