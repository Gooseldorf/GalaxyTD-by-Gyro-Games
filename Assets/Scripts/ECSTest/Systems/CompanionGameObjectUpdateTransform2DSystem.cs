#if !UNITY_DISABLE_MANAGED_COMPONENTS
using CardTD.Utilities;
using ECSTest.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

namespace Unity.Entities
{
    internal struct CompanionGameObjectUpdateTransformCleanup2D : ICleanupComponentData
    {
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [BurstCompile]
    internal partial class CompanionGameObjectUpdateTransform2DSystem : SystemBase
    {
        private readonly ProfilerMarker profilerMarkerAddNew = new("AddNew");
        private readonly ProfilerMarker profilerMarkerRemove = new("Remove");
        private readonly ProfilerMarker profilerMarkerUpdate = new("Update");

        private struct IndexAndInstance
        {
            public int TransformAccessArrayIndex;
            public int InstanceID;
        }

        private TransformAccessArray transformAccessArray;
        private NativeList<Entity> entities;
        private NativeHashMap<Entity, IndexAndInstance> entitiesMap;
        private EntityQuery createdQuery;
        private EntityQuery destroyedQuery;
        private EntityQuery modifiedQuery;

        protected override void OnCreate()
        {
            transformAccessArray = new TransformAccessArray(0);
            entities = new NativeList<Entity>(64, Allocator.Persistent);
            entitiesMap = new NativeHashMap<Entity, IndexAndInstance>(64, Allocator.Persistent);
            createdQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Link2D>()
                .WithNone<CompanionGameObjectUpdateTransformCleanup2D>()
                .Build(this);
            destroyedQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CompanionGameObjectUpdateTransformCleanup2D>()
                .WithNone<Link2D>()
                .Build(this);
            modifiedQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Link2D>()
                .Build(this);
            modifiedQuery.SetChangedVersionFilter(typeof(Link2D));
        }

        protected override void OnDestroy()
        {
            transformAccessArray.Dispose();
            entities.Dispose();
            entitiesMap.Dispose();
        }

        private struct RemoveDestroyedEntitiesArgs
        {
            public EntityQuery DestroyedQuery;
            public NativeList<Entity> Entities;
            public NativeHashMap<Entity, IndexAndInstance> EntitiesMap;
            public TransformAccessArray TransformAccessArray;
            public EntityManager EntityManager;
        }

        [BurstCompile]
        private static void RemoveDestroyedEntities(ref RemoveDestroyedEntitiesArgs args)
        {
            var entities = args.DestroyedQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                // This check is necessary because the code for adding entities is conditional and in edge-cases where
                // objects are quickly created-and-destroyed, we might not have the entity in the map.
                if (args.EntitiesMap.TryGetValue(entity, out var indexAndInstance))
                {
                    int index = indexAndInstance.TransformAccessArrayIndex;
                    args.TransformAccessArray.RemoveAtSwapBack(index);
                    args.Entities.RemoveAtSwapBack(index);
                    args.EntitiesMap.Remove(entity);
                    if (index < args.Entities.Length)
                    {
                        var fixup = args.EntitiesMap[args.Entities[index]];
                        fixup.TransformAccessArrayIndex = index;
                        args.EntitiesMap[args.Entities[index]] = fixup;
                    }
                }
            }
            entities.Dispose();
            args.EntityManager.RemoveComponent<CompanionGameObjectUpdateTransformCleanup2D>(args.DestroyedQuery);
        }

        protected override void OnUpdate()
        {
            using (profilerMarkerAddNew.Auto())
            {
                if (!createdQuery.IsEmpty)
                {
                    var entities = createdQuery.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var entity = entities[i];
                        var link = EntityManager.GetComponentData<Link2D>(entity);

                        // It is possible that an object is created and immediately destroyed, and then this shouldn't run.
                        if (link.Companion != null)
                        {
                            IndexAndInstance indexAndInstance = default;
                            indexAndInstance.TransformAccessArrayIndex = this.entities.Length;
                            indexAndInstance.InstanceID = link.Companion.GetInstanceID();
                            if (!entitiesMap.ContainsKey(entity))
                                entitiesMap.Add(entity, indexAndInstance);
                            transformAccessArray.Add(link.Companion.transform);
                            this.entities.Add(entity);
                        }
                    }
                    entities.Dispose();
                    EntityManager.AddComponent<CompanionGameObjectUpdateTransformCleanup2D>(createdQuery);
                }
            }

            using (profilerMarkerRemove.Auto())
            {
                if (!destroyedQuery.IsEmpty)
                {
                    var args = new RemoveDestroyedEntitiesArgs
                    {
                        Entities = entities,
                        DestroyedQuery = destroyedQuery,
                        EntitiesMap = entitiesMap,
                        EntityManager = EntityManager,
                        TransformAccessArray = transformAccessArray
                    };
                    RemoveDestroyedEntities(ref args);
                }
            }

            using (profilerMarkerUpdate.Auto())
            {
                foreach (var (link, entity) in SystemAPI.Query<Link2D>().WithChangeFilter<Link2D>().WithEntityAccess())
                {
                    IndexAndInstance cached = entitiesMap[entity];
                    int currentID = link.Companion.GetInstanceID();
                    if (cached.InstanceID != currentID)
                    {
                        // We avoid the need to update the indices and reorder the entities array by adding
                        // the new transform first, and removing the old one after with a RemoveAtSwapBack.
                        // Example, replacing B with X in ABCD:
                        // 1. ABCD + X = ABCDX
                        // 2. ABCDX - B = AXCD
                        // -> the transform is updated, but the index remains unchanged
                        transformAccessArray.Add(link.Companion.transform);
                        transformAccessArray.RemoveAtSwapBack(cached.TransformAccessArrayIndex);
                        cached.InstanceID = currentID;
                        entitiesMap[entity] = cached;
                    }
                }
            }

            Dependency = new CopyTransformJob
            {
                PositionLookUp = GetComponentLookup<PositionComponent>(),
                Entities = entities
            }.Schedule(transformAccessArray, Dependency);
        }

        [BurstCompile]
        private struct CopyTransformJob : IJobParallelForTransform
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<PositionComponent> PositionLookUp;
            [ReadOnly] public NativeList<Entity> Entities;

            public void Execute(int index, TransformAccess transform)
            {
                PositionComponent position = PositionLookUp[Entities[index]];
                transform.localPosition = new Vector3(position.Position.x, position.Position.y);

                // We need to use the safe version if vectors are not be normalized
                if(!position.Direction.Equals(float2.zero))
                    transform.rotation = Utilities.Direction2DToQuaternion(position.Direction);
                else
                {
                    Debug.LogError($"position.Direction iz zero tell to Vit");
                }
            }
        }
    }
}
#endif
