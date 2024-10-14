using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECSTest.Components
{
    public class ProjectileVisualComponent : IComponentData
    {
        public ProjectileVisual Visual;

        private bool init;

        //public async void LoadVisual(AllEnums.TowerId towerId, PositionComponent positionComponent)
        //{
        //    init = true;
        //    //TODO : Need remove
        //    Tower tower = new() {TowerId = towerId};
        //    Visual = await GameServices.Instance.Get<EffectVisualManager>().TryGetTowerProjectile(tower);

        //    if (init)
        //    {
        //        Visual.Move(positionComponent.Position, positionComponent.Direction);
        //        Visual.Init();
        //    }
        //    else
        //        Visual.Hide();
        //}

        //public void DeInit()
        //{
        //    if (Visual != null)
        //        Visual.Hide();
        //    else
        //        Debug.LogError("Fast die projectile");
        //    init = false;
        //}

        //public void UpdateVisual(float2 position, float2 direction)
        //{
        //    if (Visual == null)
        //        return;
        //    Visual.Move(position, direction);
        //}
    }
}