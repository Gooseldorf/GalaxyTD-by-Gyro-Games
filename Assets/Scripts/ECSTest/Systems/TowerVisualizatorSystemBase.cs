using ECSTest.Components;
using Unity.Collections;
using Unity.Entities;

namespace ECSTest.Systems
{
    //[DisableAutoCreation]
    //[UpdateBefore(typeof(MovingSystemBase))]
    public partial struct TowerVisualizatorSystemBase : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((AttackerComponent attackerComponent, Entity entity) in SystemAPI
                         .Query<AttackerComponent>().WithAbsent<TowerVisualComponent>().WithEntityAccess())
            {
                var component = new TowerVisualComponent();
                component.LoadVisual(attackerComponent.TowerType, entity);
                ecb.AddComponent(entity, component);
            }

            foreach ((PositionComponent positionComponent, TowerVisualComponent visualComponent) in SystemAPI
                         .Query<PositionComponent, TowerVisualComponent>())
            {
                visualComponent.UpdateVisual(positionComponent.Position, positionComponent.Direction);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}