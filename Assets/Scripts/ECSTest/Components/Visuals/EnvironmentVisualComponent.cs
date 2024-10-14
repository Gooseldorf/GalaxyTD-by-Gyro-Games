using Unity.Entities;

namespace ECSTest.Components
{
    public class EnvironmentVisualComponent : IComponentData
    {
        public EnvironmentVisual EnvironmentVisual;

        public void AddVisual(EnvironmentVisual environmentVisual) => EnvironmentVisual = environmentVisual;
    }
}