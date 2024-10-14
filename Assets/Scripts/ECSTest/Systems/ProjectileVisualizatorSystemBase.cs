using CardTD.Utilities;
using ECSTest.Components;
using Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using Visual;
using TowerId = AllEnums.TowerId;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(RemoveEventSystem))]
    public partial struct ProjectileVisualizatorSystemBase : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimpleEffectManager effectManager = GameServices.Instance.Get<SimpleEffectManager>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((LaserComponent laserComponent, PositionComponent positionComponent, Entity entity) in SystemAPI.Query<LaserComponent, PositionComponent>().WithEntityAccess())
            {
                DynamicBuffer<Float2Buffer> buffer = state.EntityManager.GetBuffer<Float2Buffer>(entity);
                laserComponent.Visual.UpdatePosition(buffer.ToNativeArray(Allocator.Temp), positionComponent.Position.ToFloat3());
            }

            foreach ((ProjectileComponent projectileComponent, PositionComponent positionComponent, DestroyComponent destroyComponent, Entity entity) in SystemAPI
                         .Query<ProjectileComponent, PositionComponent, DestroyComponent>().WithAbsent<Link2D>().WithAbsent<LaserComponent>()
                         .WithEntityAccess())
            {
                if (destroyComponent.IsNeedToDestroy)
                    continue;

                if (projectileComponent.TowerId != TowerId.Laser)
                {
                    if (projectileComponent.IsEnhanced)
                    {
                        effectManager.EnhancedProjectilesDict.TryGetValue(projectileComponent.TowerId, out IObjectPool<GameObject> tempPool);
                        SetProjectile(positionComponent, tempPool, entity, ecb);
                    }
                    else
                    {
                        effectManager.ProjectilesDict.TryGetValue(projectileComponent.TowerId, out IObjectPool<GameObject> tempPool);
                        SetProjectile(positionComponent, tempPool, entity, ecb);
                    }
                }
                else
                {
                    var buffer = state.EntityManager.GetBuffer<Float2Buffer>(entity);
                    IObjectPool<GameObject> tempPool = null;
                    if (projectileComponent.IsEnhanced)
                        effectManager.EnhancedProjectilesDict.TryGetValue(projectileComponent.TowerId, out tempPool);
                    else
                        effectManager.ProjectilesDict.TryGetValue(projectileComponent.TowerId, out tempPool);

                    //var towerPosition = state.EntityManager.GetComponentData<PositionComponent>(projectileComponent.AttackerEntity);

                    float shotDelay = .1f;
                    if (state.EntityManager.Exists(projectileComponent.AttackerEntity))
                        shotDelay = state.EntityManager.GetComponentData<AttackerComponent>(projectileComponent.AttackerEntity).AttackStats.ShootingStats.ShotDelay;

                    //towerPosition.Position + towerPosition.Direction * attackerComponent.StartOffset
                    SetLaserProjectile(positionComponent, projectileComponent.StartPosition, tempPool, buffer.ToNativeArray(Allocator.Temp), entity, ecb, shotDelay);
                }
            }

            foreach ((RocketProjectile rocketProjectile, PositionComponent positionComponent,DestroyComponent destroyComponent, Entity entity) in SystemAPI.Query<RocketProjectile, PositionComponent,DestroyComponent>().WithAbsent<Link2D>()
                         .WithEntityAccess())
            {
                if(destroyComponent.IsNeedToDestroy)
                    continue;
                
                if (rocketProjectile.IsEnhanced)
                {
                    effectManager.EnhancedProjectilesDict.TryGetValue(TowerId.Rocket, out IObjectPool<GameObject> rocketPool);
                    SetProjectile(positionComponent, rocketPool, entity, ecb);
                }
                else
                {
                    effectManager.ProjectilesDict.TryGetValue(TowerId.Rocket, out IObjectPool<GameObject> rocketPool);
                    SetProjectile(positionComponent, rocketPool, entity, ecb);
                }

                var destinationEffectPool = effectManager.RocketProjectileDestinationPool;
                destinationEffectPool.Get().GetComponent<ProjectileDestinationVisual>().Init(destinationEffectPool, rocketProjectile.Target, rocketProjectile.TotalFlyTime, rocketProjectile.AOE);
            }


            foreach ((MortarProjectile mortarProjectile,DestroyComponent destroyComponent, Entity entity) in SystemAPI.Query<MortarProjectile,DestroyComponent>().WithAbsent<MortarDestinationComponent>()
                         .WithEntityAccess())
            {
                if(destroyComponent.IsNeedToDestroy)
                    continue;
                
                if (mortarProjectile.IsEnhanced)
                {
                    effectManager.EnhancedProjectilesDict.TryGetValue(TowerId.Mortar, out IObjectPool<GameObject> mortarPool);
                    SetDestinationVisual(mortarPool, mortarProjectile, entity, ecb);
                }
                else
                {
                    effectManager.ProjectilesDict.TryGetValue(TowerId.Mortar, out IObjectPool<GameObject> mortarPool);
                    SetDestinationVisual(mortarPool, mortarProjectile, entity, ecb);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            void SetProjectile(PositionComponent positionComponent, IObjectPool<GameObject> tempPool, Entity entity, EntityCommandBuffer ecb)
            {
                var go = GetProjectile(positionComponent, tempPool);
                var component = new Link2D() {Companion = go, Release = (x) => go.GetComponent<ProjectileVisual>().Hide(tempPool),IsHide = false};
                ecb.AddComponent(entity, component);
            }

            void SetLaserProjectile(PositionComponent positionComponent, float2 startPosition, IObjectPool<GameObject> tempPool, NativeArray<Float2Buffer> array, Entity entity,
                EntityCommandBuffer ecb, float showTime)
            {
                var lv = GetLaserProjectile(startPosition, positionComponent, tempPool, array, showTime, entity);
                ecb.AddComponent(entity, new LaserComponent() {Visual = lv});
            }

            void SetDestinationVisual(IObjectPool<GameObject> tempPool, MortarProjectile mortarProjectile, Entity entity, EntityCommandBuffer ecb)
            {
                var go = tempPool.Get();
                go.GetComponent<ProjectileDestinationVisual>().Init(tempPool, mortarProjectile.Target, mortarProjectile.RemainingTime, mortarProjectile.AOE);
                ecb.AddComponent(entity, new MortarDestinationComponent());
            }
        }

        private static GameObject GetProjectile(PositionComponent positionComponent, IObjectPool<GameObject> pool)
        {
            GameObject go = pool.Get();
            go.transform.position = positionComponent.Position.ToFloat3();
            go.transform.right = positionComponent.Direction.ToFloat3();
            go.SetActive(true);
            return go;
        }


        private static LaserProjectileVisual GetLaserProjectile(float2 start, PositionComponent positionComponent, IObjectPool<GameObject> pool, NativeArray<Float2Buffer> array, float showTime,
            Entity projectile)
        {
            GameObject go = pool.Get();
            LaserProjectileVisual rf = go.GetComponent<LaserProjectileVisual>();
            rf.Realize = () => rf.Hide(pool);
            rf.SetStartPosition(start.ToFloat3(), showTime, projectile);
            //rf.UpdatePosition(array,positionComponent.Position.ToFloat3());
            return rf;
        }
    }
}