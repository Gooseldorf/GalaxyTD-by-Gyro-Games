using CardTD.Utilities;
using ECSTest.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct CritterSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RandomComponent>();
            state.RequireForUpdate<BaseFlowField>();
            state.RequireForUpdate<CashComponent>();
        }

        public static void Init(Mission mission, Cell[,] cells, World world, CritterStats critterStats)
        {
            if (world == null)
            {
                Debug.LogError("World is null");
                return;
            }

            EntityManager manager = world.EntityManager;
            int mapSizeX = cells.GetLength(0);
            int mapSizeY = cells.GetLength(1);
            int2 tempPos;
            int findPositionTryIndex, tempX, tempY;

            for (int i = 0; i < mission.MinCritterCountOnStart - mission.CritterSpawnPoints.Length; i++)
            {
                findPositionTryIndex = 0;
                tempPos = 0;
                while (findPositionTryIndex < 100)
                {
                    tempX = Random.Range(0, mapSizeX);
                    tempY = Random.Range(0, mapSizeY);
                    if (!cells[tempX, tempY].IsWall)
                    {
                        tempPos = new(tempX, tempY);
                        break;
                    }
                    findPositionTryIndex++;
                }

                CritterSpawnPoint spawnPoint = new()
                {
                    Direction = 1,
                    CritterStats = critterStats,
                    GridPos = tempPos,
                    GridSize = 1,
                };
                SpawnCritter(spawnPoint, manager);
            }

            foreach (var spawnPoint in mission.CritterSpawnPoints)
                SpawnCritter(spawnPoint, manager);

            void SpawnCritter(CritterSpawnPoint spawnPoint, EntityManager manager)
            {
                Entity entity = manager.CreateEntity();
                manager.SetName(entity, "Critter");
                manager.AddComponentData(entity, new CritterComponent()
                {
                    IsMoving = false,
                    IsRotating = false,
                    MovingSpeed = spawnPoint.CritterStats.Speed,
                    RotationSpeed = spawnPoint.CritterStats.RotationSpeed,
                    CritterType = spawnPoint.CritterStats.CritterType,
                    Radius = spawnPoint.CritterStats.Radius,
                    CleaningQuality = spawnPoint.CritterStats.CleaningQuality,
                    SearchTargetPositionMaxRadius = spawnPoint.CritterStats.SearchTargetPositionMaxRadius
                });
                manager.AddComponentData(entity, new PositionComponent() { Position = spawnPoint.GridPos + new float2(0.5f), Direction = new float2(0, 1) });
                manager.AddComponentData(entity, new DestroyComponent { IsNeedToDestroy = false });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            state.Dependency = new CritterMovementJob()
            {
                BaseFlowField = SystemAPI.GetSingleton<BaseFlowField>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                RandomComponent = SystemAPI.GetSingleton<RandomComponent>(),
                EntityCommandBuffer = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new AddCashJob()
            {
                CashComponent = SystemAPI.GetSingletonRW<CashComponent>().ValueRW,
                EntityCommandBuffer = singleton.CreateCommandBuffer(state.WorldUnmanaged),
            }.Schedule(state.Dependency);
        }

        [BurstCompile(CompileSynchronously = true)]
        public partial struct AddCashJob : IJobEntity
        {
            public CashComponent CashComponent;
            internal EntityCommandBuffer EntityCommandBuffer;

            private void Execute(ref DestroyComponent destroy, in CritterCashEvent cash, in PositionComponent position)
            {
                if (destroy.IsNeedToDestroy)
                    return;
                destroy.IsNeedToDestroy = true;
                destroy.DestroyDelay = 2;
                CashComponent.AddCashForCritterDie(EntityCommandBuffer, position.Position);
            }
        }


        [BurstCompile]
        public partial struct CritterMovementJob : IJobEntity
        {
            [ReadOnly] public BaseFlowField BaseFlowField;
            public RandomComponent RandomComponent;
            public float DeltaTime;
            internal EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

            public void Execute([ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, ref PositionComponent position, ref CritterComponent critter,
                ref DestroyComponent destroyComponent)
            {
                if (destroyComponent.IsNeedToDestroy)
                    return;

                if (IsCritterPositionInsideWall(position.Position, critter.Radius))
                {
                    int sortKey = chunkIndex;
                    Entity cashEventEntity = EntityCommandBuffer.CreateEntity(sortKey);

                    EntityCommandBuffer.AddComponent(sortKey, cashEventEntity, new CritterCashEvent());
                    EntityCommandBuffer.AddComponent(sortKey, cashEventEntity, new DestroyComponent { IsNeedToDestroy = false });
                    EntityCommandBuffer.AddComponent(sortKey, cashEventEntity, new PositionComponent { Position = position.Position });

                    destroyComponent.IsNeedToDestroy = true;
                    destroyComponent.DestroyDelay = 2;
                    return;
                }

                //Check if we cantMove in old direction or moving too long
                if (!critter.IsMoving && !critter.IsRotating)
                {
                    critter.TargetDirection = Utilities.GetRandomDirection(RandomComponent);
                    critter.IsMoving = false;
                    critter.IsRotating = true;
                }

                if (critter.IsRotating)
                {
                    float angleToTarget = Utilities.SignedAngleBetween(position.Direction, critter.TargetDirection);

                    //Check if we already in rightDirection
                    if (math.abs(angleToTarget) < critter.RotationSpeed * DeltaTime)
                    {
                        position.Direction = critter.TargetDirection;
                        critter.IsRotating = false;
                        critter.IsMoving = true;
                    }
                    else
                    {
                        //Continue rotating
                        float angle = math.sign(angleToTarget) * critter.RotationSpeed * DeltaTime;
                        position.Direction = position.Direction.GetRotated(angle);
                    }
                }

                if (critter.IsMoving)
                {
                    var step = position.Direction * critter.MovingSpeed * DeltaTime;
                    if (IsCritterPositionInsideWall(position.Position + step, critter.Radius))
                        critter.IsMoving = false;
                    else
                        position.Position += step;
                }
            }

            private bool IsCritterPositionInsideWall(float2 position, float critterRadius)
            {
                for (int x = math.max((int)(position.x - critterRadius), 0); x <= position.x + critterRadius; x++)
                {
                    for (int y = math.max((int)(position.y - critterRadius), 0); y <= position.y + critterRadius; y++)
                    {
                        if (BaseFlowField[x, y].IsWall) return true;
                    }
                }
                return false;
            }
        }
    }
}