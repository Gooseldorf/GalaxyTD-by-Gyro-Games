using Unity.Entities;
using ECSTest.Systems;
using Unity.Mathematics;
using Unity.Collections;

namespace ECSTest.Components
{
    public struct TimeSkipper : IComponentData, ICustomManaged<TimeSkipper>
    {
        private NativeArray<float> missionStartTime;
        private NativeArray<float> timeOffset;
        private NativeArray<int> wavesCount;
        private NativeArray<int> announcedWaveIndex;

        public bool IsCreated => announcedWaveIndex.IsCreated;

        public int AnnouncedWavesIndex
        {
            get => announcedWaveIndex[0];
            set => announcedWaveIndex[0] = value;
        }

        public float MissionStartTime
        {
            get => this.missionStartTime[0];
            private set => this.missionStartTime[0] = value;
        }

        public float TimeOffset
        {
            get => this.timeOffset[0];
            private set => this.timeOffset[0] = value;
        }

        public int WavesCount
        {
            get => this.wavesCount[0];
            private set => this.wavesCount[0] = value;
        }

        public void Dispose()
        {
            wavesCount.Dispose();
            timeOffset.Dispose();
            missionStartTime.Dispose();
            announcedWaveIndex.Dispose();
        }

        public TimeSkipper(float missionStartTime, int wavesCount)
        {
            this.missionStartTime = new NativeArray<float>(1, Allocator.Persistent);
            this.missionStartTime[0] = missionStartTime;

            timeOffset = new NativeArray<float>(1, Allocator.Persistent);
            timeOffset[0] = 0;

            this.wavesCount = new NativeArray<int>(1, Allocator.Persistent);
            this.wavesCount[0] = wavesCount;

            announcedWaveIndex = new NativeArray<int>(1, Allocator.Persistent);
            announcedWaveIndex[0] = -1;
        }

        public int CurrentWave(float elapsedTime)
        {
            // whole time passed since spawn start
            float timePassed = elapsedTime + TimeOffset - MissionStartTime - SpawnerSystem.FirstWaveOffset;
            // estimated wave index based on passed time
            int estimatedWave = (int)(timePassed / (SpawnerSystem.WaveTimeLength + SpawnerSystem.PauseBetweenWaves));
            // check if it is last wave
            if (GameServices.Instance.IsRoguelike)
                return estimatedWave;
            return math.min(estimatedWave, WavesCount - 1);
        }

        public bool CanSkip(float elapsedTime)
        {
            // calculate current wave index
            int currentWave = CurrentWave(elapsedTime);
            // if it is last wave we cant skip
            if (currentWave + 1 >= WavesCount)
                return false;
            // calculate next wave time
            float waveStartTime = WaveStartTime(currentWave + 1);
            // if we are waiting between waves we can skip
            return waveStartTime - elapsedTime - TimeOffset < SpawnerSystem.PauseBetweenWaves;
        }

        public float WaveStartTime(int waveIndex) => MissionStartTime + SpawnerSystem.FirstWaveOffset + waveIndex * (SpawnerSystem.WaveTimeLength + SpawnerSystem.PauseBetweenWaves);

        public float CurrentTime(float elapsedTime) => elapsedTime + TimeOffset - MissionStartTime;

        /// <returns>Can be Infinity if it is last wave</returns>
        public float TimeToNextWave(float elapsedTime)
        {
            // calculate current wave index
            int currentWave = CurrentWave(elapsedTime);
            // if it is last wave
            if (currentWave + 1 >= WavesCount)
                return float.PositiveInfinity;

            return WaveStartTime(currentWave + 1) - elapsedTime - TimeOffset;
        }

        public void SkipTime(float elapsedTime)
        {
            int currentWave = CurrentWave(elapsedTime);
            float waveStartTime = WaveStartTime(currentWave + 1);
            float timeToSkip = waveStartTime - elapsedTime - TimeOffset;
            TimeOffset += timeToSkip;
        }

        public void SkipFirstWaveOffset(float elapsedTime)
        {
            float actualSpawnTime = SpawnerSystem.FirstWaveOffset - SpawnerSystem.FirstWaveSpawnOffset;
            float currentTime = CurrentTime(elapsedTime);
            if (currentTime < actualSpawnTime)
                TimeSkip(actualSpawnTime - currentTime);
        }

        public void TimeSkip(float time)
        {
            TimeOffset += time;
        }

        public TimeSkipper Clone()
        {
            return new TimeSkipper()
            {
                missionStartTime = new NativeArray<float>(missionStartTime, Allocator.Persistent),
                timeOffset = new NativeArray<float>(timeOffset, Allocator.Persistent),
                wavesCount = new NativeArray<int>(wavesCount, Allocator.Persistent),
                announcedWaveIndex = new NativeArray<int>(announcedWaveIndex, Allocator.Persistent)
            };
        }

        public void Load(TimeSkipper from)
        {
            missionStartTime.CopyFrom(from.missionStartTime);
            timeOffset.CopyFrom(from.timeOffset);
            wavesCount.CopyFrom(from.wavesCount);
            announcedWaveIndex.CopyFrom(from.announcedWaveIndex);
        }
    }
}