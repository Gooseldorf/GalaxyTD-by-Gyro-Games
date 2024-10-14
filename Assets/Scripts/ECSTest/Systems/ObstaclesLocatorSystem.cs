using CardTD.Utilities;
using ECSTest.Components;
using NativeTrees;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static AllEnums;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ObstaclesCacheBuildSystem))]
    public partial struct ObstaclesLocatorSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ObstaclesLocator>();
            state.RequireForUpdate<ObstaclesLocatorCache>();
        }

        public static void Init(World world, float2 botLeftCorner, float2 topRightCorner)
        {
            AABB2D bounds = new(botLeftCorner, topRightCorner);

            world.EntityManager.CreateSingleton(new ObstaclesLocator(bounds));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new UpdateObstaclesLocatorJob
            {
                ObstaclesLocator = SystemAPI.GetSingletonRW<ObstaclesLocator>().ValueRW, ObstaclesLocatorCache = SystemAPI.GetSingleton<ObstaclesLocatorCache>()
            }.Schedule(state.Dependency);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct UpdateObstaclesLocatorJob : IJob
    {
        public ObstaclesLocator ObstaclesLocator;
        [ReadOnly] public ObstaclesLocatorCache ObstaclesLocatorCache;

        public void Execute()
        {
            ObstaclesLocator.ObstaclesTree.CopyFrom(ObstaclesLocatorCache.ObstaclesTree);
        }
    }


    public struct ObstaclesLocator : IComponentData, IDisposable
    {
        public NativeQuadtree<ObstacleInfo> ObstaclesTree;

        public ObstaclesLocator(AABB2D bounds) => ObstaclesTree = new NativeQuadtree<ObstacleInfo>(bounds, Allocator.Persistent);

        public void Dispose()
        {
            ObstaclesTree.Dispose();
        }

        public bool RaycastObstacle(float2 startPosition, float2 endPosition, out QuadtreeRaycastHit<ObstacleInfo> hit, ObstacleInfo obstacleInfo, bool nearCheck = true)
        {
            float2 r = endPosition - startPosition;
            Ray2D ray = new(startPosition, r);

            var obstacleRayIntersecter = new ObstacleRayIntersecter() {R = r, ObstacleInfo = obstacleInfo};

            return ObstaclesTree.Raycast(ray, out hit, obstacleRayIntersecter, math.length(r));
            //return ObstaclesTree.Raycast(ray, out hit, RayAABBIntersecter , math.length(r));
            //quadtree.Raycast<RayAABBIntersecter<T>>(ray, out hit, maxDistance: maxDistance);
        }
    }

    public struct ObstacleRayIntersecter : IQuadtreeRayIntersecter<ObstacleInfo>
    {
        public float2 R;
        public ObstacleInfo ObstacleInfo;

        public bool IntersectRay(in PrecomputedRay2D ray, ObstacleInfo obstacleInfo, AABB2D objBounds, out float2 closestNormal, out float distance)
        {
            return HasSquareCollision(obstacleInfo.GetPoints(), obstacleInfo.Entity, ray.origin, out distance, out closestNormal);
        }

        private bool HasSquareCollision(NativeArray<float2> points, Entity collisionEntity, float2 startPoint, out float distance, out float2 normal)
        {
            normal = new();
            distance = float.MaxValue;

            if (ObstacleInfo.Entity == collisionEntity)
                return false;

            if (IsPointInside(points, startPoint))
                return false;

            for (int i = 0; i < points.Length; i++)
            {
                int nextPoint = (i + 1) % points.Length;
                if (FindSegmentIntersection(startPoint, points[i], points[nextPoint],
                        out float2 segmentIntersection, out float2 segmentNormal))
                {
                    float tempDist = math.distance(startPoint, segmentIntersection);

                    if (tempDist < distance)
                    {
                        normal = segmentNormal;
                        distance = tempDist;
                    }
                }
            }

            return (distance < float.MaxValue);
        }

        private bool IsPointInside(NativeArray<float2> convexPolygon, float2 pTest)
        {
            for (int i = 0; i < convexPolygon.Length; i++)
            {
                int nextPoint = (i + 1) % convexPolygon.Length;
                float side = Utilities.CrossProduct(pTest - convexPolygon[i],
                    convexPolygon[nextPoint] - convexPolygon[i]);
                if (side < 0) return false;
            }

            return true;
        }

        private bool FindSegmentIntersection(float2 startPoint, float2 currentPoint, float2 nexPoint,
            out float2 collisionPoint,
            out float2 normal)
        {
            collisionPoint = new();

            normal = (nexPoint - currentPoint).GetNormal();

            float2 s = nexPoint - currentPoint;

            float crossRS = Utilities.CrossProduct(R, s);

            // we should ignore parallel case
            if (crossRS == 0)
                return false;

            float t = Utilities.CrossProduct(currentPoint - startPoint, s) / crossRS;
            float u = Utilities.CrossProduct(currentPoint - startPoint, R) / crossRS;
            collisionPoint = startPoint + t * R;
            return t <= 1 && t >= 0 && u <= 1 && u >= 0;
        }
    }


    public struct ObstacleInfo
    {
        public Entity Entity;
        public float2 BotLeftPoint;
        public float2 TopLeftPoint;
        public float2 TopRightPoint;
        public float2 BotRightPoint;
        public ObstacleType ObstacleType;

        public AABB2D GetBounds => new AABB2D(BotLeftPoint, TopRightPoint);
    }
}