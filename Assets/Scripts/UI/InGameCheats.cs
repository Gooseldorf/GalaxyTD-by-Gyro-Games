using CardTD.Utilities;
using ECSTest.Components;
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InGameCheats
{
    //variables to calculate mission loading time
    public static double MissionStartLoadTime;
    public static double MissionInitedTime;
    public static bool WinWithCheats = false;
    public static int PowerCellsForWin = 0;
    
    public static void WinLevelStars(int stars)
    {
        GameServices.Instance.SetPause(false);
        Validate(() => WinMissionWithStars(stars));
    }

    private static void WinMissionWithStars(int stars)
    {
        WinWithCheats = true;
        int maxPowerCells = 0;
        foreach (EnergyCore energyCore in GameServices.Instance.CurrentMission.EnergyCores)
        {
            maxPowerCells += energyCore.PowerCellCount;
        }

        float powerCellsToStars = stars switch
        {
            3 => (float)maxPowerCells * (2f / 3f),
            2 => (float)maxPowerCells * (1f / 3f),
            _ => 1
        };
        int powerCells = Mathf.CeilToInt((float)maxPowerCells / 2 * (stars - 1));
        PowerCellsForWin = powerCells > 0 ? powerCells : 1; //stars - 1 cos we already have 1 star 
        
        GameServices.Instance.WinMission((int)Math.Ceiling(powerCellsToStars));
        /*EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = manager.CreateEntityQuery(typeof(EnergyCoreComponent));

        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        int energyCoreCount = entityArray.Length;
        int powerCellsInOneCore = (int)Math.Ceiling(powerCellsToStars / energyCoreCount);

        foreach (var entity in entityArray)
        {
            manager.RemoveComponent<EnergyCoreComponent>(entity);
            var comp = new EnergyCoreComponent() {PowerCellCount = powerCellsInOneCore};
            manager.AddComponentData(entity, comp);
        }*/
    }

    public static void LoseLevel()
    {
        GameServices.Instance.SetPause(false);
        Validate(() => GameServices.Instance.LoseMission());
    }
    
    public static void AddCash(int cash)
    {
        Validate(() =>
        {
            var cashComponent = GameServices.Instance.GetCashComponent();
            cashComponent.AddCash(cash);
        }); 
    }

    public static void IncreaseTimeScale(float value)
    {
        Validate(() => 
        {
            float currentTimeScale = GameServices.Instance.CurrentTimeScale;
            if (currentTimeScale + value >= 100) return;
            
            GameServices.Instance.SetTimeScale(currentTimeScale + value);
        });
    }
    
    public static void DecreaseTimeScale(float value)
    {
        Validate(() =>
        {
            float currentTimeScale = GameServices.Instance.CurrentTimeScale;
            if (currentTimeScale <= 0) return;
        
            GameServices.Instance.SetTimeScale(currentTimeScale - value);
        });
    }

    public static void DoubleTimeScale()
    {
        Validate(() =>
        {
            float currentTimeScale = GameServices.Instance.CurrentTimeScale;
            if (currentTimeScale * 2 >= 100) return;
            
            GameServices.Instance.SetTimeScale(currentTimeScale * 2);
        });
    }
    
    public static void HalveTimeScale()
    {
        Validate(() =>
        {
            float currentTimeScale = GameServices.Instance.CurrentTimeScale;
            if (currentTimeScale <= 0) return;
        
            GameServices.Instance.SetTimeScale(currentTimeScale / 2);
        });
    }
    
    private static void Validate(Action action)
    {
        if (IsSceneValid())
            action();
    }

    private static bool IsSceneValid() => SceneManager.GetActiveScene().name != "MainMenu";
}
