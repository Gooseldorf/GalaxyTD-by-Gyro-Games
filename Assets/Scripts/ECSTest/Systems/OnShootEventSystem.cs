using Unity.Burst;
using ECSTest.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System;
using CardTD.Utilities;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(MovingSystem))]
    public partial struct OnShootEventSystem : ISystem
    {
        private EntityQuery shootQuery;

        public void OnCreate(ref SystemState state)
        {
            shootQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MuzzleTimedEvent>()
                .Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            NativeArray<MuzzleTimedEvent> shootEvents = shootQuery.ToComponentDataArray<MuzzleTimedEvent>(Allocator.Temp);
            NativeArray<Entity> entities = shootQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!state.EntityManager.Exists(shootEvents[i].Tower))
                {
                    Debug.LogError("Tower in OnShootEvent doesn't Exist=> WRITE How you got this warning and report to higher ups");
                    ecb.DestroyEntity(entities[i]);
                    continue;
                }
                DynamicBuffer<EntitiesBuffer> dynamicBuffer = state.EntityManager.GetBuffer<EntitiesBuffer>(entities[i]);

                var tagsComponent = state.EntityManager.GetComponentData<TagsComponent>(shootEvents[i].Tower);
                try
                {
                    foreach (Tag tag in tagsComponent.Tags)
                    {
                        if (tag is OnShootTag shootTag)
                            shootTag.OnShoot(shootEvents[i].Tower, entities[i], ecb, state.EntityManager, dynamicBuffer);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"-> error in OnShoot tags: {e}");
                }

                //from Tower visualisationSystem => shoot drag + update states
                Messenger<Entity>.Broadcast(GameEvents.TowerUpdated, shootEvents[i].Tower, MessengerMode.DONT_REQUIRE_LISTENER);

                TowerVisual towerVisual = null;

                if (state.EntityManager.HasComponent<TowerVisualComponent>(shootEvents[i].Tower))
                {
                    towerVisual = state.EntityManager.GetComponentData<TowerVisualComponent>(shootEvents[i].Tower).TowerVisual;
                }

                if (towerVisual != null)
                {
                    towerVisual.Shoot();
                    if (shootEvents[i].TowerId != AllEnums.TowerId.Gatling) //GatlingSound is playing and stopping from GatlingTowerVisual
                    {
                        MusicManager.PlayMuzzleOrImpact(shootEvents[i].TowerId, isMuzzle: true, shootEvents[i].Position);
                    }
                }
                ecb.SetComponentEnabled<MuzzleTimedEvent>(entities[i], false);
            }
            shootEvents.Dispose();
            entities.Dispose();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

        }
    }
}