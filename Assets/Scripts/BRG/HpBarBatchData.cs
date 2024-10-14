using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class HpBarBatchData : BatchData
{
    public int ByteAddressHealth;
    public override int BytesPerInstance => (kSizeOfPackedMatrix * 2) + sizeof(float);
    public HpBarBatchData(Material material, Mesh mesh, BatchRendererGroup mBRG, int sortingOrder = 0, int maxInstances = 10000)
        : base(material, mesh, mBRG, sortingOrder, maxInstances)
    {

    }

    protected override NativeArray<MetadataValue> GetMetaData()
    {
        // pointer (in bytes) to ObjectToWorld matrix
        ByteAddressObjectToWorld = kSizeOfPackedMatrix * 2;
        // pointer (in bytes) to WorldToObject matrix
        ByteAddressWorldToObject = ByteAddressObjectToWorld + kSizeOfPackedMatrix * instancesPerWindow;
        // pointer (in bytes) to Color value
        ByteAddressHealth = ByteAddressWorldToObject + kSizeOfPackedMatrix * instancesPerWindow;

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
            NameID = Shader.PropertyToID("_Health"),
            Value = (uint)(0x80000000 | ByteAddressHealth)
        };
        return metadata;
    }
}