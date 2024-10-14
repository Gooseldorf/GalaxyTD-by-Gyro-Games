using ECSTest.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ECSTest.Authorings
{
    public class RandomAuthoring : MonoBehaviour
    {
        public uint Seed;
    }

    public class RandomBaker : SimpleBaker<RandomAuthoring>
    {
        protected override void OnEntityCreated(Entity entity, EntityManager manager, RandomAuthoring authoring)
        {
            var randoms = new NativeArray<Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent);

            for (int i = 0; i < randoms.Length; i++)
            {
                randoms[i] = new Random((uint)(authoring.Seed + i));
            }

            manager.AddComponentData(entity, new RandomComponent {Randoms = randoms,});
        }
    }
}