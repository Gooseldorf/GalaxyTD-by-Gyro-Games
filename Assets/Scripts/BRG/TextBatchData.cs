using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class TextBatchData : BatchData
{
    public int ByteAddressUV;
    public int ByteAddressColor;

    public override int BytesPerInstance => (kSizeOfPackedMatrix * 2) + (kSizeOfFloat4 * 2);

    public TextBatchData(Material material, Mesh mesh, BatchRendererGroup mBRG, int sortingOrder = 0, int maxInstances = 10000)
        : base(material, mesh, mBRG, sortingOrder, maxInstances)
    {

    }

    protected override NativeArray<MetadataValue> GetMetaData()
    {
        ByteAddressObjectToWorld = (kSizeOfPackedMatrix * 2);
        ByteAddressWorldToObject = ByteAddressObjectToWorld + kSizeOfPackedMatrix * instancesPerWindow;
        ByteAddressColor = ByteAddressWorldToObject + kSizeOfPackedMatrix * instancesPerWindow;
        ByteAddressUV = ByteAddressColor + kSizeOfFloat4 * instancesPerWindow;

        NativeArray<MetadataValue> metadata = new NativeArray<MetadataValue>(4, Allocator.Temp);
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

        return metadata;
    }
}