using CardTD.Utilities;
using ECSTest.Components;
using System;
using System.Collections.Generic;
using UI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static AllEnums;
using Random = UnityEngine.Random;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MovingSystem))]
    public partial struct SpawnerSystem : ISystem
    {
        public const float WaveTimeLength = 30;
        public const float PauseBetweenWaves = 5;
        public const float SpawnZoneVisualizatorOffset = -2;
        public const float FirstWaveSpawnOffset = 5f;
        public const float FirstWaveOffset = 10000f;
        //Higher value - Lower Mass
        public const int MassToHpConversion = 150;

        private double lastTime;

        private static Mission currentMission;
        private bool isRoguelike;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeSkipper>();
            state.RequireForUpdate<CashComponent>();
        }

        // [BurstCompile(CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            TimeSkipper timeSkipper = SystemAPI.GetSingleton<TimeSkipper>();

            CheckRestart(ref state, timeSkipper);

            lastTime = SystemAPI.Time.ElapsedTime;
            isRoguelike = GameServices.Instance.IsRoguelike;

            double elapsedTime = lastTime + timeSkipper.TimeOffset;
            BeginFixedStepSimulationEntityCommandBufferSystem.Singleton singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer buffer = singleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new SpawnJob()
            {
                ElapsedTime = elapsedTime,
                EntityCommandBuffer = buffer.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            CheckNextWaveEvent(ref state, timeSkipper, buffer, elapsedTime);
        }

        private void CheckNextWaveEvent(ref SystemState state, TimeSkipper timeSkipper, EntityCommandBuffer buffer, double elapsedTime)
        {
            if (timeSkipper.AnnouncedWavesIndex >= timeSkipper.WavesCount && !isRoguelike)
                return;

            if (!(elapsedTime > timeSkipper.WaveStartTime(timeSkipper.AnnouncedWavesIndex + 1) - UIHelper.WaveAnnouncementOffset))
                return;

            //Debug.Log($"{elapsedTime} > {timeSkipper.WaveStartTime(timeSkipper.AnnouncedWavesIndex + 1)} index {timeSkipper.AnnouncedWavesIndex + 1}  offset {UIHelper.WaveAnnouncementOffset}");

            timeSkipper.AnnouncedWavesIndex++;
            //Debug.Log($"index after increment : {timeSkipper.AnnouncedWavesIndex}");

            state.Dependency = new NextWaveEventJob()
            {
                EntityCommandBuffer = buffer,
                CashComponent = SystemAPI.GetSingletonRW<CashComponent>().ValueRW,
                WaveIndex = timeSkipper.AnnouncedWavesIndex
            }
                .Schedule(state.Dependency);

            int countPrevWave = 1;
            if (timeSkipper.AnnouncedWavesIndex < timeSkipper.WavesCount - countPrevWave || !isRoguelike)
                return;


            // ������� ������ ��� �������� � ��������� �����(��� ������ ������ ���� �������������� ��������)

            //Debug.Log("spawn new units");

            //SpawnNewWave(timeSkipper, ref state, countPrevWave);
            //state.EntityManager.CompleteDependencyBeforeRW<CashComponent>();
            //RefRW<CashComponent> cashComponent = SystemAPI.GetSingletonRW<CashComponent>();
            //cashComponent.ValueRW.AddCashPerStartWave((int)(cashComponent.ValueRO.GetLastCashPerWaveStart * 1.2f));
        }


        private void SpawnNewWave(TimeSkipper timeSkipper, ref SystemState state, int countPrevWave)
        {
            ECBCreator creator = new ECBCreator(new EntityCommandBuffer(Allocator.Temp));

            int spawnIndex = (timeSkipper.AnnouncedWavesIndex + countPrevWave) % timeSkipper.WavesCount;
            int iterationCounter = ((timeSkipper.AnnouncedWavesIndex + countPrevWave) / timeSkipper.WavesCount);
            float creepTypeHpModifer = currentMission.GetRoguelikeHpModifer(spawnIndex, iterationCounter);

            float spawnTime = timeSkipper.WaveStartTime(timeSkipper.AnnouncedWavesIndex + countPrevWave);

            foreach (SpawnGroup spawnGroup in currentMission.SpawnData)
            {
                foreach (Wave wave in spawnGroup.Waves)
                {
                    if (wave.WaveNum != spawnIndex)
                        continue;
                    CreateUnits(currentMission.CreepStatsPerWave[spawnIndex], wave, spawnGroup, creator, spawnTime, new(), creepTypeHpModifer);
                }
            }

            creator.Playback(state.World.EntityManager);
            creator.Dispose();
        }


        private void CheckRestart(ref SystemState state, TimeSkipper timeSkipper)
        {
            if (timeSkipper.AnnouncedWavesIndex > -1 && lastTime > SystemAPI.Time.ElapsedTime)
            {
                Debug.Log("CheckRestart");
                timeSkipper.AnnouncedWavesIndex = -1;
            }
        }

        public static HashSet<CreepStats> Init(Mission mission, World world)
        {
            currentMission = mission;

            ECBCreator creator = new ECBCreator(new EntityCommandBuffer(Allocator.Temp));

            TimeSkipper timeSkipper = new TimeSkipper((float)world.Time.ElapsedTime, mission.WavesCount);
            world.EntityManager.CreateSingleton(timeSkipper);

            HashSet<CreepStats> result = new();

            float spawnTime;

            for (int index = 0; index < mission.SpawnData.Length; index++)
            {
                SpawnGroup spawnGroup = mission.SpawnData[index];
                for (int j = 0; j < spawnGroup.Waves.Count; ++j)
                {
                    Wave wave = spawnGroup.Waves[j];
                    spawnTime = timeSkipper.WaveStartTime(wave.WaveNum);

                    if (mission.CreepStatsPerWave.Count < wave.WaveNum)
                        throw new Exception($"mission.CreepStatsPerWave.Count {mission.CreepStatsPerWave.Count} < wave index: {wave.WaveNum}");

                    CreateUnits(mission.CreepStatsPerWave[wave.WaveNum], wave, spawnGroup, creator, spawnTime, result, mission.HpModifier, mission.MassModifier);
                }
            }

            creator.Playback(world.EntityManager);
            creator.Dispose();

            return result;
        }

        private static void CreateUnits(CreepStats creepStats, Wave wave, SpawnGroup spawnGroup, IEntityCreator creator, float spawnTime, HashSet<CreepStats> result, float missionHpModifier, float missionMassModifier = 1)
        {
            result.Add(creepStats);

            SharedCreepData sharedCreepData = new(creepStats);

            float timeBetweenCreeps = (WaveTimeLength - wave.ExtraTime) / wave.Count;
            float lastSpawnTime = 0;

            float hp = wave.CreepHp * missionHpModifier;
            int digits = (int)math.log10(hp) + 1;
            if (digits > 1)
            {
                float temp = hp / math.pow(10, digits - 2);
                temp = math.round(temp);
                hp = (int)(temp * math.pow(10, digits - 2));
            }

            for (int i = 0; i < wave.Count; i++)
            {
                lastSpawnTime += timeBetweenCreeps;

                float2 position = Utilities.GetRandomPosition(spawnGroup.SpawnPositions) + new float2(.5f, .5f);
                Entity unit = CreateBaseCreep(creator, creepStats, sharedCreepData, hp, position, wave.CashReward, wave.WaveNum, missionMassModifier);

                creator.SetComponentEnabled<CreepComponent>(unit, false);

                creator.AddComponent(unit, new SpawnComponent() { SpawnTime = spawnTime + lastSpawnTime });
                creator.SetComponentEnabled<SpawnComponent>(unit, true);
            }
        }

        public static Entity CreateBaseCreep(IEntityCreator creator, CreepStats creepStats, SharedCreepData sharedCreepData, float maxHp, float2 position, int cashReward = 0, int waveNum = 0, float missionMassModifier = 0)
        {
            Entity unit = creator.CreateEntity();
            creator.SetName(unit, $"Unit_{creepStats.CreepType}");
            creator.AddComponent(unit, new PositionComponent() { Position = position, Direction = float2.zero });
            creator.AddComponent(unit, new Movable() { IsGoingIn = true, MoveSpeedModifer = 1f });

            creator.AddComponent(unit, new CreepComponent()
            {
                MaxHp = maxHp,
                Hp = maxHp,
                Escaped = false,
                IsCaringRelic = false,
                Mass = creepStats.Mass * maxHp / MassToHpConversion * missionMassModifier,
                CashReward = cashReward,
                WaveNumber = waveNum,
                CashRewardMultiplayer = 1,
            });

            creator.AddComponent(unit, new RoundObstacle() { Range = creepStats.CollisionRange });

            creator.AddComponent(unit, new Knockback());
            creator.AddComponent(unit, new SlowComponent() { Percent = 0, Time = 0 });
            creator.AddComponent(unit, new StunComponent() { Time = 0 });
            creator.AddComponent(unit, new RadiationComponent() { DPS = 0, Time = 0 });
            creator.AddComponent(unit, new FearComponent() { Time = 0 });

            creator.AddComponent(unit, new DestroyComponent() { IsNeedToDestroy = false });

            creator.AddSharedComponent(unit, sharedCreepData);

            switch (creepStats)
            {
                case HydraCreepStats hydraStats:
                    creator.AddComponent(unit, new HydraComponent()
                    {
                        UnitToSpawn = hydraStats.UnitToSpawn.CreepType,
                        SpawnUnitsCount = hydraStats.SpawnUnitsCount,
                        PecrentOfHpAndMass = hydraStats.PecrentOfHpAndMass,
                        PercentOfReward = hydraStats.PercentOfReward,
                        WasSpawned = false
                    });
                    break;
                case SpawnerCreepStats spawnerCreepStats:
                    creator.AddComponent(unit, new SpawnerComponent()
                    {
                        UnitToSpawn = spawnerCreepStats.UnitToSpawn.CreepType,
                        SpawnUnitsCount = spawnerCreepStats.SpawnUnitsCount,
                        PecrentOfHpAndMass = spawnerCreepStats.PecrentOfHpAndMass,
                        PercentOfReward = spawnerCreepStats.PercentOfReward,
                        TimeBetweenSpawn = spawnerCreepStats.Delay,
                        Timer = spawnerCreepStats.Delay
                    });
                    break;
                case EvolveCreepStats evolveCreepStats:
                    creator.AddComponent(unit, new EvolveComponent
                    {
                        UnitToEvolveTo = evolveCreepStats.UnitToSpawn.CreepType,
                        PecrentOfHpAndMass = evolveCreepStats.PecrentOfHpAndMass,
                        PercentOfReward = evolveCreepStats.PercentOfReward,
                        Timer = evolveCreepStats.Delay
                    });
                    break;
                case TeleportCreepStats teleportCreepStats:
                    creator.AddComponent(unit, new TeleportationMovable
                    {
                        MaxTime = teleportCreepStats.WaiteTime,
                        JumpTime = Random.Range(0, teleportCreepStats.WaiteTime),
                        MaxCountJumps = teleportCreepStats.MaxJumpLenght,
                        MinCountJumps = teleportCreepStats.MinJumpLenght,
                    });
                    break;
            }

            return unit;
        }

        [BurstCompile]
        private struct NextWaveEventJob : IJob
        {
            internal EntityCommandBuffer EntityCommandBuffer;
            public CashComponent CashComponent;
            [ReadOnly] public int WaveIndex;

            public void Execute()
            {
                Entity nextWaveEvent = EntityCommandBuffer.CreateEntity();
                EntityCommandBuffer.SetName(nextWaveEvent, nameof(NextWaveEvent));
                EntityCommandBuffer.AddComponent(nextWaveEvent, new NextWaveEvent() { WaveNumber = WaveIndex });

                if (WaveIndex > 0)
                {
                    CashComponent.WaveStart(WaveIndex, EntityCommandBuffer);
                }
            }
        }

        [BurstCompile]
        private partial struct SpawnJob : IJobEntity
        {
            [ReadOnly] public double ElapsedTime;

            internal EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

            public void Execute([ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, in SpawnComponent spawnComponent, Entity entity)
            {
                if (spawnComponent.SpawnTime < ElapsedTime)
                {
                    int sortKey = chunkIndex;

                    EntityCommandBuffer.SetComponentEnabled<SpawnComponent>(sortKey, entity, false);
                    EntityCommandBuffer.SetComponentEnabled<CreepComponent>(sortKey, entity, true);

                    Entity spawnEvent = EntityCommandBuffer.CreateEntity(sortKey);
                    EntityCommandBuffer.SetName(sortKey, spawnEvent, nameof(SpawnEvent));
                    EntityCommandBuffer.AddComponent(sortKey, spawnEvent, new SpawnEvent());
                }
            }
        }
    }
}