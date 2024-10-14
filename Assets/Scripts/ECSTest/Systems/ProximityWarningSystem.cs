using ECSTest.Aspects;
using ECSTest.Components;
using System.Collections;
using System.Collections.Generic;
using Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static DarkTonic.MasterAudio.EventSounds;
using UnityEngine.SocialPlatforms.Impl;
using static AllEnums;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UIEventSystem))]
    public partial struct ProximityWarningSystem : ISystem
    {
        private const float radius = 5f;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CreepsLocator>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            BeginFixedStepSimulationEntityCommandBufferSystem.Singleton singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);

            CreepsLocator creepsLocator = SystemAPI.GetSingleton<CreepsLocator>();
            NativeList<CreepInfo> creeps = new(Allocator.Temp);

            foreach ((PositionComponent position,EnergyCoreComponent energyCore,Entity entity) in SystemAPI.Query<PositionComponent, EnergyCoreComponent>().WithEntityAccess())
            {
                creepsLocator.LocateNearestCreeps(position.Position, radius, ref creeps,1);
                if(!creeps.IsEmpty && energyCore.PowerCellCount > 0)
                {
                    CreateProximityWarningEvent(ecb, entity, true);
                    creeps.Clear();
                }
                else
                    CreateProximityWarningEvent(ecb, entity, false);
            }
        }

        private void CreateProximityWarningEvent(EntityCommandBuffer ecb, Entity core, bool hasWarning)
        {
            Entity proximityWarningEvent = ecb.CreateEntity();
            ecb.SetName(proximityWarningEvent, "ProximityWarningEvent");
            ecb.AddComponent(proximityWarningEvent, new ProximityWarningEvent() { EnergyCore = core, HasWarning = hasWarning});
        }
    }
}
