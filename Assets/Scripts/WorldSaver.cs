using ECSTest.Components;
using ECSTest.Systems;
using System;
using Unity.Entities;

public static class WorldSaver
{
    private static BaseFlowField baseFlowField;

    private static InFlowFieldCache inFlowFieldCache;
    private static OutFlowFieldCache outFlowFieldCache;

    private static InFlowField inFlowField;
    private static OutFlowField outFlowField;

    private static CashComponent cashComponent;

    private static RandomComponent randomComponent;

    private static TimeSkipper timeSkipperComponent;

    public static void Dispose(EntityManager manager)
    {
        DisposeSavedComponents();
        DisposeSingletons(manager);
    }

    public static void SaveSingletons(EntityManager manager)
    {
        DisposeSavedComponents();
        manager.CompleteDependencyBeforeRO<BaseFlowField>();
        var baseFlow = GetSingleton<BaseFlowField>(manager);
        baseFlowField = baseFlow.ValueRO.Clone();

        manager.CompleteDependencyBeforeRO<InFlowFieldCache>();
        var inCache = GetSingleton<InFlowFieldCache>(manager);
        inFlowFieldCache = inCache.ValueRO.Clone();
        manager.CompleteDependencyBeforeRO<OutFlowFieldCache>();
        var outCache = GetSingleton<OutFlowFieldCache>(manager);
        outFlowFieldCache = outCache.ValueRO.Clone();

        manager.CompleteDependencyBeforeRO<InFlowField>();
        var inDir = GetSingleton<InFlowField>(manager);
        inFlowField = inDir.ValueRO.Clone();
        manager.CompleteDependencyBeforeRO<OutFlowField>();
        var outDir = GetSingleton<OutFlowField>(manager);
        outFlowField = outDir.ValueRO.Clone();

        manager.CompleteDependencyBeforeRO<CashComponent>();
        var cashComp = GetSingleton<CashComponent>(manager);
        cashComponent = cashComp.ValueRO.Clone();

        manager.CompleteDependencyBeforeRO<RandomComponent>();
        var randComp = GetSingleton<RandomComponent>(manager);
        randomComponent = randComp.ValueRO.Clone();

        manager.CompleteDependencyBeforeRO<TimeSkipper>();
        var timeSkipperComp = GetSingleton<TimeSkipper>(manager);
        timeSkipperComponent = timeSkipperComp.ValueRO.Clone();

        //PowerSystemBase.ConnectedPowerablesComponent
        //CreepsCacheBuildSystem.CreepsLocatorCache 
        //CreepsLocator
        //ObstaclesLocatorCache
        //ObstaclesLocator

        // Debug.LogError("---------> Singletons saved");
    }

    public static void LoadSingletons(EntityManager manager)
    {
        GetSingleton<BaseFlowField>(manager).ValueRW.Load(baseFlowField);

        GetSingleton<InFlowFieldCache>(manager).ValueRW.Load(inFlowFieldCache);
        GetSingleton<OutFlowFieldCache>(manager).ValueRW.Load(outFlowFieldCache);

        GetSingleton<InFlowField>(manager).ValueRW.Load(inFlowField);
        GetSingleton<OutFlowField>(manager).ValueRW.Load(outFlowField);

        GetSingleton<CashComponent>(manager).ValueRW.Load(cashComponent);

        GetSingleton<RandomComponent>(manager).ValueRW.Load(randomComponent);

        GetSingleton<TimeSkipper>(manager).ValueRW.Load(timeSkipperComponent);


        RefRW<CreepsLocator> creepsLocator = GetSingleton<CreepsLocator>(manager);
        creepsLocator.ValueRW.CreepsTree.Clear();
        creepsLocator.ValueRW.CreepHashMap.Clear();

        RefRW<CreepsCacheBuildSystem.CreepsLocatorCache> creepsLocatorCache = GetSingleton<CreepsCacheBuildSystem.CreepsLocatorCache>(manager);
        creepsLocatorCache.ValueRW.CreepsTree.Clear();
        creepsLocatorCache.ValueRW.CreepEntities.Clear();

        RefRW<ObstaclesLocatorCache> obstaclesLocatorCache = GetSingleton<ObstaclesLocatorCache>(manager);
        obstaclesLocatorCache.ValueRW.ObstaclesTree.Clear();

        RefRW<ObstaclesLocator> obstaclesLocator = GetSingleton<ObstaclesLocator>(manager);
        obstaclesLocator.ValueRW.ObstaclesTree.Clear();

        // Debug.LogError("---------> Singletons restored");
    }

