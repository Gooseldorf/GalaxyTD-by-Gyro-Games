using Unity.Entities;

namespace ECSTest.Components
{
    public struct NextWaveEvent : IComponentData
    {
        public int WaveNumber;
    }
}
