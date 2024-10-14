using DefaultNamespace;
using ECSTest.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ECSTest.Systems
{
    [UpdateAfter(typeof(VisualizatorSystemBase))]
    public partial struct SpawnZoneVisualizator : ISystem
    {
        private static NativeArray<int2> spawnZonePositions;
        private static NativeArray<Entity> spawnZoneEntities;

        private EntityQuery spawnZoneQuery;
        
        private static NativeArray<int> spawnZonesCreepCount;
        private static NativeArray<AllEnums.CreepType> spawnZonesCreepType;

        private EntityManager entityManager;
        private int spawnZoneCount;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeSkipper>();

            spawnZoneQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpawnZoneComponent, PositionComponent, EnvironmentVisualComponent>()
                .Build(ref state);
        }

        public static void DisposeArrays()
        {
            if (spawnZoneEntities.IsCreated)
                spawnZoneEntities.Dispose();
            if (spawnZonePositions.IsCreated)
                spawnZonePositions.Dispose();

            if (spawnZonesCreepCount.IsCreated)
                spawnZonesCreepCount.Dispose();
            if (spawnZonesCreepType.IsCreated)
                spawnZonesCreepType.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!spawnZoneEntities.IsCreated)
            {
                spawnZoneCount = spawnZoneQuery.CalculateEntityCount();
                if (spawnZoneCount > 0)
                    CreateSpawnZoneHashMap();
                else
                    return;
            }

            for (int i = 0;i < spawnZoneCount;i++)
                spawnZonesCreepCount[i] = 0;

            TimeSkipper timeSkipper = SystemAPI.GetSingleton<TimeSkipper>();
            int currentWaveIndex = timeSkipper.CurrentWave((float)SystemAPI.Time.ElapsedTime + SpawnerSystem.PauseBetweenWaves + SpawnerSystem.SpawnZoneVisualizatorOffset);
             float nextWaveTime = currentWaveIndex <= 0 ? timeSkipper.WaveStartTime(1) : //currentWaveIndex <= 0 appears before start button pressed
                timeSkipper.WaveStartTime(currentWaveIndex + 1);
            
            JobHandle jh = new CalculateWaveSpawnUnitJob()
            {
                SpawnZonePositions = spawnZonePositions,
                CreepType = spawnZonesCreepType,
                CreepCount = spawnZonesCreepCount,
                NextWaveTime = nextWaveTime,
                SpawnZonesCount = spawnZoneCount
            }.Schedule(state.Dependency);
            jh.Complete();

            //update icon
            for (int i = 0; i < spawnZoneCount; i++)
            {
                if (!entityManager.HasComponent<EnvironmentVisualComponent>(spawnZoneEntities[i]))
                {
                    Debug.LogError("Has no EnvironmentVisual");
                    continue;
                }

                EnvironmentVisual spawnZoneVisual = entityManager.GetComponentData<EnvironmentVisualComponent>(spawnZoneEntities[i]).EnvironmentVisual;
                if (spawnZoneVisual != null)
                    (spawnZoneVisual as SpawnZonePartialVisual).UpdateIncomingCreeps(spawnZonesCreepType[i], spawnZonesCreepCount[i]);
            }

            //update creepCount
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            var textAnimationData = GameServices.Instance.RenderDataHolder.TextAnimationData.GetTextAnimationData;
            for (int i = 0; i < spawnZoneCount; i++)
            {
                var posOffset = new float2(textAnimationData.SpawnZoneXStartOffset, textAnimationData.SpawnZoneYStartOffset);

                for (int k = 1; k < 5; k++)//dirty
                {
                    if (spawnZonesCreepCount[i] >= math.pow(10, k))
                        posOffset += new float2(textAnimationData.DropZoneXStartOffset, 0);
                    else
                        break;
                }

                ecb.AddComponent(spawnZoneEntities[i],
                new AnimatedTextComponent()
                {
                    NonCashValue = spawnZonesCreepCount[i],
                    Position = spawnZonePositions[i] + posOffset,
                    Timer = 0,
                    Color = textAnimationData.SpawnZoneTextColor,
                    TextType = AllEnums.TextType.SpawnZone,
                    Scale = textAnimationData.SpawnZoneScale
                });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void CreateSpawnZoneHashMap()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            spawnZoneEntities = spawnZoneQuery.ToEntityArray(Allocator.Persistent);
            spawnZonePositions = new NativeArray<int2>(spawnZoneCount, Allocator.Persistent);
            spawnZonesCreepCount = new NativeArray<int>(spawnZoneCount, Allocator.Persistent);
            spawnZonesCreepType = new NativeArray<AllEnums.CreepType>(spawnZoneCount, Allocator.Persistent);

            NativeArray<PositionComponent> tempSpawnZonePositions = spawnZoneQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);

            for (int i = 0; i < spawnZoneCount; i++)
                spawnZonePositions[i] = (int2)tempSpawnZonePositions[i].Position;

            tempSpawnZonePositions.Dispose();
        }

        public void OnDestroy(ref SystemState state) => DisposeArrays();

        [BurstCompile(CompileSynchronously = true)]
        private partial struct CalculateWaveSpawnUnitJob : IJobEntity
        {
            [ReadOnly] public NativeArray<int2> SpawnZonePositions;
            [WriteOnly] public NativeArray<AllEnums.CreepType> CreepType;
            public NativeArray<int> CreepCount;
            [ReadOnly] public double NextWaveTime;
            [ReadOnly] public int SpawnZonesCount;

            public void Execute(in SpawnComponent spawnComponent,in PositionComponent positionComponent,in SharedCreepData creepData)
            {
                if (spawnComponent.SpawnTime < NextWaveTime)
                {
                    int index = GetSpawnZoneIndex(positionComponent.Position, SpawnZonePositions);
                    if (index != -1)
                    {
                        CreepCount[index]++;
                        CreepType[index] = creepData.CreepType;
                    }
                    else
                    {
                        Debug.LogError("SpawnZoneVisualizator: Execute: index = -1");
                    }
                }
            }

            private int GetSpawnZoneIndex(float2 position, NativeArray<int2> spawnZonesPositions)
            {
                int2 positionInt = (int2)position;
                for (int i = 0; i < SpawnZonesCount; i++)
                {
                    if (spawnZonesPositions[i].Equals(positionInt)
                        || (spawnZonesPositions[i].x - 1 == positionInt.x && spawnZonesPositions[i].y == positionInt.y)
                        || (spawnZonesPositions[i].x == positionInt.x && spawnZonesPositions[i].y - 1 == positionInt.y)
                        || (spawnZonesPositions[i] - 1).Equals(positionInt))
                    {
                        return i;
                    }
                }
                return -1;
            }
        }
    }
}