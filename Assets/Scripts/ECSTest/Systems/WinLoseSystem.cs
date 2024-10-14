using Data.Managers;
using DG.Tweening;
using ECSTest.Aspects;
using ECSTest.Components;
using ECSTest.Systems;
using Managers;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct WinLoseSystem : ISystem
{
    EntityQuery unitsQuery;
    EntityQuery energyCoreQuery;
    private bool gameEnded;

    private const int countToPreLoad = 5;
    private const int countUnitToPreload = 50;
    private bool isPreloaded;

    private EntityQuery powerCellsQuery;

    public void OnCreate(ref SystemState state)
    {
        unitsQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<CreepComponent>()
            .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
            .Build(ref state);

        energyCoreQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<EnergyCoreComponent>()
            .Build(ref state);

        powerCellsQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<PowerCellComponent, DestroyComponent>()
            .Build(ref state);

        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        var powerCells = powerCellsQuery.ToComponentDataArray<DestroyComponent>(Allocator.Temp);
        int powerCellsCount = powerCells.Length;

        foreach (var powerCell in powerCells)
        {
            if (powerCell.IsNeedToDestroy)
                powerCellsCount--;
        }

        powerCells.Dispose();

        // var energyCores = energyCoreQuery.ToComponentDataArray<EnergyCoreComponent>(Allocator.Temp);
        // int powerCellsCount = 0;
        // for (int i = 0; i < energyCores.Length; i++)
        //     powerCellsCount += energyCores[i].PowerCellCount;
        // energyCores.Dispose();

        if (powerCellsCount > 0 && gameEnded)
        {
            gameEnded = false;
            TouchCamera.Instance.CanDrag = true;
            isPreloaded = false;
        }

        if (gameEnded)
            return;

        if (!isPreloaded && powerCellsCount < countToPreLoad)
        {
            AdsManager.LoadReward(AdsRewardType.SecondChance);
            isPreloaded = true;
        }

        if (powerCellsCount <= 0)
        {
            gameEnded = true;
            TouchCamera.Instance.CanDrag = false;
            GameServices.Instance.LoseMission();
            return;
        }

        if (!isPreloaded && unitsQuery.CalculateEntityCount() <= countUnitToPreload)
        {
            AdsManager.LoadReward(AdsRewardType.IncreaseReward);
            isPreloaded = true;
        }

        if (unitsQuery.CalculateChunkCount() <= 0)
        {
            gameEnded = true;

            ReturnAllPowerCells(ref state);

            TouchCamera.Instance.CanDrag = false;
            DOVirtual.DelayedCall(1, () => GameServices.Instance.WinMission(powerCellsCount));
            state.Enabled = false;
        }
    }

    private void ReturnAllPowerCells(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer buffer = singleton.CreateCommandBuffer(state.WorldUnmanaged);
        foreach (PowerCellAspect powerCell in SystemAPI.Query<PowerCellAspect>())
        {
            if (powerCell.PowerCellComponent.ValueRO.IsMoves && !powerCell.DestroyComponent.ValueRO.IsNeedToDestroy)
            {
                PowerCellSystemBase.AttachToCore(state.EntityManager, powerCell, buffer);
                PowerCellSystemBase.ShowBubble(buffer, powerCell.Entity, false);
            }
        }
    }
}