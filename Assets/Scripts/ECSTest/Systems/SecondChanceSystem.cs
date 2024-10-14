using ECSTest.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static AllEnums;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(FlowFieldBuildCacheSystem))]
    public partial struct SecondChanceSystem : ISystem
    {
        private EntityQuery secondChanceQuery;
        private EntityQuery attackerQuery;
        private EntityQuery coresQuery;

        public void OnCreate(ref SystemState state)
        {
            coresQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnergyCoreComponent, PositionComponent>()
                .Build(ref state);

            secondChanceQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SecondChanceEvent>()
                .Build(ref state);

            attackerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AttackerComponent>()
                .Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!secondChanceQuery.IsEmpty)
            {


                var cores = coresQuery.ToComponentDataArray<EnergyCoreComponent>(Allocator.Temp);
                var coreEntities = coresQuery.ToEntityArray(Allocator.Temp);

                if (cores.Length == 0)
                {
                    Debug.LogError($"cores count is 0");
                }
                else
                {
                    PowerSystemBase.ConnectedPowerablesComponent component = SystemAPI.GetSingleton<PowerSystemBase.ConnectedPowerablesComponent>();
                    EntityCommandBuffer ecb = new(Allocator.Temp);

                    Entity lastCore = coreEntities[0];
                    float time = cores[0].TurnedOffTime;

                    if (cores.Length > 1)
                    {
                        for (int i = 1; i < cores.Length; i++)
                        {
                            if (cores[i].TurnedOffTime > time)
                            {
                                lastCore = coreEntities[i];
                                time = cores[i].TurnedOffTime;
                            }
                        }
                    }


                    ReloadAllAttackers(state.EntityManager);

                    state.EntityManager.CompleteDependencyBeforeRW<CashComponent>();
                    RefRW<CashComponent> cashComponent = SystemAPI.GetSingletonRW<CashComponent>();
                    PositionComponent position = state.EntityManager.GetComponentData<PositionComponent>(lastCore);

                    cashComponent.ValueRW.SecondChance(GameServices.Instance.CurrentMission, GameServices.Instance.CurrentWave(),
                        state.EntityManager, position.Position);

                    var secondChances = secondChanceQuery.ToComponentDataArray<SecondChanceEvent>(Allocator.Temp);
                    var secondChanceEvent = secondChances[0];

                    var creepLocator = SystemAPI.GetSingleton<CreepsLocator>();
                    NativeList<CreepInfo> creepInfos = new(Allocator.Temp);
                    creepLocator.LocateNearestCreeps(position.Position, secondChanceEvent.Range, ref creepInfos, 50);

                    PowerSystemBase.CreatePowerCellEvent(ecb, CellEventType.AttachNew, lastCore, float2.zero, secondChanceEvent.CountPowerCells);

                    GameServices.Instance.Get<SimpleEffectManager>().ShowSecondChanceVisual(position.Position, secondChanceEvent.Range);

                    foreach (var creep in creepInfos)
                        DamageSystem.DestroyCreep(state.EntityManager, creep.Entity);

                    EnergyCoreComponent coreComponent = state.EntityManager.GetComponentData<EnergyCoreComponent>(lastCore);
                    PowerSystemBase.CoreTurnOff(state.EntityManager, ecb, coreComponent, component, lastCore, secondChanceEvent.CountPowerCells, true);

                    for (int i = 0; i < secondChanceEvent.CountPowerCells; i++)
                        Mission.CreatePowerCell(state.EntityManager, lastCore, position.Position);

                    Entity eventEntity = ecb.CreateEntity();
                    ecb.AddComponent(eventEntity, new BaseCostChangedEvent());

                    if (secondChances.Length > 1)
                    {
                        Debug.LogError("More than 1 SecondChanceDetected");
                        state.EntityManager.DestroyEntity(secondChanceQuery);
                    }
                    else
                    {
                        var entities = secondChanceQuery.ToEntityArray(Allocator.Temp);
                        state.EntityManager.SetComponentEnabled<SecondChanceEvent>(entities[0], false);
                        entities.Dispose();
                    }

                    creepInfos.Dispose();
                    secondChances.Dispose();

                    ecb.Playback(state.EntityManager);
                    ecb.Dispose();
                }

                cores.Dispose();
                coreEntities.Dispose();
            }
        }

        private void ReloadAllAttackers(EntityManager manager)
        {
            var attackers = attackerQuery.ToComponentDataArray<AttackerComponent>(Allocator.Temp);
            var entities = attackerQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                AttackerComponent attacker = attackers[i];
                attacker.Bullets = attacker.AttackStats.ReloadStats.MagazineSize;
                attacker.ReloadTimer = 0;
                attacker.BulletLeftInCurrentBurst = attacker.AttackStats.ShootingStats.ShotsPerBurst;

                attacker.WindUpTimer = attacker.AttackStats.ShootingStats.WindUpTime;
                attacker.ShootTimer = 0;
                attacker.BurstTimer = 0;

                manager.SetComponentData(entities[i], attacker);
            }

            attackers.Dispose();
            entities.Dispose();
        }
    }
}