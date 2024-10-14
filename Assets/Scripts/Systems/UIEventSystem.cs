using CardTD.Utilities;
using DG.Tweening;
using ECSTest.Components;
using Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static AllEnums;
/*
using static MusicManager;
*/

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(RemoveEventSystem))]
    public partial struct UIEventSystem : ISystem
    {
        private EntityQuery cashQuery;
        private EntityQuery waveQuery;
        private EntityQuery spawnQuery;
        private EntityQuery spawnEventQuery;
        private EntityQuery cellQuery;
        private EntityQuery reloadQuery;
        private EntityQuery bubbleQuery;
        private EntityQuery warningQuery;
        private EntityQuery secondChanceQuery;
        private EntityQuery changePowerEventQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CashComponent>();
            cashQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<CashUpdatedEvent>().WithAbsent<AnimatedTextComponent>().Build(ref state);
            waveQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<NextWaveEvent>().Build(ref state);
            cellQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<PowerCellEvent>().Build(ref state);
            reloadQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<ReloadEvent>().Build(ref state);
            bubbleQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<BubbleEvent>().Build(ref state);
            warningQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<ProximityWarningEvent>().Build(ref state);
            secondChanceQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<SecondChanceEvent>().Build(ref state);
            spawnQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<SpawnComponent>().Build(ref state);
            spawnEventQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<SpawnEvent>().Build(ref state);
            changePowerEventQuery = new EntityQueryBuilder(Allocator.Temp).WithDisabled<ChangePowerEvent>().Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            if (!cashQuery.IsEmpty)
            {
                NativeArray<CashUpdatedEvent> cashEvents = cashQuery.ToComponentDataArray<CashUpdatedEvent>(Allocator.Temp);
                bool cashForWave = false;
                foreach (CashUpdatedEvent cashEvent in cashEvents)
                {
                    if (!cashEvent.Position.Equals(float2.zero) && cashEvent.CashAmount != 0)
                    {
                        Entity cashEntity = ecb.CreateEntity();
                        ecb.SetName(cashEntity, "AnimatedText");
                        ecb.AddComponent(cashEntity,
                            new AnimatedTextComponent()
                            {
                                CashValue = cashEvent.CashAmount,
                                NonCashValue = 0,
                                Position = new float2(cashEvent.Position.x + GameServices.Instance.RenderDataHolder.TextAnimationData.CashPopUpXStartOffset, cashEvent.Position.y + GameServices.Instance.RenderDataHolder.TextAnimationData.CashPopUpYStartOffset),
                                Timer = 0,
                                Color = cashEvent.CashAmount < 0
                                    ? GameServices.Instance.RenderDataHolder.TextAnimationData.SubtractCashTextColor
                                    : GameServices.Instance.RenderDataHolder.TextAnimationData.AddCashTextColor,
                                TextType = TextType.Cash
                            });
                    }

                    if (cashEvent.CashForWave) cashForWave = true;
                }

                if (cashEvents.Length > 0)
                {
                    state.EntityManager.CompleteDependencyBeforeRO<CashComponent>();
                    var cashComponent = SystemAPI.GetSingleton<CashComponent>();
                    Messenger<int, bool>.Broadcast(GameEvents.CashUpdated, cashComponent.Cash, cashForWave, MessengerMode.DONT_REQUIRE_LISTENER);
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
                //state.EntityManager.DestroyEntity(cashQuery);
                cashEvents.Dispose();
            }

            int unitLeftToSpawn = 0;
            if (!spawnEventQuery.IsEmpty)
            {
                NativeArray<SpawnComponent> spawnComponents = spawnQuery.ToComponentDataArray<SpawnComponent>(Allocator.Temp);
                unitLeftToSpawn = spawnComponents.Length;
                MusicManager.PlaySound2D(SoundKey.Creep_spawn);
                Messenger<int>.Broadcast(GameEvents.UnitSpawned, spawnComponents.Length, MessengerMode.DONT_REQUIRE_LISTENER);
                spawnComponents.Dispose();
            }

            if (!waveQuery.IsEmpty)
            {
                NativeArray<NextWaveEvent> waveEvents = waveQuery.ToComponentDataArray<NextWaveEvent>(Allocator.Temp);
                foreach (NextWaveEvent waveEvent in waveEvents)
                {
                    MusicManager.PlaySound2D(SoundKey.Next_wave);
                    Messenger<int>.Broadcast(GameEvents.NextWave, waveEvent.WaveNumber, MessengerMode.DONT_REQUIRE_LISTENER);
                }

                if (unitLeftToSpawn != 0)
                    Messenger<int>.Broadcast(GameEvents.UpdateUnitsLeftToSpawn, unitLeftToSpawn, MessengerMode.DONT_REQUIRE_LISTENER);
                
                waveEvents.Dispose();
            }

            if (!cellQuery.IsEmpty)
            {
                NativeArray<PowerCellEvent> cellEvents = cellQuery.ToComponentDataArray<PowerCellEvent>(Allocator.Temp);
                foreach (PowerCellEvent cellEvent in cellEvents)
                {
                    Messenger<PowerCellEvent>.Broadcast(GetGameEvent(cellEvent.EventType), cellEvent, MessengerMode.DONT_REQUIRE_LISTENER);

                    if (cellEvent.EventType == CellEventType.Return)
                    {
                        var go = GameServices.Instance.Get<SimpleEffectManager>().PowerCellMovePool.Get();
                        go.transform.position = new Vector3(cellEvent.Position.x, cellEvent.Position.y, 0);

                        var corePosition = state.EntityManager.GetComponentData<PositionComponent>(cellEvent.Core).Position;

                        go.transform.DOMove(new Vector3(corePosition.x, corePosition.y, 0), 12f).SetSpeedBased(true).OnComplete(() =>
                        {
                            GameServices.Instance.Get<SimpleEffectManager>().PowerCellMovePool.Release(go);
                        });
                    }

                    string GetGameEvent(CellEventType evtType)
                    {
                        switch (evtType)
                        {
                            case CellEventType.Detach: return GameEvents.CellDetached;
                            case CellEventType.Return: return GameEvents.CellAttached;
                            case CellEventType.DestroyAll: return GameEvents.CellDestroyedAll;
                            case CellEventType.AttachNew: return GameEvents.CellAttachedNew;
                            default: 
                                MusicManager.PlaySound2D(SoundKey.Cell_destroy);
                                return GameEvents.CellDestroyed;
                        }
                    }
                }

                // state.EntityManager.DestroyEntity(cellQuery);
                cellEvents.Dispose();
            }

            if (!reloadQuery.IsEmpty)
            {
                NativeArray<ReloadEvent> reloadEvents = reloadQuery.ToComponentDataArray<ReloadEvent>(Allocator.Temp);
                foreach (ReloadEvent reloadEvent in reloadEvents)
                    Messenger<Entity>.Broadcast(GameEvents.TowerReload, reloadEvent.Tower, MessengerMode.DONT_REQUIRE_LISTENER);

                // state.EntityManager.DestroyEntity(reloadQuery);
                reloadEvents.Dispose();
            }

            if (!bubbleQuery.IsEmpty)
            {
                NativeArray<BubbleEvent> bubbleEvents = bubbleQuery.ToComponentDataArray<BubbleEvent>(Allocator.Temp);
                foreach (BubbleEvent bubbleEvent in bubbleEvents)
                    Messenger<Entity, bool>.Broadcast(GameEvents.BubbleEvent, bubbleEvent.PowerCell, bubbleEvent.NeedToShow, MessengerMode.DONT_REQUIRE_LISTENER);

                // state.EntityManager.DestroyEntity(bubbleEvents);
                bubbleEvents.Dispose();
            }

            if (!warningQuery.IsEmpty)
            {
                NativeArray<ProximityWarningEvent> warningEvents = warningQuery.ToComponentDataArray<ProximityWarningEvent>(Allocator.Temp);
                bool needToIncreaseBattleMusicIntensity = false;
                foreach (ProximityWarningEvent warningEvent in warningEvents)
                {
                    Messenger<Entity, bool>.Broadcast(GameEvents.UpdateVisualWarning, warningEvent.EnergyCore, warningEvent.HasWarning, MessengerMode.DONT_REQUIRE_LISTENER);
                    if(warningEvent.HasWarning)
                        needToIncreaseBattleMusicIntensity = true;
                }

                if (needToIncreaseBattleMusicIntensity)
                {
                    if(!MusicManager.IsIntenseBattleMusicOn) 
                        MusicManager.IncreaseBattleMusicIntensity(); 
                }
                else
                {
                    if(MusicManager.IsIntenseBattleMusicOn)
                        MusicManager.DecreaseBattleMusicIntensity();
                }
                
                // state.EntityManager.DestroyEntity(warningEvents);
                warningEvents.Dispose();
            }

            if (!secondChanceQuery.IsEmpty)
                Messenger.Broadcast(GameEvents.SecondChanceUsed, MessengerMode.DONT_REQUIRE_LISTENER);

            if (!changePowerEventQuery.IsEmpty)
            {
                NativeArray<ChangePowerEvent> changePowerEvents = changePowerEventQuery.ToComponentDataArray<ChangePowerEvent>(Allocator.Temp);
                foreach (ChangePowerEvent changePowerEvent in changePowerEvents)
                {
                    Messenger<ChangePowerEvent>.Broadcast(GameEvents.PowerChanged, changePowerEvent, MessengerMode.DONT_REQUIRE_LISTENER);
                }
                changePowerEvents.Dispose();
            }
        }
    }
}