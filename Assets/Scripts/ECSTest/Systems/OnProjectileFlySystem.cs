using ECSTest.Components;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct OnProjectileFlySystem : ISystem
    {
        private EntityQuery projectileQuery;

        public void OnCreate(ref SystemState state)
        {
            projectileQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ProjectileFlyComponent, ProjectileComponent,DestroyComponent>()
                .Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            
            EntityCommandBuffer ecb = new(Allocator.Temp);
            EntityManager manager = state.EntityManager;

            NativeArray<Entity> entities = projectileQuery.ToEntityArray(Allocator.Temp);
            NativeArray<ProjectileComponent> projectileComponents = projectileQuery.ToComponentDataArray<ProjectileComponent>(Allocator.Temp);
            NativeArray<DestroyComponent> destroyComponents = projectileQuery.ToComponentDataArray<DestroyComponent>(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                if(destroyComponents[i].IsNeedToDestroy)
                    continue;
                
                ProjectileComponent projectileComponent = projectileComponents[i];
                
                if (!manager.Exists(projectileComponent.AttackerEntity))
                {
                    Debug.LogError("Tower in OnProjectileFlySystem doesn't Exist");
                    ecb.DestroyEntity(entities[i]);
                    continue;
                }
                
                TagsComponent tagsComponent = state.EntityManager.GetComponentData<TagsComponent>(projectileComponent.AttackerEntity);
                OnProjectileFly(entities[i], tagsComponent.Tags, manager, ecb);
            }

            entities.Dispose();
            destroyComponents.Dispose();
            projectileComponents.Dispose();
            ecb.Playback(manager);
            ecb.Dispose();
            
            
            try
            {
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in OnProjectileFlySystem {e}");
            }
            
        }

        private void OnProjectileFly(Entity projectile, List<Tag> tags, EntityManager manager, EntityCommandBuffer ecb)
        {
            try
            {
                foreach (Tag tag in tags)
                {
                    if (tag is OnProjectileFlyTag projectileFlyTag)
                        projectileFlyTag.OnProjectileFly(projectile, manager, ecb);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"-> error in OnProjectileFly tags: {e}");
            }
        }
    }
}