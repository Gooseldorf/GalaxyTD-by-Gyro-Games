using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Jobs;
using System;
using Unity.Collections;
using Unity.Entities;
using ECSTest.Components;
using Unity.Mathematics;
using CardTD.Utilities;

[BurstCompile]
public class FlowFieldVisualizator : MonoBehaviour
{
    public enum ShowingDirection { InDirection, OutDirection };

    [SerializeField, FoldoutGroup("Mesh & Material")]
    private Material flowFieldMaterial;
    [SerializeField, FoldoutGroup("Mesh & Material")]
    private Material creepObstacleMaterial;
    [SerializeField]
    private ShowingDirection direction;

    private bool isFieldShowing = false;
    private bool isObstacleShowing = false;

    private BatchRendererGroup mBRG;

    private FlowFieldBatchData fieldBatchData;
    private BatchData creepObstacleData;

    private int cellsCount;

    private bool isInitialized;

    private EntityQuery creepQuery;

    public ShowingDirection Direction
    {
        get => direction;
        set => direction = value;
    }

    private void Init()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery baseFieldQuery = entityManager.CreateEntityQuery(typeof(BaseFlowField));
        entityManager.CompleteDependencyBeforeRO<BaseFlowField>();
        BaseFlowField ch = baseFieldQuery.GetSingleton<BaseFlowField>();
        cellsCount = ch.Cells.Length;
        mBRG = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);
        fieldBatchData = new FlowFieldBatchData(flowFieldMaterial, GameServices.Instance.RenderDataHolder.Quad, mBRG, 0, cellsCount);
        creepObstacleData = new BatchData(creepObstacleMaterial, GameServices.Instance.RenderDataHolder.Quad, mBRG, 10000);

        isInitialized = true;

        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        creepQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<PositionComponent, CreepComponent, RoundObstacle>()
            .Build(manager);
    }

    private void OnDestroy()
    {
        if (isInitialized)
        {
            mBRG.Dispose();
            fieldBatchData.Dispose();
            creepObstacleData.Dispose();
        }
    }

    [Button, HideIf("isFieldShowing")]
    public void ShowFlowField()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
            isFieldShowing = true;
    }

    [Button, ShowIf("isFieldShowing")]
    public void HideFlowField()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
            isFieldShowing = false;
    }

    [Button, HideIf("isObstacleShowing")]
    public void ShowObstacles()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
            isObstacleShowing = true;
    }

    [Button, ShowIf("isObstacleShowing")]
    public void HideObstacles()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
            isObstacleShowing = false;
    }

    [BurstCompile]
    private struct CalculateCreepObstacles : IJobParallelFor
    {
        [Unity.Collections.ReadOnly, DeallocateOnJobCompletion] public NativeArray<RoundObstacle> Obstacles;
        [Unity.Collections.ReadOnly, DeallocateOnJobCompletion] public NativeArray<PositionComponent> Positions;

        [Unity.Collections.ReadOnly] public int IndexAddressObjectToWorld;
        [Unity.Collections.ReadOnly] public int IndexAddressWorldToObject;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<float> DataBuffer;
        public void Execute(int i)
        {
            float4x4 matr = float4x4.TRS(new float3(Positions[i].Position.x, Positions[i].Position.y, 0),
                quaternion.Euler(0, 0, Utilities.SignedAngleBetween(new float2(1, 0), Positions[i].Direction)),
                new float3(Obstacles[i].Range * 2, Obstacles[i].Range * 2, 1));

            BRGSystem.FillDataBuffer(ref DataBuffer, IndexAddressObjectToWorld, i, matr);

            float4x4 inverse = math.inverse(matr);

            BRGSystem.FillDataBuffer(ref DataBuffer, IndexAddressWorldToObject, i, inverse);
        }
    }

    [BurstCompile]
    private struct CalculateArrows : IJobParallelFor
    {
        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<float> DataBuffer;
        [Unity.Collections.ReadOnly]
        public NativeArray<Cell> Cells;
        [Unity.Collections.ReadOnly]
        public NativeArray<float2> InDirections;
        [Unity.Collections.ReadOnly]
        public NativeArray<float2> OutDirections;
        [Unity.Collections.ReadOnly]
        public int FieldWidth;
        [Unity.Collections.ReadOnly]
        public int IndexAddressObjectToWorld;
        [Unity.Collections.ReadOnly]
        public int IndexAddressWorldToObject;
        [Unity.Collections.ReadOnly]
        public int IndexAddressColor;
        [Unity.Collections.ReadOnly]
        public bool IsInDirection;

        public void Execute(int i)
        {
            if (FieldWidth == 0)
                return;

            float4x4 matr = float4x4.TRS(new float3((i % FieldWidth) + .5f, (i / FieldWidth) + .5f, 0),
                                            quaternion.Euler(GetEulerAngle(IsInDirection ? InDirections[i].ToFloat3() : OutDirections[i].ToFloat3())),
                                            new float3(1, 1, 1));

            BRGSystem.FillDataBuffer(ref DataBuffer, IndexAddressObjectToWorld, i, matr);

            float4x4 inverse = math.inverse(matr);

            BRGSystem.FillDataBuffer(ref DataBuffer, IndexAddressWorldToObject, i, inverse);


            if ((IsInDirection && InDirections[i].x == 0 && InDirections[i].y == 0) || (!IsInDirection && OutDirections[i].x == 0 && OutDirections[i].y == 0))
                BRGSystem.FillDataBuffer(ref DataBuffer, IndexAddressColor, i, new float4(1, 0, 0, 1));

            else
                BRGSystem.FillDataBuffer(ref DataBuffer, IndexAddressColor, i, new float4(0, IsInDirection ? 1 : 0, IsInDirection ? 0 : 1, Cells[i].IsWall ? 0 : 1));

            float3 GetEulerAngle(float3 dir) => new float3(0, 0, math.atan2(dir.y, dir.x));
        }
    }

    [BurstCompile]
    public unsafe JobHandle OnPerformCulling(
       BatchRendererGroup rendererGroup,
       BatchCullingContext cullingContext,
       BatchCullingOutput cullingOutput,
       IntPtr userContext)
    {
        if (!isFieldShowing && !isObstacleShowing
#if UNITY_EDITOR
            && Application.isPlaying
#endif
            )
            return new JobHandle();

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        entityManager.CompleteDependencyBeforeRO<BaseFlowField>();
        EntityQuery baseFieldQuery = entityManager.CreateEntityQuery(typeof(BaseFlowField));
        BaseFlowField ch = baseFieldQuery.GetSingleton<BaseFlowField>();

        entityManager.CompleteDependencyBeforeRO<InFlowField>();
        EntityQuery inFieldQuery = entityManager.CreateEntityQuery(typeof(InFlowField));
        InFlowField inFlowField = inFieldQuery.GetSingleton<InFlowField>();

        entityManager.CompleteDependencyBeforeRO<OutFlowField>();
        EntityQuery outFieldQuery = entityManager.CreateEntityQuery(typeof(OutFlowField));
        OutFlowField outFlowField = outFieldQuery.GetSingleton<OutFlowField>();

        int entitiesCount = 0;
        int visibleOffset = 0;
        int drawCommandIndex = 0;
        int drawCommandsCount = 0;
        int creepsCount = 0;

        if (isFieldShowing)
        {
            entitiesCount += cellsCount;
            drawCommandsCount += fieldBatchData.GetCommandsCount(cellsCount);
        }

        if (isObstacleShowing)
        {
            creepsCount = creepQuery.CalculateEntityCount();
            entitiesCount += creepsCount;
            drawCommandsCount += creepObstacleData.GetCommandsCount(creepsCount);
        }

        BatchCullingOutputDrawCommands* drawCommands = BRGSystem.AllocateMemory(cullingOutput, entitiesCount, drawCommandsCount);
        // Configure the single draw range to cover the single draw command which
        // is at offset 0.
        drawCommands->drawRanges[0].drawCommandsBegin = 0;
        drawCommands->drawRanges[0].drawCommandsCount = (uint)drawCommandsCount;

        // This example doesn't care about shadows or motion vectors, so it leaves everything
        // at the default zero values, except the renderingLayerMask which it sets to all ones
        // so Unity renders the instances regardless of mask settings.
        drawCommands->drawRanges[0].filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff, };

        if (isFieldShowing)
        {
            NativeArray<float> bufferArray = fieldBatchData.LockGPUArray(cellsCount);

            CalculateArrows calculateArrows = new CalculateArrows
            {
                DataBuffer = bufferArray,
                Cells = ch.Cells,
                FieldWidth = ch.Width,
                InDirections = inFlowField.Directions,
                OutDirections = outFlowField.Directions,
                IndexAddressObjectToWorld = fieldBatchData.ByteAddressObjectToWorld / sizeof(float),
                IndexAddressWorldToObject = fieldBatchData.ByteAddressWorldToObject / sizeof(float),
                IndexAddressColor = fieldBatchData.ByteAddressColor / sizeof(float),
                IsInDirection = direction == ShowingDirection.InDirection
            };

            JobHandle jobHandle = calculateArrows.Schedule(cellsCount, 64);
            jobHandle.Complete();

            fieldBatchData.CreateDrawCommands(drawCommands, ref drawCommandIndex, cellsCount, ref visibleOffset);
            fieldBatchData.UnlockGPUArray();
        }

        if (isObstacleShowing)
        {
            NativeArray<float> bufferArray = creepObstacleData.LockGPUArray(creepsCount);

            NativeArray<RoundObstacle> obstacles = creepQuery.ToComponentDataArray<RoundObstacle>(Allocator.TempJob);
            NativeArray<PositionComponent> positions = creepQuery.ToComponentDataArray<PositionComponent>(Allocator.TempJob);

            CalculateCreepObstacles calculateObstacles = new CalculateCreepObstacles
            {
                Positions = positions,
                Obstacles = obstacles,
                DataBuffer = bufferArray,
                IndexAddressObjectToWorld = creepObstacleData.ByteAddressObjectToWorld / sizeof(float),
                IndexAddressWorldToObject = creepObstacleData.ByteAddressWorldToObject / sizeof(float),
            };

            JobHandle jobHandle = calculateObstacles.Schedule(creepsCount, 64);
            jobHandle.Complete();

            creepObstacleData.CreateDrawCommands(drawCommands, ref drawCommandIndex, creepsCount, ref visibleOffset);
            creepObstacleData.UnlockGPUArray();
        }

        return new JobHandle();
    }

    private class FlowFieldBatchData : BatchData
    {
        public int ByteAddressColor;

        public override int BytesPerInstance => (kSizeOfPackedMatrix * 2) + kSizeOfFloat4;

        public FlowFieldBatchData(Material material, Mesh mesh, BatchRendererGroup mBRG, int sortingOrder = 0, int maxInstances = 10000)
            : base(material, mesh, mBRG, sortingOrder, maxInstances)
        {
        }

        protected override NativeArray<MetadataValue> GetMetaData()
        {
            ByteAddressObjectToWorld = kSizeOfPackedMatrix * 2;
            ByteAddressWorldToObject = ByteAddressObjectToWorld + kSizeOfPackedMatrix * instancesPerWindow;
            ByteAddressColor = ByteAddressWorldToObject + kSizeOfPackedMatrix * instancesPerWindow;

            NativeArray<MetadataValue> metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
            metadata[0] = new MetadataValue
            {
                NameID = Shader.PropertyToID("unity_ObjectToWorld"),
                Value = (uint)(0x80000000 | ByteAddressObjectToWorld)
            };
            metadata[1] = new MetadataValue
            {
                NameID = Shader.PropertyToID("unity_WorldToObject"),
                Value = (uint)(0x80000000 | ByteAddressWorldToObject)
            };
            metadata[2] = new MetadataValue
            {
                NameID = Shader.PropertyToID("_BaseColor"),
                Value = (uint)(0x80000000 | ByteAddressColor)
            };

            return metadata;
        }
    }
}
