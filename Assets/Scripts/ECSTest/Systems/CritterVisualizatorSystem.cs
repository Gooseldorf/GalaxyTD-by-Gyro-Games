using CardTD.Utilities;
using ECSTest.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Pool;

namespace ECSTest.Systems
{
    public partial struct CritterVisualizatorSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimpleEffectManager effectManager = GameServices.Instance.Get<SimpleEffectManager>();
            IObjectPool<GameObject> tempPool;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((CritterComponent critterComponent, PositionComponent position,DestroyComponent destroyComponent, Entity entity) in SystemAPI
                         .Query<CritterComponent, PositionComponent,DestroyComponent>().WithAbsent<Link2D>().WithEntityAccess())
            {
                if(destroyComponent.IsNeedToDestroy)
                    continue;
                
                effectManager.CrittersDict.TryGetValue(critterComponent.CritterType, out tempPool);
                // Debug.Log($"criters {position.Direction}");
                var link = new Link2D()
                {
                    Companion = GetCritter(position, tempPool),
                    Get = tempPool.Get,
                    Release = (x) => tempPool.Release(x.Companion),
                    IsHide = false,
                };
                ecb.AddComponent(entity, link);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        private static GameObject GetCritter(PositionComponent positionComponent, IObjectPool<GameObject> pool)
        {
            GameObject go = pool.Get();
            go.transform.position = positionComponent.Position.ToFloat3();
            go.SetActive(true);
            return go;
        }
    }
}