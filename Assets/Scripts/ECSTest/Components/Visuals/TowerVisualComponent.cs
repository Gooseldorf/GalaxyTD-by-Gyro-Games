using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Pool;
using UnityEngine;

namespace ECSTest.Components
{
    public class TowerVisualComponent : IComponentData
    {
        public TowerVisual TowerVisual;

        public void LoadVisual(AllEnums.TowerId type, Entity towerEntity)
        {
            GameServices.Instance.Get<SimpleEffectManager>().TowersDict.TryGetValue(type, out IObjectPool<GameObject> pool);
            TowerVisual = pool.Get().GetComponent<TowerVisual>();
            TowerVisual.Init(towerEntity, pool);
        }

        public void UpdateVisual(float2 position, float2 direction)
        {
            if (TowerVisual == null)
                return;
            
            if (!math.isnan(position).x && !math.isnan(position).y)
                TowerVisual.transform.position = new float3(position.x, position.y, 0);
            TowerVisual.RotateVisual(direction);
        }
        
        public void ReleaseVisual()
        {
            TowerVisual.Release();
        }
    }
}