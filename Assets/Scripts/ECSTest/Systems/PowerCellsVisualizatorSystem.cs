using CardTD.Utilities;
using ECSTest.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Pool;

namespace ECSTest.Systems
{
    public partial struct PowerCellsVisualizatorSystem: ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SimpleEffectManager effectManager = GameServices.Instance.Get<SimpleEffectManager>();
            
            EntityCommandBuffer ecb = new (Allocator.Temp);

            foreach ((PowerCellComponent powerCellComponent, PositionComponent position,DestroyComponent destroyComponent, Entity entity) in SystemAPI
                         .Query<PowerCellComponent, PositionComponent,DestroyComponent>().WithAbsent<Link2D>().WithEntityAccess())
            {
                if(destroyComponent.IsNeedToDestroy)
                    continue;
                if (!powerCellComponent.IsMoves) continue;
                
                Link2D link = new ()
                {
                    Companion = GetPowerCell(position, effectManager.PowerCellMovePool),
                    Get = () => effectManager.PowerCellMovePool.Get(),
                    Release = (x) => effectManager.PowerCellMovePool.Release(x.Companion),
                    IsHide = false,
                };
                ecb.AddComponent(entity, link);
            }
            
            foreach ((PowerCellComponent powerCellComponent, Link2D link, Entity entity) in SystemAPI
                         .Query<PowerCellComponent, Link2D>().WithEntityAccess())
            {
                if (!powerCellComponent.IsMoves)
                {
                    ecb.RemoveComponent<Link2D>(entity);
                }
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        private static GameObject GetPowerCell(PositionComponent positionComponent, IObjectPool<GameObject> pool)
        {
            GameObject go = pool.Get();
            go.transform.position = positionComponent.Position.ToFloat3();
            return go;
        }
    }
}