using ECSTest.Aspects;
using ECSTest.Components;
using NativeTrees;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using static AllEnums;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CreepsLocatorSystem))]
    public partial struct ProjectileSystemBase : ISystem
    {
        private static float2 leftBottomPoint;
        private static float2 rightTopPoint;
        private ComponentLookup<AttackerComponent> attakerLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ObstaclesLocator>();
            state.RequireForUpdate<CreepsLocator>();
            state.RequireForUpdate<ProjectileSystemBaseData>();
            attakerLookup = state.GetComponentLookup<AttackerComponent>(true);
        }

        public static void Init(World world, float2 point1, float2 point2)
        {
            world.EntityManager.CreateSingleton(new ProjectileSystemBaseData {LeftBottomPoint = point1, RightTopPoint = point2,});
        }

        [BurstCompile(CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            attakerLookup.Update(ref state);
            var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var data = SystemAPI.GetSingleton<ProjectileSystemBaseData>();
            new ProjectilesJob
            {
                CreepsLocator = SystemAPI.GetSingleton<CreepsLocator>(),
                ObstaclesLocator = SystemAPI.GetSingleton<ObstaclesLocator>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                EntityCommandBuffer = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                LeftBottomPoint = data.LeftBottomPoint,
                RightTopPoint = data.RightTopPoint,
                attakeLookup = attakerLookup,
            }.ScheduleParallel();
        }

        [BurstCompile(CompileSynchronously = true)]
        public partial struct ProjectilesJob : IJobEntity
        {
            public float DeltaTime;
            public float2 LeftBottomPoint;
            public float2 RightTopPoint;
            [ReadOnly] public CreepsLocator CreepsLocator;
            [ReadOnly] public ObstaclesLocator ObstaclesLocator;
            [ReadOnly] public ComponentLookup<AttackerComponent> attakeLookup;

            public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

            private void Execute(ProjectileAspect projectileAspect, [ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, Entity projectileEntity)
            {
                if (projectileAspect.DestroyComponent.ValueRO.IsNeedToDestroy)
                    return;


                float remainingDistance = DeltaTime * projectileAspect.ProjectileComponent.ValueRO.Velocity;

                if (projectileAspect.ProjectileComponent.ValueRO.StartDistance != 0)
                {
                    remainingDistance += projectileAspect.ProjectileComponent.ValueRO.StartDistance;
                    projectileAspect.ProjectileComponent.ValueRW.StartDistance = 0;
                }

                ProjectileCollisionData projectileData = new()
                {
                    ProjectileEntity = projectileEntity,
                    Tower = projectileAspect.ProjectileComponent.ValueRO.AttackerEntity,
                    CreepInfo = default,
                    ObstacleInfo = default,
                    SortKey = chunkIndex,
                    ProjectileAspect = projectileAspect,
                    RemainingDistance = remainingDistance,
                    SortCollisionIndex = 0,
                };

                ReCalculateProjectile(projectileData);
            }


            private void ReCalculateProjectile(ProjectileCollisionData projectileData)
            {
                if(projectileData.ProjectileAspect.DestroyComponent.ValueRO.IsNeedToDestroy)
                    return;
                
                float2 starPosition = projectileData.ProjectileAspect.PositionComponent.ValueRO.Position;

                if (starPosition.x < LeftBottomPoint.x || starPosition.y < LeftBottomPoint.y || starPosition.x > RightTopPoint.x || starPosition.y > RightTopPoint.y)
                {
                    projectileData.ProjectileAspect.DestroyComponent.ValueRW.IsNeedToDestroy = true;
                    projectileData.ProjectileAspect.DestroyComponent.ValueRW.DestroyDelay = 2f;
                    EntityCommandBuffer.SetComponentEnabled<ProjectileComponent>(projectileData.SortKey, projectileData.ProjectileEntity, false);
                    //if (projectileData.ProjectileAspect.ProjectileComponent.ValueRO.TowerId == TowerId.Laser)
                    //    projectileData.ProjectileAspect.DestroyComponent.ValueRW.DestroyDelay = .5f;
                    return;
                }
                
                float2 toPosition = projectileData.ProjectileAspect.PositionComponent.ValueRO.Direction * projectileData.RemainingDistance;
                float2 endPosition = starPosition + toPosition;

                if (CreepsLocator.RaycastCreeps(starPosition, endPosition, out QuadtreeRaycastHit<CreepInfo> hit, projectileData.CreepInfo))
                {
                    projectileData.CreepFleshType = hit.obj.FleshType;
                    projectileData.CreepArmorType = hit.obj.ArmorType;

                    CreateCreepCollisionComponent(EntityCommandBuffer, hit.point, hit.obj.Entity);
                    UpdateProjectileDataAfterCollision(hit);

                    projectileData.ObstacleType = hit.obj.ObstacleType;
                    projectileData.CreepInfo = hit.obj;
                    projectileData.ObstacleInfo = default;
                    projectileData.ObstaclePoint = hit.point;
                }
                else if (ObstaclesLocator.RaycastObstacle(starPosition, endPosition, out QuadtreeRaycastHit<ObstacleInfo> obstacleHit, projectileData.ObstacleInfo,false))
                {
                    UpdateProjectileDataAfterCollision(obstacleHit);

                    projectileData.ObstacleType = obstacleHit.obj.ObstacleType;
                    projectileData.CreepInfo = default;
                    projectileData.ObstacleInfo = obstacleHit.obj;
                    projectileData.ObstaclePoint = obstacleHit.point;

                    CreateObstacleCollisionComponent(EntityCommandBuffer, obstacleHit.point);
                }
                else
                {
                    projectileData.ProjectileAspect.PositionComponent.ValueRW.Position = endPosition;
                    projectileData.ProjectileAspect.ProjectileComponent.ValueRW.DistanceTraveled += projectileData.RemainingDistance;
                    return;
                }

                CalculateCollision(projectileData);

                void CreateCreepCollisionComponent(EntityCommandBuffer.ParallelWriter entityCommandBuffer, float2 collisionPoint, Entity creepEntity)
                {
                    Entity collisionEntity = entityCommandBuffer.CreateEntity(projectileData.SortKey);
                    entityCommandBuffer.SetName(projectileData.SortKey, collisionEntity, $"{nameof(GunCollisionEvent)}");
                    //UnityEngine.Debug.LogError(projectileData.CreepInfo.FleshType);

                    GunCollisionEvent collisionEvent = new()
                    {
                        TowerId = projectileData.ProjectileAspect.ProjectileComponent.ValueRO.TowerId,
                        DistanceTraveled = projectileData.ProjectileAspect.ProjectileComponent.ValueRO.DistanceTraveled,
                        SortCollisionIndex = projectileData.SortCollisionIndex,
                        Point = collisionPoint,
                        CollisionDirection = projectileData.ProjectileAspect.PositionComponent.ValueRO.Direction,
                        Damage = projectileData.ProjectileAspect.ProjectileComponent.ValueRO.Damage,
                        Target = creepEntity,
                        Tower = projectileData.Tower,
                        FleshType = projectileData.CreepFleshType,
                        ArmorType = projectileData.CreepArmorType,
                        ProjectileComponent = projectileData.ProjectileAspect.ProjectileComponent.ValueRO,
                        IsEnhanced = false
                    };
                    entityCommandBuffer.AddComponent(projectileData.SortKey, collisionEntity, collisionEvent);
                }

                void CreateObstacleCollisionComponent(EntityCommandBuffer.ParallelWriter entityCommandBuffer, float2 collisionPoint)
                {
                    Entity collisionEntity = entityCommandBuffer.CreateEntity(projectileData.SortKey);
                    entityCommandBuffer.SetName(projectileData.SortKey, collisionEntity, $"{nameof(CollisionObstacleEvent)}");
                    CollisionObstacleEvent collisionObstacleEvent = new()
                    {
                        TowerId = projectileData.ProjectileAspect.ProjectileComponent.ValueRO.TowerId,
                        Tower = projectileData.Tower,
                        Point = collisionPoint,
                        CollisionDirection = projectileData.ProjectileAspect.PositionComponent.ValueRO.Direction,
                        Normal = projectileData.Normal,
                        ProjectileComponent = projectileData.ProjectileAspect.ProjectileComponent.ValueRO
                    };
                    entityCommandBuffer.AddComponent(projectileData.SortKey, collisionEntity, collisionObstacleEvent);
                }

                void UpdateProjectileDataAfterCollision<T>(QuadtreeRaycastHit<T> collisionHit)
                {
                    float collisionDistance = math.distance(starPosition, collisionHit.point);
                    projectileData.ProjectileAspect.PositionComponent.ValueRW.Position = collisionHit.point;
                    projectileData.ProjectileAspect.ProjectileComponent.ValueRW.DistanceTraveled += collisionDistance;
                    projectileData.RemainingDistance -= collisionDistance;
                    projectileData.Normal = math.normalize(collisionHit.normal);
                }
            }

            private void CalculateCollision(ProjectileCollisionData projectileData)
            {
                if (projectileData.ProjectileAspect.DestroyComponent.ValueRO.IsNeedToDestroy)
                    return;

                projectileData.SortCollisionIndex += 1;
                if (projectileData.ObstacleType is ObstacleType.OnlyPenetrate && projectileData.ProjectileAspect.ProjectileComponent.ValueRO.PenetrationCount > 0)
                {
                    projectileData.ProjectileAspect.ProjectileComponent.ValueRW.PenetrationCount--;
                    projectileData.ProjectileAspect.ProjectileComponent.ValueRW.Damage *= projectileData.ProjectileAspect.ProjectileComponent.ValueRO.DamageMultPerPenetration;
                    if (projectileData.ProjectileAspect.ProjectileComponent.ValueRO.TowerId == TowerId.Laser)
                    {
                        EntityCommandBuffer.AppendToBuffer(projectileData.SortKey, projectileData.ProjectileEntity, (Float2Buffer)projectileData.ObstaclePoint);
                    }

                    ReCalculateProjectile(projectileData);
                    return;
                }

                if (projectileData.ObstacleType is ObstacleType.OnlyRicochet && projectileData.ProjectileAspect.ProjectileComponent.ValueRO.RicochetCount > 0)
                {
                    projectileData.Normal = math.normalize(projectileData.Normal);
                    projectileData.ProjectileAspect.ProjectileComponent.ValueRW.RicochetCount--;
                    projectileData.ProjectileAspect.ProjectileComponent.ValueRW.Damage *= projectileData.ProjectileAspect.ProjectileComponent.ValueRO.DamageMultPerRicochet;
                    float dot = math.dot(projectileData.ProjectileAspect.PositionComponent.ValueRW.Direction, projectileData.Normal);
                    projectileData.ProjectileAspect.PositionComponent.ValueRW.Direction -= (dot * 2 * projectileData.Normal);
                    if (projectileData.ProjectileAspect.ProjectileComponent.ValueRO.TowerId == TowerId.Laser)
                    {
                        EntityCommandBuffer.AppendToBuffer(projectileData.SortKey, projectileData.ProjectileEntity, (Float2Buffer)projectileData.ObstaclePoint);
                    }

                    ReCalculateProjectile(projectileData);
                    return;
                }

                projectileData.ProjectileAspect.DestroyComponent.ValueRW.IsNeedToDestroy = true;
                projectileData.ProjectileAspect.DestroyComponent.ValueRW.DestroyDelay = 2f;
                EntityCommandBuffer.SetComponentEnabled<ProjectileComponent>(projectileData.SortKey, projectileData.ProjectileEntity, false);

                //if (projectileData.ProjectileAspect.ProjectileComponent.ValueRO.TowerId == TowerId.Laser)
                //    projectileData.ProjectileAspect.DestroyComponent.ValueRW.DestroyDelay = .5f;
            }
        }

        private struct ProjectileCollisionData
        {
            public Entity ProjectileEntity;
            public ProjectileAspect ProjectileAspect;
            public Entity Tower;
            public ObstacleType ObstacleType;
            public int SortKey;
            public float2 Normal;
            public float RemainingDistance;
            public CreepInfo CreepInfo;
            public FleshType CreepFleshType;
            public ArmorType CreepArmorType;
            public ObstacleInfo ObstacleInfo;
            public int SortCollisionIndex;
            public float2 ObstaclePoint;
        }
    }

    public struct ProjectileSystemBaseData : IComponentData
    {
        public float2 LeftBottomPoint;

        [FormerlySerializedAs("LightTopPoint")]
        public float2 RightTopPoint;
    }
}