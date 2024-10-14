using ECSTest.Components;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ReloadingSystem))]
    public partial struct OnReloadEventSystem : ISystem
    {
        private EntityQuery reloadQuery;

        public void OnCreate(ref SystemState state)
        {
            reloadQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ReloadEvent>()
                .Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            EntityManager manager = state.EntityManager;
            
            NativeArray<ReloadEvent> reloadEvents = reloadQuery.ToComponentDataArray<ReloadEvent>(Allocator.Temp);
            NativeArray<Entity> entities = reloadQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                TagsComponent tagsComponent = state.EntityManager.GetComponentData<TagsComponent>(reloadEvents[i].Tower);
                OnReload(reloadEvents[i].Tower, tagsComponent.Tags, manager);
               
                ecb.SetComponentEnabled<ReloadEvent>(entities[i],false);
            }
            
            reloadEvents.Dispose();
            entities.Dispose();

            ecb.Playback(manager);
            ecb.Dispose();
        }

        private void OnReload(Entity attacker, List<Tag> tags, EntityManager manager)
        {
            try
            {
                foreach (Tag tag in tags)
                {
                    if (tag is OnReloadTag reloadTag)
                        reloadTag.OnReload(attacker, manager);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"-> error in OnReload tags: {e}");
            }
        }
    }
}