using ECSTest.Aspects;
using ECSTest.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static AllEnums;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MovingSystem))]
    public partial struct PowerCellSystemBase : ISystem
    {
        public const float ReturnTime = 15f;
        public const float ShowBubbleTime = 5f;
        private const float takerMassModifier = 5f;
        private const float takerMassModifierPerFrame = 1.01f;
        private const float distance = 1.5f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CreepsLocator>();
            state.RequireForUpdate<BaseFlowField>();
        }

        [BurstCompile(CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer buffer = singleton.CreateCommandBuffer(state.WorldUnmanaged);

            var creepsLocator = SystemAPI.GetSingleton<CreepsLocator>();
            float deltaTime = SystemAPI.Time.DeltaTime;
            NativeList<CreepInfo> creeps = new(Allocator.Temp);

            foreach (PowerCellAspect powerCell in SystemAPI.Query<PowerCellAspect>())
            {
                if(powerCell.DestroyComponent.ValueRO.IsNeedToDestroy)
                    continue;
                
                // if power cell is not on core
                if (powerCell.PowerCellComponent.ValueRO.IsMoves)
                {
                    //if carried by creep
                    if (creepsLocator.HasEntity(powerCell.PowerCellComponent.ValueRO.Creep))
                    {
                        FearComponent fearComponent = state.EntityManager.GetComponentData<FearComponent>(powerCell.PowerCellComponent.ValueRO.Creep);
                        CreepComponent creepComponent = state.EntityManager.GetComponentData<CreepComponent>(powerCell.PowerCellComponent.ValueRO.Creep);
                        
                        if (fearComponent.Time > 0)
                        {
                            
                            creepComponent.Mass /= takerMassModifier;
                            
                            Movable movable = state.EntityManager.GetComponentData<Movable>(powerCell.PowerCellComponent.ValueRO.Creep);
                            movable.IsGoingIn = false;
                            state.EntityManager.SetComponentData(powerCell.PowerCellComponent.ValueRO.Creep, movable);       
                            
                            state.EntityManager.SetComponentData(powerCell.PowerCellComponent.ValueRO.Creep, creepComponent);
                            powerCell.Detach();
                            powerCell.Timer.ValueRW.Timer -= deltaTime;
                        }
                        else
                        {
                            creepComponent.Mass *= takerMassModifierPerFrame;
                            PositionComponent positionComponent = state.EntityManager.GetComponentData<PositionComponent>(powerCell.PowerCellComponent.ValueRO.Creep);
                            powerCell.Position.ValueRW.Position = positionComponent.Position;
                        }
                    }
                    else
                    {
                        if (powerCell.PowerCellComponent.ValueRO.Creep != Entity.Null)
                        {
                            powerCell.Detach();
                        }

                        powerCell.Timer.ValueRW.Timer -= deltaTime;

                        // if time to show bubble
                        bool shouldShowBubble = powerCell.Timer.ValueRW.Timer < (ReturnTime - ShowBubbleTime);
                        bool lastFrameShouldShowBubble = (powerCell.Timer.ValueRW.Timer + deltaTime) < (ReturnTime - ShowBubbleTime);

                        if (shouldShowBubble && !lastFrameShouldShowBubble)
                            ShowBubble(buffer, powerCell.Entity, true);

                        // if timer passed attach to core
                        if (powerCell.Timer.ValueRO.Timer <= 0)
                        {
                            AttachToCore(state.EntityManager, powerCell, buffer);
                            ShowBubble(buffer, powerCell.Entity, false);
                            continue;
                        }
                        // check other creeps to attach to
                        creepsLocator.LocateNearestCreeps(powerCell.Position.ValueRO.Position, distance, ref creeps,5);
                        if (CanAttachToCreepEntity(state.EntityManager, creeps, out Entity entity))
                        {
                            AttachToCreep(state.EntityManager, powerCell, entity);
                            ShowBubble(buffer, powerCell.Entity, false);
                        }
                    }
                }
                // if power cell is on core
                else
                {
                    var core = powerCell.PowerCellComponent.ValueRO.CurrentCore;

                    GridPositionComponent coreGridPosition = state.EntityManager.GetComponentData<GridPositionComponent>(core);
                    PositionComponent corePositionComponent = state.EntityManager.GetComponentData<PositionComponent>(core);

                    creepsLocator.LocateNearestCreeps(corePositionComponent.Position, coreGridPosition.Value.GridSize.x / 2, ref creeps,5);
                    // check creeps to attach to
                    if (!CanAttachToCreepEntity(state.EntityManager, creeps, out Entity entity)) continue;
                    // attach to creep
                    EnergyCoreComponent energyCoreComponent = state.EntityManager.GetComponentData<EnergyCoreComponent>(core);
                    energyCoreComponent.PowerCellCount -= 1;
                    state.EntityManager.SetComponentData(core, energyCoreComponent);
                    PowerSystemBase.CreatePowerCellEvent(buffer, CellEventType.Detach, core, state.EntityManager.GetComponentData<PositionComponent>(entity).Position);
                    AttachToCreep(state.EntityManager, powerCell, entity);
                }
            }

            creeps.Dispose();
        }

        private bool CanAttachToCreepEntity(EntityManager manager, NativeList<CreepInfo> creeps, out Entity entity)
        {
            foreach (var creepInfo in creeps)
            {
                Movable movable = manager.GetComponentData<Movable>(creepInfo.Entity);
                if (movable.IsGoingIn)
                {
                    entity = creepInfo.Entity;
                    return true;
                }
            }

            entity = Entity.Null;
            return false;
        }

        public static void AttachToCore(EntityManager manager, PowerCellAspect powerCellAspect, EntityCommandBuffer ecb)
        {
            Entity currentCore = powerCellAspect.PowerCellComponent.ValueRO.CurrentCore;
            Entity saveCore = powerCellAspect.PowerCellComponent.ValueRO.SaveCore;

            CellEventType eventType = CellEventType.Return;
            
            if (saveCore != Entity.Null && currentCore != saveCore)
            {
                powerCellAspect.PowerCellComponent.ValueRW.CurrentCore = saveCore;
                currentCore = saveCore;
                eventType = CellEventType.AttachNew;
            }
            
            powerCellAspect.ReturnToCore();
            EnergyCoreComponent energyCoreComponent = manager.GetComponentData<EnergyCoreComponent>(currentCore);
            energyCoreComponent.PowerCellCount += 1;
            manager.SetComponentData(currentCore, energyCoreComponent);

            PowerSystemBase.CreatePowerCellEvent(ecb, eventType ,currentCore, powerCellAspect.Position.ValueRO.Position, 1);
        }

        private void AttachToCreep(EntityManager manager, PowerCellAspect powerCellAspect, Entity creepEntity)
        {
            powerCellAspect.AttachToCreep(creepEntity);
            Movable movable = manager.GetComponentData<Movable>(creepEntity);
            movable.IsGoingIn = false;
            manager.SetComponentData(creepEntity, movable);

            CreepComponent creepComponent = manager.GetComponentData<CreepComponent>(creepEntity);
            creepComponent.Mass *= takerMassModifier;
            manager.SetComponentData(creepEntity, creepComponent);
            
            powerCellAspect.Timer.ValueRW.Timer = ReturnTime;
        }
        public static void ShowBubble(EntityCommandBuffer ecb, Entity powerCell, bool show)
        {
            Entity bubbleEvent = ecb.CreateEntity();
            ecb.SetName(bubbleEvent, "BubbleEvent");
            ecb.AddComponent(bubbleEvent, new BubbleEvent() { PowerCell = powerCell, NeedToShow = show });
        }
    }
} 