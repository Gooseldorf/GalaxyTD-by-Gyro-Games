using ECSTest.Components;
using ECSTest.Systems;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using System.Reflection;
using UnityEngine.UIElements;
using Unity.Mathematics;
using Unity.Entities.UniversalDelegates;
using Unity.Burst;
using TMPro;
using Unity.Transforms;
using System.Globalization;
using CardTD.Utilities;

namespace ECSTest.Systems
{
    // system for hydra, spawner, evolving
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct DynamicSpawnerSystem : ISystem
    {
        private NativeHashMap<int, Entity> creepsPrefabs;

        public static void Init(World world, Mission mission, HashSet<CreepStats> result)
        {
            SystemHandle spawnerHandle = world.Unmanaged.GetExistingUnmanagedSystem<DynamicSpawnerSystem>();
            ref DynamicSpawnerSystem dynamicSpawnerSystem = ref world.Unmanaged.GetUnsafeSystemRef<DynamicSpawnerSystem>(spawnerHandle);
            dynamicSpawnerSystem.Init(mission, result, new ManagerCreator(World.DefaultGameObjectInjectionWorld.EntityManager));
        }

        public static void Dispose(World world)
        {
            SystemHandle spawnerHandle = world.Unmanaged.GetExistingUnmanagedSystem<DynamicSpawnerSystem>();
            DynamicSpawnerSystem dynamicSpawnerSystem = world.Unmanaged.GetUnsafeSystemRef<DynamicSpawnerSystem>(spawnerHandle);
            dynamicSpawnerSystem.Dispose();
        }

        private void Init(Mission mission, HashSet<CreepStats> result, IEntityCreator creator)
        {
            creepsPrefabs = new NativeHashMap<int, Entity>(64, Allocator.Persistent);
            foreach (CreepStats creepStats in mission.CreepStatsPerWave)
                if (creepStats is DynamicSpawnerStats dynamicStats)
                    CheckUnit(dynamicStats.UnitToSpawn, creator, result);
        }

        private void CheckUnit(CreepStats creepStats, IEntityCreator creator, HashSet<CreepStats> result)
        {
            if (!creepsPrefabs.ContainsKey((int)creepStats.CreepType))
                CreatePrefab(creepStats, creator, result);

            if (creepStats is DynamicSpawnerStats dynamicStats)
                CheckUnit(dynamicStats.UnitToSpawn, creator, result);
        }

        private void CreatePrefab(CreepStats creepStats, IEntityCreator creator, HashSet<CreepStats> result)
        {
            result.Add(creepStats);

            SharedCreepData sharedCreepData = new(creepStats);

            Entity unit = SpawnerSystem.CreateBaseCreep(creator, creepStats, sharedCreepData, 0, float2.zero);

            creator.SetComponentEnabled<CreepComponent>(unit, true);
            creator.AddComponent(unit, new Prefab());

            creepsPrefabs.Add((int)creepStats.CreepType, unit);
        }

        private void Dispose()
        {
            if (creepsPrefabs.IsCreated)
                creepsPrefabs.Dispose();
        }

        public void OnCreate(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!creepsPrefabs.IsCreated) return;

            var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();

            new HydraJob()
            {
                Ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                CreepsPrefabs = creepsPrefabs
            }.ScheduleParallel();

            new SpawnerJob()
            {
                Ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                CreepsPrefabs = creepsPrefabs,
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();

            new EvolveJob()
            {
                Ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                CreepsPrefabs = creepsPrefabs,
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        public void OnDestroy(ref SystemState state) => Dispose();

        #region jobs
        [BurstCompile]
        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        private partial struct HydraJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public NativeHashMap<int, Entity> CreepsPrefabs;

            public void Execute([ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, ref HydraComponent hydraComponent, in DestroyComponent destroyComponent, in CreepComponent parent, in PositionComponent parentPosition)
            {
                if (destroyComponent.IsNeedToDestroy && !hydraComponent.WasSpawned)
                {
                    CreateUnitFromPrefab(
                       ref Ecb,
                       (chunkIndex * 128 + indexInChunk),
                       CreepsPrefabs,
                       hydraComponent.UnitToSpawn,
                       hydraComponent.SpawnUnitsCount,
                       hydraComponent.PecrentOfHpAndMass,
                       hydraComponent.PercentOfReward,
                       parentPosition,
                       parent);

                    hydraComponent.WasSpawned = true;
                }
            }
        }
        [BurstCompile]
        private partial struct SpawnerJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public NativeHashMap<int, Entity> CreepsPrefabs;
            [ReadOnly] public float DeltaTime;

            public void Execute([ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, ref SpawnerComponent spawnerComponent, in CreepComponent parent, in PositionComponent parentPosition)
            {
                spawnerComponent.Timer -= DeltaTime;
                if (spawnerComponent.Timer < 0)
                {
                    CreateUnitFromPrefab(
                        ref Ecb,
                        (chunkIndex * 128 + indexInChunk),
                        CreepsPrefabs,
                        spawnerComponent.UnitToSpawn,
                        spawnerComponent.SpawnUnitsCount,
                        spawnerComponent.PecrentOfHpAndMass,
                        spawnerComponent.PercentOfReward,
                        parentPosition,
                        parent);

                    spawnerComponent.Timer += spawnerComponent.TimeBetweenSpawn;
                }
            }
        }

        [BurstCompile]
        private partial struct EvolveJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public NativeHashMap<int, Entity> CreepsPrefabs;
            [ReadOnly] public float DeltaTime;

            public void Execute([ChunkIndexInQuery] int chunkIndex, ref EvolveComponent evolveComponent, ref DestroyComponent destroyComponent, in CreepComponent parent, in PositionComponent parentPosition)
            {
                if (destroyComponent.IsNeedToDestroy) return;

                evolveComponent.Timer -= DeltaTime;
                if (evolveComponent.Timer < 0)
                {
                    int sortKey = chunkIndex;

                    CreateUnitFromPrefab(
                        ref Ecb,
                        sortKey,
                        CreepsPrefabs,
                        evolveComponent.UnitToEvolveTo,
                        1,
                        evolveComponent.PecrentOfHpAndMass,
                        evolveComponent.PercentOfReward,
                        parentPosition,
                        parent,
                        true);

                    destroyComponent.IsNeedToDestroy = true;
                    destroyComponent.DestroyDelay = 2f;

                    Entity evolveEvent = Ecb.CreateEntity(sortKey);
                    Ecb.SetName(sortKey, evolveEvent, nameof(EvolveEvent));
                    Ecb.AddComponent(sortKey, evolveEvent, new EvolveEvent() { Position = parentPosition.Position });
                }
            }
        }

        private static void CreateUnitFromPrefab(ref EntityCommandBuffer.ParallelWriter ecb, int sortKey, NativeHashMap<int, Entity> creepsPrefabs, AllEnums.CreepType creepType, int count, float pecrentOfHpAndMass, float percentOfReward, PositionComponent parentPosition, CreepComponent parent, bool samePosition = false)
        {
            if (creepsPrefabs.TryGetValue((int)creepType, out Entity prefab))
            {
                for (int i = 0; i < count; i++)
                {
                    Entity unit = ecb.Instantiate(sortKey, prefab);

                    ecb.SetComponent(sortKey, unit, new PositionComponent()
                    {
                        Position = samePosition ? parentPosition.Position : GetPosition(parentPosition.Position, parentPosition.Direction, i, count),
                        Direction = parentPosition.Direction
                    });

                    ecb.SetComponent(sortKey, unit, new CreepComponent()
                    {
                        MaxHp = parent.MaxHp * pecrentOfHpAndMass,
                        Hp = parent.MaxHp * pecrentOfHpAndMass,
                        Escaped = false,
                        IsCaringRelic = false,
                        Mass = parent.Mass * pecrentOfHpAndMass,
                        CashReward = (int)(parent.CashReward * percentOfReward),
                        WaveNumber = parent.WaveNumber,
                        CashRewardMultiplayer = 1,
                    });
                }
            }
            else
            {
                Debug.LogError($"Can't find prefab for {creepType}");
            }

            float2 GetPosition(float2 position, float2 direction, int index, int count)
            {
                float angle = (360.0f / count) * index;
                float2 resultDirection = new float2((-direction.x * math.cos(angle)) - (-direction.y * math.sin(angle)), (-direction.x * math.sin(angle)) + (-direction.y * math.cos(angle)));
                return position + resultDirection;
            }
        }
        #endregion
    }
}
