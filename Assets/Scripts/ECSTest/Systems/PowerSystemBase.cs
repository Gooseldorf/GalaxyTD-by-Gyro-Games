using DG.Tweening;
using ECSTest.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static AllEnums;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(FlowFieldBuildCacheSystem))]
    public partial struct PowerSystemBase : ISystem
    {
        private EntityQuery coresQuery;
        private EntityQuery powerCellsQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConnectedPowerablesComponent>();

            coresQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnergyCoreComponent, PositionComponent>()
                .Build(ref state);

            powerCellsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PowerCellComponent,DestroyComponent>()
                .Build(ref state);
        }

        internal static void Init(World world, ref NativeParallelMultiHashMap<Entity, Entity> connectedPowerables)
        {
            ConnectedPowerablesComponent component = new() {ConnectedPowerables = connectedPowerables};
            world.EntityManager.CreateSingleton(component);
        }

        public struct ConnectedPowerablesComponent : IComponentData, IDisposable
        {
            public NativeParallelMultiHashMap<Entity, Entity> ConnectedPowerables;

            public void Dispose()
            {
                ConnectedPowerables.Dispose();
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            bool needUpdateFlowField = false;
            ConnectedPowerablesComponent component = SystemAPI.GetSingleton<ConnectedPowerablesComponent>();
            TimeSkipper timeSkipper = SystemAPI.GetSingleton<TimeSkipper>();

            EntityCommandBuffer ecb = new(Allocator.Temp);

            var cores = coresQuery.ToComponentDataArray<EnergyCoreComponent>(Allocator.Temp);
            var coreEntities = coresQuery.ToEntityArray(Allocator.Temp);

            int countEnergyCores = 0;

            for (int i = 0; i < cores.Length; i++)
            {
                EnergyCoreComponent core = cores[i];

                if (!core.IsTurnedOn)
                    continue;

                if (cores[i].TurnedOffTime != 0)
                    continue;

                if (core.PowerCellCount <= 0)
                {
                    FindNearestAndTurnOff(coreEntities[i], core, state.EntityManager, coreEntities, cores, ecb, component);
                    core.TurnedOffTime = timeSkipper.CurrentTime((float)SystemAPI.Time.ElapsedTime);
                    needUpdateFlowField = true;
                    ecb.SetComponent(coreEntities[i], core);
                    continue;
                }

                countEnergyCores++;

                if (core.DeactivationTime == 0)
                {
                    continue;
                }

                if (timeSkipper.CurrentTime((float)SystemAPI.Time.ElapsedTime) >= (SpawnerSystem.FirstWaveOffset - SpawnerSystem.FirstWaveSpawnOffset))
                    core.DeactivationTime -= SystemAPI.Time.DeltaTime;

                if (core.DeactivationTime <= 0)
                {
                    FindNearestAndTurnOff(coreEntities[i], core, state.EntityManager, coreEntities, cores, ecb, component);
                    needUpdateFlowField = true;
                }
                else
                    ecb.SetComponent(coreEntities[i], core);
            }

            cores.Dispose();
            coreEntities.Dispose();

            if (needUpdateFlowField)
            {
                Entity eventEntity = ecb.CreateEntity();
                ecb.AddComponent(eventEntity, new BaseCostChangedEvent());
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        private void FindNearestAndTurnOff(Entity coreEntity, EnergyCoreComponent core, EntityManager manager, NativeArray<Entity> coreEntities, NativeArray<EnergyCoreComponent> cores,
            EntityCommandBuffer ecb, ConnectedPowerablesComponent component)
        {
            int nearestCoreIndex = -1;
            var position = manager.GetComponentData<PositionComponent>(coreEntity).Position;
            float distance = float.MaxValue;

            for (int j = 0; j < coreEntities.Length; j++)
            {
                if (coreEntity == coreEntities[j] || !cores[j].IsTurnedOn || cores[j].TurnedOffTime > 0)
                    continue;

                PositionComponent positionComponent = manager.GetComponentData<PositionComponent>(coreEntities[j]);
                float lenght = math.length(positionComponent.Position - position);
                if (lenght >= distance)
                    continue;
                distance = lenght;
                nearestCoreIndex = j;
            }

            if (nearestCoreIndex == -1)
            {
                return;
            }

            var powerCells = powerCellsQuery.ToComponentDataArray<PowerCellComponent>(Allocator.Temp);
            var powerCellsEntities = powerCellsQuery.ToEntityArray(Allocator.Temp);
            var destroyComponents = powerCellsQuery.ToComponentDataArray<DestroyComponent>(Allocator.Temp);

            int countPowerCells = 0;
            for (int j = 0; j < powerCells.Length; j++)
            {
                if(destroyComponents[j].IsNeedToDestroy)
                    continue;
                
                PowerCellComponent powerCell = powerCells[j];
                if (powerCell.CurrentCore == coreEntity || powerCell.SaveCore == coreEntity)
                {
                    countPowerCells++;
                    powerCell.SaveCore = coreEntities[nearestCoreIndex];
                    if (!powerCell.IsMoves)
                        powerCell.CurrentCore = coreEntities[nearestCoreIndex];

                    manager.SetComponentData(powerCellsEntities[j], powerCell);
                }
            }

            destroyComponents.Dispose();
            powerCells.Dispose();
            powerCellsEntities.Dispose();


            EnergyCoreComponent nearestCore = cores[nearestCoreIndex];
            nearestCore.PowerCellCount += core.PowerCellCount;
            //ecb.SetComponent(coreEntities[nearestCoreIndex], nearestCore);
            manager.SetComponentData(coreEntities[nearestCoreIndex], nearestCore);

            // //TODO: need create events
            CreatePowerCellEvent(ecb, CellEventType.AttachNew, coreEntities[nearestCoreIndex], float2.zero, core.PowerCellCount);
            CreatePowerCellEvent(ecb, CellEventType.DestroyAll, coreEntity, float2.zero, countPowerCells);
            CoreTurnOff(manager, ecb, core, component, coreEntity, 0, false);
        }


        public static void CreatePowerCellEvent(EntityCommandBuffer ecb, CellEventType eventType, Entity core, float2 position, int value = 0) //, int countActive = 0
        {
            Entity powerCellEvent = ecb.CreateEntity();
            ecb.SetName(powerCellEvent, "PowerCellEvent");
            ecb.AddComponent(powerCellEvent, new PowerCellEvent() {EventType = eventType, Core = core, Value = value, Position = position});
        }

        public static void CoreTurnOff(EntityManager manager, EntityCommandBuffer ecb, EnergyCoreComponent energyCore, ConnectedPowerablesComponent component, Entity coreEntity, int cellCount,
            bool isPowered)
        {
            NativeParallelMultiHashMap<Entity, Entity>.Enumerator enumerator = component.ConnectedPowerables.GetValuesForKey(coreEntity);
            while (enumerator.MoveNext())
            {
                PowerableComponent powerable = manager.GetComponentData<PowerableComponent>(enumerator.Current);
                powerable.IsPowered = isPowered;
                // ecb.SetComponent(enumerator.Current, powerable);
                manager.SetComponentData(enumerator.Current, powerable);

                Entity powerEvent = ecb.CreateEntity();
                ecb.SetName(powerEvent, nameof(ChangePowerEvent));
                ecb.AddComponent(powerEvent, new ChangePowerEvent() {Entity = enumerator.Current, IsTurnedOn = powerable.IsTurnedOn});

                if (!manager.HasComponent<DropZoneComponent>(enumerator.Current))
                    continue;

                Entity entity = manager.GetComponentData<EntityHolderComponent>(enumerator.Current);
                if (entity != Entity.Null)
                {
                    powerable = manager.GetComponentData<PowerableComponent>(entity);
                    powerable.IsPowered = isPowered;
                    ecb.SetComponent(entity, powerable);
                }
            }

            energyCore.IsTurnedOn = isPowered;
            energyCore.PowerCellCount = cellCount;

            // ecb.SetComponent(coreEntity, energyCore);
            manager.SetComponentData(coreEntity, energyCore);
        }
    }
}