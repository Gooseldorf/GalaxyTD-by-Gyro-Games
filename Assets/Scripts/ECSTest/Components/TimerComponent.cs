using Unity.Entities;

namespace ECSTest.Components
{
    public struct TimerComponent : IComponentData
    {
        public readonly float InitialTime;
        public float Timer;

        public TimerComponent(float initialTime)
        {
            InitialTime = initialTime;
            Timer = 0;
        }
    }
}