using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECSTest.Components
{
    public struct CashComponent : IComponentData, ICustomManaged<CashComponent>
    {
        private const float cashOnSecondChanceModifier = .3f;


        public static void SpawnCashUpdatedEvent(EntityCommandBuffer commandBuffer, int cashAmount, float2 position = default, bool cashForWave = false)
        {

            Entity cashUpdatedEvent = commandBuffer.CreateEntity();
            commandBuffer.SetName(cashUpdatedEvent, nameof(CashUpdatedEvent));
            commandBuffer.AddComponent(cashUpdatedEvent, new CashUpdatedEvent() { CashAmount = cashAmount, Position = position, CashForWave = cashForWave });
        }

        public static void SpawnCashUpdatedEvent(EntityManager manager, int cashAmount, float2 position = default, bool cashForWave = false)
        {
            Entity cashUpdatedEvent = manager.CreateEntity();
            manager.SetName(cashUpdatedEvent, nameof(CashUpdatedEvent));
            manager.AddComponentData(cashUpdatedEvent, new CashUpdatedEvent() { CashAmount = cashAmount, Position = position });
        }

        public CashComponent(List<int> cashesOnWaveEnd)
        {
            if (cashesOnWaveEnd.Count == 0)
            {
                Debug.LogError("Cashes is empty");
            }

            this.cashs = new NativeArray<int>(1, Allocator.Persistent);
            this.cashs[0] = cashesOnWaveEnd[0];
            this.cashsForCreeps = new NativeArray<int>(1, Allocator.Persistent);
            this.cashsForCreeps[0] = 0;
            this.cashsForWaves = new NativeArray<int>(1, Allocator.Persistent);
            this.cashsForWaves[0] = 0;
            this.cashsPerStartWave = new NativeArray<int>(cashesOnWaveEnd.ToArray(), Allocator.Persistent);
            this.cashsToReloadingForMin = new NativeArray<int>(1, Allocator.Persistent);
        }

        public void Load(CashComponent from)
        {
            cashs.CopyFrom(from.cashs);
            cashsForCreeps.CopyFrom(from.cashsForCreeps);
            cashsForWaves.CopyFrom(from.cashsForWaves);
            cashsPerStartWave.CopyFrom(from.cashsPerStartWave);
        }

        public CashComponent Clone()
        {
            return new CashComponent()
            {
                cashs = new NativeArray<int>(cashs, Allocator.Persistent),
                cashsForCreeps = new NativeArray<int>(cashsForCreeps, Allocator.Persistent),
                cashsForWaves = new NativeArray<int>(cashsForWaves, Allocator.Persistent),
                cashsPerStartWave = new NativeArray<int>(cashsPerStartWave, Allocator.Persistent),
            };
        }

        public void Dispose()
        {
            cashs.Dispose();
            cashsForCreeps.Dispose();
            cashsForWaves.Dispose();
            cashsPerStartWave.Dispose();
            cashsToReloadingForMin.Dispose();
            //Debug.LogError($"----> CashComponent Disposed");
        }

        public readonly bool IsCreated => cashs.IsCreated;

        private NativeArray<int> cashs;
        private NativeArray<int> cashsPerStartWave;
        private NativeArray<int> cashsForCreeps;
        private NativeArray<int> cashsForWaves;

        private NativeArray<int> cashsToReloadingForMin;

        public int GetLastCashPerWaveStart => cashsPerStartWave[^1];

        public void AddCashPerStartWave(int cash)
        {
            NativeList<int> cashes = new(Allocator.Temp);
            foreach (int cashPerWave in cashsPerStartWave)
                cashes.Add(cashPerWave);
            cashes.Add(cash);

            cashsPerStartWave.Dispose();
            cashsPerStartWave = new NativeArray<int>(cashes.AsArray(), Allocator.Persistent);
        }

        public int CashForCritterDeath => (cashsPerStartWave[0] / 2);

        public void AddCashForCritterDie(EntityCommandBuffer commandBuffer, float2 position)
        {
            Cash += CashForCritterDeath;
            SpawnCashUpdatedEvent(commandBuffer, CashForCritterDeath, position);
        }

        public int CashsToReloadingForMin
        {
            get => this.cashsToReloadingForMin[0];
            private set => this.cashsToReloadingForMin[0] = value;
        }

        public int Cash
        {
            get => this.cashs[0];
            private set
            {
                // var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
                // var cashUpdatedEntity = manager.CreateEntity();
                // manager.SetName(cashUpdatedEntity, "Cash Update");
                // manager.AddComponentData(cashUpdatedEntity, new DestroyComponent() {IsNeedToDestroy = false});
                // manager.AddComponentData(cashUpdatedEntity, new CashUpdateComponent() {Variation = this.cash - value});
                this.cashs[0] = value;
                //TODO: create Entity for changed cash event
                ///Messenger<int>.Broadcast(GameEvents.CashUpdated, this.cash, MessengerMode.DONT_REQUIRE_LISTENER);
            }
        }

        public int CashForCreeps
        {
            get => this.cashsForCreeps[0];
            private set => this.cashsForCreeps[0] = value;
        }

        public int CashForWaves
        {
            get => this.cashsForWaves[0];
            private set => this.cashsForWaves[0] = value;
        }

        public bool CanSpendCash(int value) => Cash >= value;

        public void ReloadAttacker(AttackerComponent attacker, EntityCommandBuffer commandBuffer, float2 position)
        {
            if (CanSpendCash(attacker.AttackStats.ReloadStats.ReloadCost))
            {
                Cash -= attacker.AttackStats.ReloadStats.ReloadCost;
                SpawnCashUpdatedEvent(commandBuffer, -attacker.AttackStats.ReloadStats.ReloadCost, position);
            }
        }

        public void BuildTower(Tower tower)
        {
            if (CanSpendCash(tower.BuildCost))
            {
                Cash -= tower.BuildCost;
            }
        }

        public void SpendCash(int cost)
        {
            Cash -= cost;
        }

        public void AddCash(int value)
        {
            Cash += value;
        }

        public void SellTower(CostComponent component)
        {
            Cash += component.SellCost;
        }

        public void CreepDestroyCashChange(int creepCashReward, EntityCommandBuffer commandBuffer, float2 position)
        {
            Cash += creepCashReward;
            CashForCreeps += creepCashReward;
            SpawnCashUpdatedEvent(commandBuffer, creepCashReward, position);
        }

        public void AddCashForTest(int cash)
        {
            Cash += cash;
            CashForCreeps += cash;
        }

        public void SetCashsToSafeReload(int cashToReload) => CashsToReloadingForMin = cashToReload;

        public void WaveStart(int wave, EntityCommandBuffer commandBuffer)
        {
            int cashOnWaveEnd = cashsPerStartWave.Length > wave ? cashsPerStartWave[wave] : 0;//cash Per Start Wave[^1]
            Cash += cashOnWaveEnd;
            CashForWaves += cashOnWaveEnd;

            SpawnCashUpdatedEvent(commandBuffer, cashOnWaveEnd, float2.zero, true);
        }

        public int SecondChance(Mission mission, int currentWave, EntityManager commandBuffer, float2 position)
        {
            int fullCash = 0;

            if (currentWave > -1)
            {
                if (currentWave >= mission.CashPerWaveStart.Count)
                    currentWave = mission.CashPerWaveStart.Count - 1;

                fullCash = mission.CashPerWaveStart[currentWave];

                foreach (SpawnGroup spawnGroup in mission.SpawnData)
                {
                    foreach (Wave wave in spawnGroup.Waves)
                    {
                        if (wave.WaveNum == currentWave)
                            fullCash += wave.Count * wave.CashReward;
                    }
                }

                fullCash = (int)(fullCash * cashOnSecondChanceModifier);
            }

            Cash += fullCash;
            SpawnCashUpdatedEvent(commandBuffer, fullCash, position);

            return fullCash;
        }
    }
}