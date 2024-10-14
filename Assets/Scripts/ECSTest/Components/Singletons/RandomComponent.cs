using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct RandomComponent : IComponentData, ICustomManaged<RandomComponent>
    {
        //TODO: create from init
        [NativeDisableParallelForRestriction] public NativeArray<Random> Randoms;

        public RandomComponent Clone() => new RandomComponent() { Randoms = new NativeArray<Random>(Randoms, Allocator.Persistent) };

        public void Load(RandomComponent from) => Randoms.CopyFrom(from.Randoms);

        public void Dispose() => Randoms.Dispose();

        public Random GetRandom(int threadIndex) => Randoms[threadIndex];

        public void SetRandom(Random random, int threadIndex)
        {
            Randoms[threadIndex] = random;
        }


    }
}