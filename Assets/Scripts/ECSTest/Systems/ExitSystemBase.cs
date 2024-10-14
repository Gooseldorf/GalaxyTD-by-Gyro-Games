using ECSTest.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static AllEnums;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MovingSystem))]
    [UpdateBefore(typeof(DestroySystemBase))]
    public partial struct ExitSystemBase : ISystem
    {
        private EntityQuery powerCellQuery;
        private EntityQuery exitsQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CreepsLocator>();

            powerCellQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PowerCellComponent, DestroyComponent, PositionComponent>()
                .WithNone<CreepComponent>()
                .Build(ref state);

            exitsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ExitPointComponent, PositionComponent>()
                .WithNone<CreepComponent>()
                .Build(ref state);

            state.RequireForUpdate<CreepsLocator>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var manager = state.EntityManager;
            var powerCells = powerCellQuery.ToComponentDataArray<PowerCellComponent>(Allocator.Temp);
            var destroyComponents = powerCellQuery.ToComponentDataArray<DestroyComponent>(Allocator.Temp);

            NativeList<int> powerCellIndexes = new(Allocator.Temp);

            for (int i = 0; i < powerCells.Length; i++)
            {
                if (destroyComponents[i].IsNeedToDestroy)
                    continue;
                if (!powerCells[i].IsMoves || !manager.Exists(powerCells[i].Creep))
                    continue;

                powerCellIndexes.Add(i);
            }

            if (powerCellIndexes.Length != 0)
            {
                var cellPositions = powerCellQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);
                var powerCellEntities = powerCellQuery.ToEntityArray(Allocator.Temp);

                var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
                EntityCommandBuffer buffer = singleton.CreateCommandBuffer(state.WorldUnmanaged);

                var exits = exitsQuery.ToComponentDataArray<ExitPointComponent>(Allocator.Temp);
                var exitPositions = exitsQuery.ToComponentDataArray<PositionComponent>(Allocator.Temp);

                for (int i = 0; i < exits.Length; i++)
                {
                    if (powerCellIndexes.Length == 0)
                        break;

                    for (int j = 0; j < powerCellIndexes.Length; j++)
                    {
                        int index = powerCellIndexes[j];
                        if (math.distance(cellPositions[index].Position, exitPositions[i].Position) < 1f)
                        {
                            buffer.SetComponent(powerCellEntities[index], new DestroyComponent { IsNeedToDestroy = true, DestroyDelay = 2 });
                            buffer.SetComponent(powerCells[index].Creep, new CreepComponent() { Escaped = true });
                            buffer.SetComponent(powerCells[index].Creep, new DestroyComponent { IsNeedToDestroy = true, DestroyDelay = 2 });

                            //add ref
                            PowerSystemBase.CreatePowerCellEvent(buffer, CellEventType.Destroy, powerCells[index].CurrentCore, cellPositions[index].Position);

                            powerCellIndexes.RemoveAt(j);
                            j--;
                        }
                    }
                }

                powerCellEntities.Dispose();
                cellPositions.Dispose();
                exits.Dispose();
                exitPositions.Dispose();
            }

            powerCellIndexes.Dispose();
            destroyComponents.Dispose();
            powerCells.Dispose();

        }

        // [BurstCompile]
        // public void OnUpdate(ref SystemState state)
        // {
        //     var singleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        //     EntityCommandBuffer buffer = singleton.CreateCommandBuffer(state.WorldUnmanaged);
        //
        //     var creepLocator = SystemAPI.GetSingleton<CreepsLocator>();
        //
        //     NativeList<Entity> unitsToDestroy = new(Allocator.Temp);
        //
        //     NativeList<CreepInfo> creeps = new(Allocator.Temp);
        //     foreach (ExitPointAspect aspect in SystemAPI.Query<ExitPointAspect>())
        //     {
        //         creepLocator.LocateNearestCreeps(aspect.PositionComponent.ValueRO.Position, aspect.GridPosition.ValueRO.Value.GridSize.x / 2, ref creeps);
        //
        //         foreach (CreepInfo creepInfo in creeps)
        //         {
        //             if (creepInfo.IsGoingIn) continue;
        //
        //             state.EntityManager.SetComponentData(creepInfo.Entity, new DestroyComponent() {IsNeedToDestroy = true});
        //
        //             var creepComponent = state.EntityManager.GetComponentData<CreepComponent>(creepInfo.Entity);
        //             creepComponent.Escaped = true;
        //             state.EntityManager.SetComponentData(creepInfo.Entity, creepComponent);
        //
        //             unitsToDestroy.Add(creepInfo.Entity);
        //         }
        //
        //         creeps.Clear();
        //     }
        //
        //
        //     if (unitsToDestroy.Length != 0)
        //     {
        //         //TODO: mb use some component with link to creep on relic?
        //         foreach ((PowerCellComponent powerCellComponent, RefRW<DestroyComponent> destroyComponent) in SystemAPI.Query<PowerCellComponent, RefRW<DestroyComponent>>())
        //         {
        //             if (!powerCellComponent.IsMoves || !state.EntityManager.Exists(powerCellComponent.Creep))
        //                 continue;
        //
        //             foreach (Entity unit in unitsToDestroy)
        //             {
        //                 if (unit == powerCellComponent.Creep)
        //                 {
        //                     destroyComponent.ValueRW.IsNeedToDestroy = true;
        //
        //                     Entity powerCellEvent = buffer.CreateEntity();
        //                     buffer.SetName(powerCellEvent, nameof(PowerCellEvent));
        //                     buffer.AddComponent(powerCellEvent, new PowerCellEvent() {EventType = AllEnums.CellEventType.Destroy, Core = powerCellComponent.Core});
        //                     break;
        //                 }
        //             }
        //         }
        //     }
        //     
        //     creeps.Dispose();
        //     unitsToDestroy.Dispose();
        // }
    }
}