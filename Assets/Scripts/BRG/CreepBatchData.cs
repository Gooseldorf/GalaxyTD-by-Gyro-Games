using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using ECSTest.Components;

public class CreepBatchData : BatchData
{
    public int ByteAddressColor;
    public int ByteAddressUV;
    public int ByteAddressBlink;
    public int ByteAddressOutline;
    public int SortingOrder;

    public bool IsLockedForWrite { get; private set; } = false;

    public AllEnums.CreepType CreepType;

    public NativeArray<AnimationFrameData> AnimationTableRun;
    public NativeArray<AnimationFrameData> AnimationTableDeath;

    // obj to world, world to obj, color, uv, is blink, is outline
    // ((12 * 2) + (4 * 2) + 2) * 4(sizeoffloat) = 136 byte
    public override int BytesPerInstance => (kSizeOfPackedMatrix * 2) + kSizeOfFloat4 * 2 + sizeof(float) * 2;

    public CreepBatchData(Material material, Mesh mesh, BatchRendererGroup mBRG, AllEnums.CreepType creepType,
        NativeArray<AnimationFrameData> animationTableRun, NativeArray<AnimationFrameData> animationTableDeath, 
        int sortingOrder = 0, int maxInstances = 10000)
        : base(material, mesh, mBRG, sortingOrder, maxInstances)
    {
        CreepType = creepType;
        AnimationTableRun = animationTableRun;
        AnimationTableDeath = animationTableDeath;
    }

    protected override NativeArray<MetadataValue> GetMetaData()
    {
        // calculating and caching pointers (in bytes) to different data
        // pointer (in bytes) to ObjectToWorld matrix
        ByteAddressObjectToWorld = kSizeOfPackedMatrix * 2;
        // pointer (in bytes) to WorldToObject matrix
        ByteAddressWorldToObject = ByteAddressObjectToWorld + kSizeOfPackedMatrix * instancesPerWindow;
        // pointer (in bytes) to Color value
        ByteAddressColor = ByteAddressWorldToObject + kSizeOfPackedMatrix * instancesPerWindow;
        // pointer (in bytes) to UV value
        ByteAddressUV = ByteAddressColor + kSizeOfFloat4 * instancesPerWindow;
        // pointer (in bytes) to blink value
        ByteAddressBlink = ByteAddressUV + kSizeOfFloat4 * instancesPerWindow;
        // pointer (in bytes) to outline value
        ByteAddressOutline = ByteAddressBlink + sizeof(float) * instancesPerWindow;

        // for GPU we need to explain what data we pass in GPU buffer, we create metadata array to explain in which byte starts particular data, using pointers
        // we pass 4 types of data, so we create array of 4 to explain it
        NativeArray<MetadataValue> metadata = new NativeArray<MetadataValue>(6, Allocator.Temp);
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
        metadata[3] = new MetadataValue
        {
            NameID = Shader.PropertyToID("_MainTex_UV"),
            Value = (uint)(0x80000000 | ByteAddressUV)
        };
        metadata[4] = new MetadataValue
        {
            NameID = Shader.PropertyToID("_IsBlink"),
            Value = (uint)(0x80000000 | ByteAddressBlink)
        };
        metadata[5] = new MetadataValue
        {
            NameID = Shader.PropertyToID("_IsOutline"),
            Value = (uint)(0x80000000 | ByteAddressOutline)
        };

        return metadata;
    }

    public override void Dispose()
    {
        base.Dispose();
        AnimationTableRun.Dispose();
        AnimationTableDeath.Dispose();
    }

    public override NativeArray<float> LockGPUArray(int entitiesCount)
    {
        IsLockedForWrite = true;
        return base.LockGPUArray(entitiesCount);
    }

    public override void UnlockGPUArray()
    {
        IsLockedForWrite = false;
        base.UnlockGPUArray();
    }
}
