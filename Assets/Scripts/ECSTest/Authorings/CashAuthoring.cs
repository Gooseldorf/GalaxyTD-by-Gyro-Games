using ECSTest.Components;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Authorings
{
    public class CashAuthoring : MonoBehaviour
    {
        public List<int> CashPerWaveStart = new() {100};
    }

    public class CashBaker : SimpleBaker<CashAuthoring>
    {
        protected override void OnEntityCreated(Entity entity, EntityManager manager, CashAuthoring authoring)
        {
            manager.AddComponentData(entity, new CashComponent(authoring.CashPerWaveStart));
        }
    }
}