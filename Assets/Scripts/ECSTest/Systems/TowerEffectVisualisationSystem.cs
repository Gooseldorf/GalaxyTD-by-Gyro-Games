using CardTD.UIAndVisual.Visualization.Muzzles;
using CardTD.Utilities;
using DefaultNamespace;
using ECSTest.Components;
using GoogleMobileAds.Api.AdManager;
using Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using static AllEnums;
using static ECSTest.Systems.TargetingSystemBase;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(RemoveEventSystem))]
    public partial struct TowerEffectVisualisationSystem : ISystem
    {
        private EntityQuery collisionQuery;
        private EntityQuery collisionObstaclesQuery;
        private EntityQuery aoeCollisionQuery;
        private EntityQuery shootQuery;
        private EntityQuery aoeEffectQuery;

        public void OnCreate(ref SystemState state)
        {
            collisionObstaclesQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<CollisionObstacleEvent>().Build(ref state);
            collisionQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<GunCollisionEvent>().Build(ref state);
            aoeCollisionQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<AOECollisionEvent>().Build(ref state);
            shootQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<MuzzleTimedEvent>().Build(ref state);
            aoeEffectQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<TagEffectEvent>().Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            SimpleEffectManager effectManager = GameServices.Instance.Get<SimpleEffectManager>();
            TileDecalSystem tds = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TileDecalSystem>();

            NativeArray<CollisionObstacleEvent> collisionsObstacleEvents = collisionObstaclesQuery.ToComponentDataArray<CollisionObstacleEvent>(Allocator.Temp);
            foreach (CollisionObstacleEvent collision in collisionsObstacleEvents)
            {
                CreateImpactVisual(state.EntityManager, collision.TowerId, collision.Point, -collision.Normal);

                if (collision.TowerId == TowerId.Gauss)
                    tds.AddGaussDecal(collision.Point, -collision.CollisionDirection, effectManager.GaussDecalTiledTexture);
            }

            collisionsObstacleEvents.Dispose();

            NativeArray<GunCollisionEvent> collisionsEvents = collisionQuery.ToComponentDataArray<GunCollisionEvent>(Allocator.Temp);
            foreach (GunCollisionEvent collisionEvent in collisionsEvents)
            {
                CreateImpactVisual(state.EntityManager, collisionEvent.TowerId, collisionEvent.Point, -collisionEvent.CollisionDirection);
               
                SharedCreepData component = state.EntityManager.GetSharedComponent<SharedCreepData>(collisionEvent.Target);
                if(collisionEvent.Damage > component.MaxHp * effectManager.PercentForBlood)
                    if(effectManager.TryGetBloodDecalTexture(collisionEvent.FleshType, collisionEvent.ArmorType, out Texture2D bloodTexture))
                        tds.PrintDecal(collisionEvent.CollisionDirection, collisionEvent.Point, bloodTexture);
                if (IsNeedToDrawDeath(collisionEvent.Target, state.EntityManager))
                {
                    if (effectManager.TryGetDeathDecalTexture(collisionEvent.FleshType, collisionEvent.ArmorType, out Texture2D deathTexture))
                        tds.PrintDecal(collisionEvent.CollisionDirection, collisionEvent.Point, deathTexture);

                    if (collisionEvent.TowerId == TowerId.Gauss && collisionEvent.ProjectileComponent.PenetrationCount == 0)
                        tds.AddGaussDecal(collisionEvent.Point, -collisionEvent.CollisionDirection, effectManager.GaussDecalTiledTexture);
                }
            }
            collisionsEvents.Dispose();

            NativeArray<AOECollisionEvent> aoeCollisionEvents = aoeCollisionQuery.ToComponentDataArray<AOECollisionEvent>(Allocator.Temp);
            foreach (AOECollisionEvent aoeEvent in aoeCollisionEvents)
            {
                CreateAOEImpactVisual(state.EntityManager, aoeEvent);
                if(effectManager.TryGetAoeDecalTexture(aoeEvent.TowerId, out Texture2D aoeImpactDecal))
                    tds.PrintDecal(float2.zero, aoeEvent.Point, aoeImpactDecal);
            }
            aoeCollisionEvents.Dispose();

            NativeArray<TagEffectEvent> tagEffectEvents = aoeEffectQuery.ToComponentDataArray<TagEffectEvent>(Allocator.Temp);
            foreach (TagEffectEvent effectEvent in tagEffectEvents)
                CreateTagEffectVisual(effectManager, effectEvent);

            tagEffectEvents.Dispose();

            //void DrawMuzzle(MuzzleTimedEvent shootEvent, IObjectPool<GameObject> tempPool)
            //{
            //    GameObject muzzle = tempPool.Get();
            //    muzzle.GetComponent<MuzzleVisual>().Init(tempPool, shootEvent.Position, shootEvent.Direction);
            //}
        }

        private bool IsNeedToDrawDeath(Entity target, EntityManager manager)
        {
            DestroyComponent destroyComponent = manager.GetComponentData<DestroyComponent>(target);
            return destroyComponent.IsNeedToDestroy;
        }

        private static void CreateAOEImpactVisual(EntityManager manager, AOECollisionEvent aoeEvent)
        {
            Entity impactEntity = manager.CreateEntity();
            manager.SetName(impactEntity, "ImpactEvent");
            ImpactTimedEvent impactEvent = new() { TowerId = aoeEvent.TowerId, Direction = new(1,0), Position = aoeEvent.Point, IsEnhanced = false, CurrentFrame = 0, AnimationTimer = 0, AoeScale = aoeEvent.AOE };
            manager.AddComponentData(impactEntity, impactEvent);
            MusicManager.PlayMuzzleOrImpact(aoeEvent.TowerId, isMuzzle: false, aoeEvent.Point);
        }

        private static void CreateImpactVisual(EntityManager manager, TowerId towerId, float2 position, float2 direction)
        {
            Entity impactEntity = manager.CreateEntity();
            manager.SetName(impactEntity, "ImpactEvent");
            ImpactTimedEvent impactEvent = new() { TowerId = towerId, Direction = direction, Position = position, IsEnhanced = false, CurrentFrame = 0, AnimationTimer = 0 };
			manager.AddComponentData(impactEntity, impactEvent);
			MusicManager.PlayMuzzleOrImpact(impactEvent.TowerId, isMuzzle: false, position);
        }
		
        private static void CreateTagEffectVisual(SimpleEffectManager effectManager, TagEffectEvent tagEvent)
        {
            effectManager.TagEffectsDict.TryGetValue(tagEvent.EffectType, out IObjectPool<GameObject> tempPool);
            GameObject effectGo = tempPool?.Get();
            
            if(effectGo == null)
            {
                Debug.LogError($"Cant get TagEffectGO for {tagEvent.EffectType}");
                return;
            }
            
            switch (tagEvent.EffectType)
            {
                case TagEffectType.InstantKill:
                case TagEffectType.Quantum:
                    effectGo.GetComponent<ParticleSystemEffect>().Init(tempPool, tagEvent.Point, 0);
                    break;
                default:
                    effectGo.GetComponent<AOEImpactVisual>().Init(tempPool, tagEvent.Point, tagEvent.AoeRange);
                    break;
            }
        }
    }
}