    private static void DisposeSavedComponents()
    {
        if (baseFlowField.Cells.IsCreated)
            baseFlowField.Dispose();

        if (inFlowFieldCache.Directions.IsCreated)
            inFlowFieldCache.Dispose();
        if (outFlowFieldCache.Directions.IsCreated)
            outFlowFieldCache.Dispose();

        if (inFlowField.Directions.IsCreated)
            inFlowField.Dispose();
        if (outFlowField.Directions.IsCreated)
            outFlowField.Dispose();

        if (cashComponent.IsCreated)
            cashComponent.Dispose();

        if (randomComponent.Randoms.IsCreated)
            randomComponent.Dispose();

        if (timeSkipperComponent.IsCreated)
            timeSkipperComponent.Dispose();

        //Debug.LogError("---------> WorldSaver disposed");
    }

    private static void DisposeSingletons(EntityManager manager)
    {
        GetSingleton<BaseFlowField>(manager).ValueRW.Dispose();

        GetSingleton<InFlowFieldCache>(manager).ValueRW.Dispose();
        GetSingleton<OutFlowFieldCache>(manager).ValueRW.Dispose();

        GetSingleton<InFlowField>(manager).ValueRW.Dispose();
        GetSingleton<OutFlowField>(manager).ValueRW.Dispose();

        GetSingleton<CashComponent>(manager).ValueRW.Dispose();
        GetSingleton<TimeSkipper>(manager).ValueRW.Dispose();

        GetSingleton<RandomComponent>(manager).ValueRW.Dispose();

        GetSingleton<PowerSystemBase.ConnectedPowerablesComponent>(manager).ValueRW.Dispose();

        GetSingleton<CreepsCacheBuildSystem.CreepsLocatorCache>(manager).ValueRW.Dispose();
        GetSingleton<CreepsLocator>(manager).ValueRW.Dispose();
        GetSingleton<ObstaclesLocatorCache>(manager).ValueRW.Dispose();
        GetSingleton<ObstaclesLocator>(manager).ValueRW.Dispose();

        // Debug.LogError("---------> Singletons disposed");
    }

    private static RefRW<T> GetSingleton<T>(EntityManager manager) where T : unmanaged, IComponentData
    {
        manager.CompleteDependencyBeforeRW<T>();
        EntityQuery entityQuery = manager.CreateEntityQuery(new ComponentType[] { typeof(T) });
        return entityQuery.GetSingletonRW<T>();
    }

#if UNITY_EDITOR
    public static void DisposeAndDelete(EntityManager manager)
    {
        DisposeSavedComponents();
        DestroySingleton<BaseFlowField>(manager);

        DestroySingleton<InFlowFieldCache>(manager);
        DestroySingleton<OutFlowFieldCache>(manager);

        DestroySingleton<InFlowField>(manager);
        DestroySingleton<OutFlowField>(manager);

        DestroySingleton<CashComponent>(manager);

        DestroySingleton<RandomComponent>(manager);

        DestroySingleton<PowerSystemBase.ConnectedPowerablesComponent>(manager);

        DestroySingleton<CreepsCacheBuildSystem.CreepsLocatorCache>(manager);
        DestroySingleton<CreepsLocator>(manager);
        DestroySingleton<ObstaclesLocatorCache>(manager);
        DestroySingleton<ObstaclesLocator>(manager);

        DestroySingleton<TimeSkipper>(manager);
    }

    private static void DestroySingleton<T>(EntityManager manager) where T : unmanaged, IComponentData, IDisposable
    {
        EntityQuery entityQuery = manager.CreateEntityQuery(new ComponentType[] { typeof(T) });
        entityQuery.GetSingleton<T>().Dispose();
        manager.DestroyEntity(entityQuery);
    }
#endif
}