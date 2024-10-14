using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class ShootVfxBatchData : BatchData
{
    public int ByteAdressMazzleUV;

    public override int BytesPerInstance => (kSizeOfPackedMatrix * 2) + sizeof(float) * 4;

    public ShootVfxBatchData(Material material, Mesh mesh, BatchRendererGroup mBRG, int sortingOrder = 0, int maxInstances = 10000)
        : base(material, mesh, mBRG, sortingOrder, maxInstances)
    {

    }

    protected override NativeArray<MetadataValue> GetMetaData()
    {
        // pointer (in bytes) to ObjectToWorld matrix
        ByteAddressObjectToWorld = kSizeOfPackedMatrix * 2;
        // pointer (in bytes) to WorldToObject matrix
        ByteAddressWorldToObject = ByteAddressObjectToWorld + kSizeOfPackedMatrix * instancesPerWindow;
        // pointer (in bytes) to offset + tiling
        ByteAdressMazzleUV = ByteAddressWorldToObject + kSizeOfPackedMatrix * instancesPerWindow;

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
            NameID = Shader.PropertyToID("_MainTex_UV"),
            Value = (uint)(0x80000000 | ByteAdressMazzleUV)
        };

        return metadata;
    }
}