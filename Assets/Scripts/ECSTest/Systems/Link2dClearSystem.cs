using ECSTest.Components;
using Systems;
using Unity.Entities;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(RemoveEventSystem))]
    public partial struct Link2dClearSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var linkData in SystemAPI.Query<Link2D, DestroyComponent>())
            {
                if (!linkData.Item2.IsNeedToDestroy || linkData.Item1.IsHide)
                    continue;
                linkData.Item1.Hide();
            }
        }
    }
}