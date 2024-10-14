using Unity.Entities;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial struct TileDecalSystemEnd : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            TileDecalSystem tds = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TileDecalSystem>();
            tds.CompleteRender();
        }
    }
}
