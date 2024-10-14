using NativeTrees;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static CreepsCacheBuildSystem;
using static Unity.Mathematics.math;
using Unity.Jobs.LowLevel.Unsafe;
using static NativeTrees.NativeQuadtree<CreepInfo>;
using static AllEnums;
using float2 = Unity.Mathematics.float2;
using System;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(CreepsCacheBuildSystem))]
public partial struct CreepsLocatorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CreepsLocator>();
        state.RequireForUpdate<CreepsLocatorCache>();
    }

    public static void Init(World world, float2 botLeftCorner, float2 topRightCorner, int width, int height)
    {
        AABB2D bounds = new(botLeftCorner, topRightCorner);
        //TODO: check if singleton exists and if it does replace it with new one
        world.EntityManager.CreateSingleton(new CreepsLocator(bounds, width, height));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UpdateCreepsLocatorJob job = new()
        {
            CreepsLocator = SystemAPI.GetSingletonRW<CreepsLocator>().ValueRW,
            CreepsLocatorCache = SystemAPI.GetSingleton<CreepsLocatorCache>(),
        };
        state.Dependency = job.Schedule(state.Dependency);
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct UpdateCreepsLocatorJob : IJob
{
    public CreepsLocator CreepsLocator;
    [ReadOnly] public CreepsLocatorCache CreepsLocatorCache;

    public void Execute()
    {
        CreepsLocator.CreepsTree.CopyFrom(CreepsLocatorCache.CreepsTree);
        CreepsLocator.CreepHashMap.Clear();
        CreepsLocator.FastMap.Clear();

        foreach (var pair in CreepsLocatorCache.CreepEntities)
        {
            CreepsLocator.CreepHashMap.Add(pair.Key, pair.Value);
            CreepsLocator.FastMap.Add(CreepsLocator.Index(pair.Value.Position), pair.Value);
        }
    }
}

/// <summary>
/// Singleton Used to locate Creeps
/// </summary>
public struct CreepsLocator : IComponentData, IDisposable
{
    public NativeQuadtree<CreepInfo> CreepsTree;
    public NativeHashMap<Entity, CreepInfo> CreepHashMap;
    public NativeParallelMultiHashMap<int, CreepInfo> FastMap;
    public int Width;
    public int Height;

    public int Index(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return -1;

        return x + y * Width;
    }

    public int Index(float2 position)
    {
        return Index((int)position.x, (int)position.y);
    }

    public bool HasEntity(Entity entity)
    {
        return CreepHashMap.ContainsKey(entity);
    }

    public CreepInfo GetCreepInfo(Entity entity)
    {
        return CreepHashMap[entity];
    }

    public void LocateNearestCreeps(float2 position, float distance, ref NativeList<CreepInfo> list, Entity entity,int maxSize)
    {
        NearestCreepVisitor nearestCreepVisitor = new() { Nearest = list, Set = GetSet(JobsUtility.ThreadIndex), MaxSize = maxSize};
        nearestCreepVisitor.Set.Add(entity);
        LocateNearestCreeps(nearestCreepVisitor, position, distance);
    }

    public void LocateNearestCreeps(float2 position, float distance, ref NativeList<CreepInfo> list,int maxSize)
    {
        NearestCreepVisitor nearestCreepVisitor = new() { Nearest = list, Set = GetSet(JobsUtility.ThreadIndex) ,MaxSize = maxSize};
        LocateNearestCreeps(nearestCreepVisitor, position, distance);
    }
    //add localate nearest from grid  big rang - remove from grid
    //CreepsTree.RangeAABB(); - test it

    private void LocateNearestCreeps(NearestCreepVisitor nearestCreepVisitor, float2 position, float distance)
    {
        GetQuery(JobsUtility.ThreadIndex).Nearest(ref CreepsTree, position, distance, ref nearestCreepVisitor, default(CreepsDistanceProvider));
    }

    public bool RaycastCreeps(float2 startPosition, float2 endPosition, out QuadtreeRaycastHit<CreepInfo> hit, CreepInfo creepInfo)
    {
        float2 abSegment = endPosition - startPosition;
        Ray2D ray = new(startPosition, abSegment);
        float distance = length(abSegment);
        var rayIntersecter = new RayIntersecter { AbSegment = abSegment, A = dot(abSegment, abSegment), AbSegmentLength = distance, CreepInfo = creepInfo };

        return CreepsTree.Raycast(ray, out hit, rayIntersecter, distance);
    }

    private NativeParallelHashSet<Entity> GetSet(int threadIndex)
    {
        switch (threadIndex)
        {
            case 0:
                Set0.Clear();
                return Set0;
            case 1:
                Set1.Clear();
                return Set1;
            case 2:
                Set2.Clear();
                return Set2;
            case 3:
                Set3.Clear();
                return Set3;
            case 4:
                Set4.Clear();
                return Set4;
            case 5:
                Set5.Clear();
                return Set5;
            case 6:
                Set6.Clear();
                return Set6;
            case 7:
                Set7.Clear();
                return Set7;
            case 8:
                Set8.Clear();
                return Set8;
            case 9:
                Set9.Clear();
                return Set9;
            case 10:
                Set10.Clear();
                return Set10;
            case 11:
                Set11.Clear();
                return Set11;
            case 12:
                Set12.Clear();
                return Set12;
            case 13:
                Set13.Clear();
                return Set13;
            case 14:
                Set14.Clear();
                return Set14;
            case 15:
                Set15.Clear();
                return Set15;
            case 16:
                Set16.Clear();
                return Set16;
            case 17:
                Set17.Clear();
                return Set17;
            case 18:
                Set18.Clear();
                return Set18;

            case 19: Set19.Clear(); return Set19;
            case 20: Set20.Clear(); return Set20;
            case 21: Set21.Clear(); return Set21;
            case 22: Set22.Clear(); return Set22;
            case 23: Set23.Clear(); return Set23;
            case 24: Set24.Clear(); return Set24;
            case 25: Set25.Clear(); return Set25;
            case 26: Set26.Clear(); return Set26;
            case 27: Set27.Clear(); return Set27;
            case 28: Set28.Clear(); return Set28;
            case 29: Set29.Clear(); return Set29;
            case 30: Set30.Clear(); return Set30;
            case 31: Set31.Clear(); return Set31;

            default:
                Debug.LogError("Out of threads");
                Set11.Clear();
                return Set11;
        }
    }

    private NearestNeighbourQuery GetQuery(int threadIndex)
    {
        switch (threadIndex)
        {
            case 0: return Query0;
            case 1: return Query1;
            case 2: return Query2;
            case 3: return Query3;
            case 4: return Query4;
            case 5: return Query5;
            case 6: return Query6;
            case 7: return Query7;
            case 8: return Query8;
            case 9: return Query9;
            case 10: return Query10;
            case 11: return Query11;
            case 12: return Query12;
            case 13: return Query13;
            case 14: return Query14;
            case 15: return Query15;
            case 16: return Query16;
            case 17: return Query17;
            case 18: return Query18;

            case 19: return Query19;
            case 20: return Query20;
            case 21: return Query21;
            case 22: return Query22;
            case 23: return Query23;
            case 24: return Query24;
            case 25: return Query25;
            case 26: return Query26;
            case 27: return Query27;
            case 28: return Query28;
            case 29: return Query29;
            case 30: return Query30;
            case 31: return Query31;

            default:
                Debug.LogWarning("12 Threads");
                return Query11;
        }
    }

    public CreepsLocator(AABB2D bounds, int width, int height)
    {
        Width = width;
        Height = height;
        CreepsTree = new NativeQuadtree<CreepInfo>(bounds, Allocator.Persistent);
        CreepHashMap = new NativeHashMap<Entity, CreepInfo>(20, Allocator.Persistent);
        FastMap = new NativeParallelMultiHashMap<int, CreepInfo>(200, Allocator.Persistent);
        Set0 = new NativeParallelHashSet<Entity>(20, Allocator.Persistent);
        Set1 = new NativeParallelHashSet<Entity>(20, Allocator.Persistent);
        Set2 = new NativeParallelHashSet<Entity>(20, Allocator.Persistent);
        Set3 = new NativeParallelHashSet<Entity>(20, Allocator.Persistent);
        Set4 = new NativeParallelHashSet<Entity>(20, Allocator.Persistent);
        Set5 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set6 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set7 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set8 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set9 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set10 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set11 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set12 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set13 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set14 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set15 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set16 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set17 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set18 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Query0 = new NearestNeighbourQuery(Allocator.Persistent);
        Query1 = new NearestNeighbourQuery(Allocator.Persistent);
        Query2 = new NearestNeighbourQuery(Allocator.Persistent);
        Query3 = new NearestNeighbourQuery(Allocator.Persistent);
        Query4 = new NearestNeighbourQuery(Allocator.Persistent);
        Query5 = new NearestNeighbourQuery(Allocator.Persistent);
        Query6 = new NearestNeighbourQuery(Allocator.Persistent);
        Query7 = new NearestNeighbourQuery(Allocator.Persistent);
        Query8 = new NearestNeighbourQuery(Allocator.Persistent);
        Query9 = new NearestNeighbourQuery(Allocator.Persistent);
        Query10 = new NearestNeighbourQuery(Allocator.Persistent);
        Query11 = new NearestNeighbourQuery(Allocator.Persistent);
        Query12 = new NearestNeighbourQuery(Allocator.Persistent);
        Query13 = new NearestNeighbourQuery(Allocator.Persistent);
        Query14 = new NearestNeighbourQuery(Allocator.Persistent);
        Query15 = new NearestNeighbourQuery(Allocator.Persistent);
        Query16 = new NearestNeighbourQuery(Allocator.Persistent);
        Query17 = new NearestNeighbourQuery(Allocator.Persistent);
        Query18 = new NearestNeighbourQuery(Allocator.Persistent);

        Set19 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set20 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set21 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set22 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set23 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set24 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set25 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set26 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set27 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set28 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set29 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set30 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Set31 = new NativeParallelHashSet<Entity>(2, Allocator.Persistent);
        Query19 = new NearestNeighbourQuery(Allocator.Persistent);
        Query20 = new NearestNeighbourQuery(Allocator.Persistent);
        Query21 = new NearestNeighbourQuery(Allocator.Persistent);
        Query22 = new NearestNeighbourQuery(Allocator.Persistent);
        Query23 = new NearestNeighbourQuery(Allocator.Persistent);
        Query24 = new NearestNeighbourQuery(Allocator.Persistent);
        Query25 = new NearestNeighbourQuery(Allocator.Persistent);
        Query26 = new NearestNeighbourQuery(Allocator.Persistent);
        Query27 = new NearestNeighbourQuery(Allocator.Persistent);
        Query28 = new NearestNeighbourQuery(Allocator.Persistent);
        Query29 = new NearestNeighbourQuery(Allocator.Persistent);
        Query30 = new NearestNeighbourQuery(Allocator.Persistent);
        Query31 = new NearestNeighbourQuery(Allocator.Persistent);
    }

    public void Dispose()
    {
        CreepsTree.Dispose();
        CreepHashMap.Dispose();
        FastMap.Dispose();
        Set0.Dispose(); Query0.Dispose();
        Set1.Dispose(); Query1.Dispose();
        Set2.Dispose(); Query2.Dispose();
        Set3.Dispose(); Query3.Dispose();
        Set4.Dispose(); Query4.Dispose();
        Set5.Dispose(); Query5.Dispose();
        Set6.Dispose(); Query6.Dispose();
        Set7.Dispose(); Query7.Dispose();
        Set8.Dispose(); Query8.Dispose();
        Set9.Dispose(); Query9.Dispose();
        Set10.Dispose(); Query10.Dispose();
        Set11.Dispose(); Query11.Dispose();
        Set12.Dispose(); Query12.Dispose();
        Set13.Dispose(); Query13.Dispose();
        Set14.Dispose(); Query14.Dispose();
        Set15.Dispose(); Query15.Dispose();
        Set16.Dispose(); Query16.Dispose();
        Set17.Dispose(); Query17.Dispose();
        Set18.Dispose(); Query18.Dispose();

        Set19.Dispose(); Query19.Dispose();
        Set20.Dispose(); Query20.Dispose();
        Set21.Dispose(); Query21.Dispose();
        Set22.Dispose(); Query22.Dispose();
        Set23.Dispose(); Query23.Dispose();
        Set24.Dispose(); Query24.Dispose();
        Set25.Dispose(); Query25.Dispose();
        Set26.Dispose(); Query26.Dispose();
        Set27.Dispose(); Query27.Dispose();
        Set28.Dispose(); Query28.Dispose();
        Set29.Dispose(); Query29.Dispose();
        Set30.Dispose(); Query30.Dispose();
        Set31.Dispose(); Query31.Dispose();
        //Debug.LogError($"----> CreepsLocator Disposed");
    }

    #region Sets

    public NativeParallelHashSet<Entity> Set0;
    public NativeParallelHashSet<Entity> Set1;
    public NativeParallelHashSet<Entity> Set2;
    public NativeParallelHashSet<Entity> Set3;
    public NativeParallelHashSet<Entity> Set4;
    public NativeParallelHashSet<Entity> Set5;
    public NativeParallelHashSet<Entity> Set6;
    public NativeParallelHashSet<Entity> Set7;
    public NativeParallelHashSet<Entity> Set8;
    public NativeParallelHashSet<Entity> Set9;
    public NativeParallelHashSet<Entity> Set10;
    public NativeParallelHashSet<Entity> Set11;
    public NativeParallelHashSet<Entity> Set12;
    public NativeParallelHashSet<Entity> Set13;
    public NativeParallelHashSet<Entity> Set14;
    public NativeParallelHashSet<Entity> Set15;
    public NativeParallelHashSet<Entity> Set16;
    public NativeParallelHashSet<Entity> Set17;
    public NativeParallelHashSet<Entity> Set18;

    public NativeParallelHashSet<Entity> Set19;
    public NativeParallelHashSet<Entity> Set20;
    public NativeParallelHashSet<Entity> Set21;
    public NativeParallelHashSet<Entity> Set22;
    public NativeParallelHashSet<Entity> Set23;
    public NativeParallelHashSet<Entity> Set24;
    public NativeParallelHashSet<Entity> Set25;
    public NativeParallelHashSet<Entity> Set26;
    public NativeParallelHashSet<Entity> Set27;
    public NativeParallelHashSet<Entity> Set28;
    public NativeParallelHashSet<Entity> Set29;
    public NativeParallelHashSet<Entity> Set30;
    public NativeParallelHashSet<Entity> Set31;

    #endregion

    #region Querys

    public NearestNeighbourQuery Query0;
    public NearestNeighbourQuery Query1;
    public NearestNeighbourQuery Query2;
    public NearestNeighbourQuery Query3;
    public NearestNeighbourQuery Query4;
    public NearestNeighbourQuery Query5;
    public NearestNeighbourQuery Query6;
    public NearestNeighbourQuery Query7;
    public NearestNeighbourQuery Query8;
    public NearestNeighbourQuery Query9;
    public NearestNeighbourQuery Query10;
    public NearestNeighbourQuery Query11;
    public NearestNeighbourQuery Query12;
    public NearestNeighbourQuery Query13;
    public NearestNeighbourQuery Query14;
    public NearestNeighbourQuery Query15;
    public NearestNeighbourQuery Query16;
    public NearestNeighbourQuery Query17;
    public NearestNeighbourQuery Query18;

    public NearestNeighbourQuery Query19;
    public NearestNeighbourQuery Query20;
    public NearestNeighbourQuery Query21;
    public NearestNeighbourQuery Query22;
    public NearestNeighbourQuery Query23;
    public NearestNeighbourQuery Query24;
    public NearestNeighbourQuery Query25;
    public NearestNeighbourQuery Query26;
    public NearestNeighbourQuery Query27;
    public NearestNeighbourQuery Query28;
    public NearestNeighbourQuery Query29;
    public NearestNeighbourQuery Query30;
    public NearestNeighbourQuery Query31;


    #endregion
}

public struct RayIntersecter : IQuadtreeRayIntersecter<CreepInfo>
{
    public float2 AbSegment;
    public float A;
    public float AbSegmentLength;
    public CreepInfo CreepInfo;

    public bool IntersectRay(in PrecomputedRay2D ray, CreepInfo creep, AABB2D objBounds,
        out float2 closestNormal, out float distance)
    {
        distance = 0;
        closestNormal = default;

        if (CreepInfo.Entity == creep.Entity)
            return false;

        float2 caSegment = ray.origin - creep.Position;

        float b = 2 * dot(caSegment, AbSegment);
        float c = dot(caSegment, caSegment) - creep.CollisionRange * creep.CollisionRange;

        float discriminant = b * b - 4 * A * c;
        if (discriminant >= 0)
        {
            float x = (-b - sqrt(discriminant)) / (2 * A);

            if (x > 0 && x <= 1)
            {
                distance = x * AbSegmentLength;
                closestNormal = (creep.Position - (ray.origin + x * AbSegment)) / creep.CollisionRange;
                return true;
            }
        }

        closestNormal = default;
        distance = float.PositiveInfinity;
        return false;
    }
}

public struct CreepInfo
{
    public Entity Entity;
    public ObstacleType ObstacleType;
    public FleshType FleshType;
    public ArmorType ArmorType;
    public float2 Position;

    public float2 Velocity;
    public float2 NormVelocity;
    public float Mass;
    public float CollisionRange;
    public float KineticEnergy;
    public bool IsGoingIn;

    public AABB2D GetBounds => new(Position - CollisionRange, Position + CollisionRange);
}

/// <summary>
///returns Cree distance to the point from creep border(taking in consideration creep Range
/// </summary>
public struct CreepsDistanceProvider : IQuadtreeDistanceProvider<CreepInfo>
{
    public float DistanceSquared(float2 point, CreepInfo creep, AABB2D bounds)
    {
        return math.lengthsq(creep.Position - point);
    }
}


struct NearestCreepVisitor : IQuadtreeNearestVisitor<CreepInfo>
{
    public NativeList<CreepInfo> Nearest;
    public NativeParallelHashSet<Entity> Set;
    public int MaxSize;
    
    public bool OnVist(CreepInfo creepInfo)
    {
        //TODO: optimization
        if (Set.Count() > MaxSize)
            return false;
        
        if (Set.Add(creepInfo.Entity))
        {
            Nearest.Add(creepInfo);
        }

        return true;
    }
}