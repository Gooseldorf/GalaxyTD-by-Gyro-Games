using ECSTest.Components;
using System;
using Unity.Collections;
using Unity.Entities;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(CreepsCacheBuildSystem))]
    public partial struct OnCollisionEventSystem : ISystem
    {
        private EntityQuery collisionQuery;
        private EntityQuery aoeCollisionQuery;
        private EntityQuery wallKnockbackCollisionQuery;
        private EntityQuery obstacleCollisionQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CashComponent>();

            collisionQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GunCollisionEvent>()
                .Build(ref state);

            aoeCollisionQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AOECollisionEvent>()
                .Build(ref state);

            wallKnockbackCollisionQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<KnockBackWallDamageEvent>()
                .Build(ref state);

            obstacleCollisionQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CollisionObstacleEvent>()
                .Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.CompleteDependencyBeforeRW<CashComponent>();
            RefRW<CashComponent> cashComponent = SystemAPI.GetSingletonRW<CashComponent>();

            EntityCommandBuffer ecb = new(Allocator.Temp);

            NativeArray<GunCollisionEvent> collisionsEvents = collisionQuery.ToComponentDataArray<GunCollisionEvent>(Allocator.Temp);
            NativeArray<Entity> entities = collisionQuery.ToEntityArray(Allocator.Temp);
            collisionsEvents.Sort();
            for (int i = 0; i < collisionsEvents.Length; i++)
            {
                DamageSystem.DoDamage(collisionsEvents[i], state.EntityManager, cashComponent, ecb);
            }

            foreach (Entity entity in entities)
                ecb.SetComponentEnabled<GunCollisionEvent>(entity, false);

            entities.Dispose();
            collisionsEvents.Dispose();

            CreepsLocator creepLocator = SystemAPI.GetSingleton<CreepsLocator>();
            NativeArray<AOECollisionEvent> aoeCollisionEvents = aoeCollisionQuery.ToComponentDataArray<AOECollisionEvent>(Allocator.Temp);
            entities = aoeCollisionQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < aoeCollisionEvents.Length; i++)
            {
                AOECollisionEvent aoeEvent = aoeCollisionEvents[i];
                DamageSystem.DoAOEDamage(creepLocator, state.EntityManager, aoeEvent, cashComponent, ecb);
                ecb.SetComponentEnabled<AOECollisionEvent>(entities[i], false);
            }

            entities.Dispose();
            aoeCollisionEvents.Dispose();

            NativeArray<KnockBackWallDamageEvent> wallDamageEvents = wallKnockbackCollisionQuery.ToComponentDataArray<KnockBackWallDamageEvent>(Allocator.Temp);
            entities = wallKnockbackCollisionQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < wallDamageEvents.Length; i++)
            {
                DamageSystem.DoDamageByWall(state.EntityManager, wallDamageEvents[i], cashComponent, ecb);
                ecb.SetComponentEnabled<KnockBackWallDamageEvent>(entities[i], false);
            }

            wallDamageEvents.Dispose();
            entities.Dispose();

            NativeArray<CollisionObstacleEvent> obstacleEvents = obstacleCollisionQuery.ToComponentDataArray<CollisionObstacleEvent>(Allocator.Temp);
            entities = obstacleCollisionQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < obstacleEvents.Length; i++)
            {
                OnRicochet(obstacleEvents[i], cashComponent, state.EntityManager, ecb);
                ecb.SetComponentEnabled<CollisionObstacleEvent>(entities[i], false);
            }

            obstacleEvents.Dispose();
            entities.Dispose();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void OnRicochet(CollisionObstacleEvent collisionEvent, RefRW<CashComponent> cashComponent, EntityManager manager, EntityCommandBuffer ecb)
        {
            if (collisionEvent.ProjectileComponent.RicochetCount > 0)
            {
                if (!manager.Exists(collisionEvent.Tower))
                    return;

                TagsComponent tagsComponent = manager.GetComponentData<TagsComponent>(collisionEvent.Tower);
                try
                {
                    foreach (Tag tag in tagsComponent.Tags)
                    {
                        if (tag is OnRicochetTag ricochetTag)
                            ricochetTag.OnRicochet(collisionEvent.ProjectileComponent, new PositionComponent() { Direction = collisionEvent.CollisionDirection, Position = collisionEvent.Point }, cashComponent, manager, ecb);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"-> error in OnRicochet tags: {e}");
                }
            }
        }
    }
}