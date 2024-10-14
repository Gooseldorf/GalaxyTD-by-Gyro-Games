using DefaultNamespace;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "RenderDataHolder", menuName = "ScriptableObjects/OneTime/RenderDataHolder")]
public class RenderDataHolder : ScriptableObject
{
    public Mesh Quad;
    public Material HpBarMaterial;
    public Material WarningMaterial;
    public Material StunMaterial;
    public Material FearMaterial;

    //public Material TowerBaseMaterial;
    public ShootVfxRenderStats MuzzlesRenderStats;

    [SerializeField] private TMP_FontAsset fontAsset;
    public Material FontMaterial;
    public TextAnimationDataHolder TextAnimationData;
    public int FontPointSize
    {
        get
        {
            try
            {
                return fontAsset.faceInfo.pointSize;
            }
            catch
            {
                Debug.LogError("Trouble with fontAsset");
                return 1;
            }
        }
    }

    public float ReferenceGlyphWidth
    {
        get
        {
            try
            {
                int tmpFontAssetIndex = fontAsset.characterTable.FindIndex(x => x.unicode == (uint)'0'); // == 16
                return dirtyMapSave[tmpFontAssetIndex].RectWidth / fontAsset.faceInfo.pointSize; // == 0.6056338f
            }
            catch
            {
                Debug.LogError("Trouble with fontAsset");
                return 1;
            }
        }
    }
    [SerializeField]
    private List<GlyphData> dirtyMapSave;

#if UNITY_EDITOR
    [Button]
    private void ResolveCharacterTable()
    {
        UnityEditor.EditorUtility.SetDirty(this);

        dirtyMapSave = new List<GlyphData>();
        for (int i = 0;i < fontAsset.characterTable.Count;i++)
        {
                GlyphData data = new()
                {
                    X = fontAsset.characterTable[i].glyph.glyphRect.x,
                    Y = fontAsset.characterTable[i].glyph.glyphRect.y,
                    RectWidth = fontAsset.characterTable[i].glyph.glyphRect.width,
                    RectHeight = fontAsset.characterTable[i].glyph.glyphRect.height,
                    Width = fontAsset.characterTable[i].glyph.metrics.width,
                    Height = fontAsset.characterTable[i].glyph.metrics.height,
                    HorizontalAdvance = fontAsset.characterTable[i].glyph.metrics.horizontalAdvance,
                    BearingY = fontAsset.characterTable[i].glyph.metrics.horizontalBearingY
                };
            dirtyMapSave.Add(data);
            }

        UnityEditor.AssetDatabase.SaveAssets();
    }
#endif
    public NativeHashMap<uint, GlyphData> GetGlyphData()
    {
        NativeHashMap<uint, GlyphData> map = new(fontAsset.characterTable.Count, Allocator.Persistent); //0-9 and -, +, $
        for (int i = 0;i < fontAsset.characterTable.Count;i++)
        {
            try
            {
                map.Add(fontAsset.characterTable[i].unicode, dirtyMapSave[i]);
            }
            catch
            {
                Debug.LogError("trouble with tmpChar");
                map.Add(fontAsset.characterTable[i].unicode, default);
            }
        }

        return map;
    }

    //public List<TowerIdUv> TowerBaseData;

    //public NativeHashMap<int, float4> GetTowerBaseTable()
    //{
    //    NativeHashMap<int, float4> result = new(TowerBaseData.Count, Allocator.Persistent);
    //    foreach (TowerIdUv b in TowerBaseData)
    //        result.Add((int)b.TowerId, b.UV);

    //    return result;
    //}

    //[Serializable]
    //public class TowerIdUv
    //{
    //    public AllEnums.TowerId TowerId;
    //    public float4 UV;
    //}
}

[Serializable]
public struct GlyphData
{
    public float X;
    public float Y;
    public float RectWidth;
    public float RectHeight;
    public float Width;
    public float Height;
    public float BearingY;
    public float HorizontalAdvance;
}