using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using Unity.Mathematics;
using ECSTest.Components;
using static AllEnums;
using System;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewShootVfxRenderStats", menuName = "ScriptableObjects/ShootVfxRenderStats")]
public class ShootVfxRenderStats : SerializedScriptableObject
{
    [BoxGroup("Render")]
    public Material MuzzlesMaterial;
    [BoxGroup("Render")]
    public Material ImpactsMaterial;
    [BoxGroup("Render")]
    public int SortingOrder = 20000;

    private Dictionary<TowerId, List<ShootVfxAnimationFrameData>> tempAnimationTableEnhunced = new();
    private Dictionary<TowerId, List<ShootVfxAnimationFrameData>> tempAnimationTableDefault = new();

    [TableList, BoxGroup("VfxTableData managed")]
    public List<VfxAnimationParams> VfxTableData = new List<VfxAnimationParams>();

    [Serializable]
    public struct VfxAnimationParams
    {
        [TableColumnWidth(100,resizable: false)]
        public TowerId TowerTypes;

        [VerticalGroup("TimeBetweenFrames"), LabelWidth(70)]
        public float Muzzle_T, Impact_T;

        [VerticalGroup("Scales"), LabelWidth(70)]
        public float Muzzle_S, Impact_S;
    }
    public List<float> GetTimeBetweenFrames(bool isMuzzle)
    {
        List<float> result = new List<float>();
        for (int i = 0; i < VfxTableData.Count; i++)
            result.Add(isMuzzle? VfxTableData[i].Muzzle_T : VfxTableData[i].Impact_T);
        
        return result;
    }


    [BoxGroup("AnimationTable"), SerializeField]
    public List<ShootVfxAnimationFrameData> MuzzleRenderFrameDatasDefault;
    [BoxGroup("AnimationTable"), SerializeField]
    public List<int> MuzzleTowerIndexes;
    
    [BoxGroup("AnimationTable"), SerializeField]
    public List<ShootVfxAnimationFrameData> ImpactRenderFrameDatasDefault;
    [BoxGroup("AnimationTable"), SerializeField]
    public List<int> ImpactTowerIndexes;
    //[BoxGroup("AnimationTable"), SerializeField]
    //public List<float> ImpactTimeBetweenFrames;


#if UNITY_EDITOR

    [Button, BoxGroup("ParseAnimationTable")]
    private void ParseMuzzlesSpriteSheetData(UnityEngine.Object file)
    {
        ParseSpriteSheetData(file, isMuzzle: true, out tempAnimationTableDefault, out tempAnimationTableEnhunced);

        MuzzleRenderFrameDatasDefault = GetMuzzleRenderFrameDatasDefault(tempAnimationTableDefault);
        MuzzleTowerIndexes = GetTowerIndexesForMuzzleRenderData(tempAnimationTableDefault);

        MuzzleRenderFrameDatasDefault.AddRange(GetMuzzleRenderFrameDatasDefault(tempAnimationTableEnhunced));
        MuzzleTowerIndexes.AddRange(GetTowerIndexesForMuzzleRenderData(tempAnimationTableEnhunced, MuzzleTowerIndexes[^1] + 1));

        EditorUtility.SetDirty(this);
    }

    [Button, BoxGroup("ParseAnimationTable")]
    private void ParseImpactsSpriteSheetData(UnityEngine.Object file)
    {
        ParseSpriteSheetData(file, isMuzzle: false, out tempAnimationTableDefault, out tempAnimationTableEnhunced);

        ImpactRenderFrameDatasDefault = GetMuzzleRenderFrameDatasDefault(tempAnimationTableDefault);
        ImpactTowerIndexes = GetTowerIndexesForMuzzleRenderData(tempAnimationTableDefault);

        //ImpactRenderFrameDatasDefault.AddRange(GetMuzzleRenderFrameDatasDefault(tempAnimationTableEnhunced));
        //ImpactTowerIndexes.AddRange(GetTowerIndexesForMuzzleRenderData(tempAnimationTableEnhunced, ImpactTowerIndexes[^1] + 1));

        EditorUtility.SetDirty(this);
    }
    private List<ShootVfxAnimationFrameData> GetMuzzleRenderFrameDatasDefault(Dictionary<TowerId, List<ShootVfxAnimationFrameData>> animationTable)
    {

        int size = 0;
        List<TowerId> dirtyKeys = new List<TowerId>();
        foreach (var key in animationTable.Keys)
        {
            dirtyKeys.Add(key);
        }


        foreach (var value in animationTable.Values)
        {
            foreach (var frame in value)
            {
                size++;
            }
        }

        List<ShootVfxAnimationFrameData> result = new(size);

        for (int i = 0; i < animationTable.Count; i++)
        {
            try
            {
                var key = dirtyKeys[i];//animationTable.GetKeyByIndex(i);

                for (int j = 0; j < animationTable[key].Count; j++)
                {
                    result.Add(animationTable[key][j]);
                }
            }
            catch
            {
                Debug.LogError("trouble with DefaultMuzzle HashMap");
            }
        }

        return result;
    }

    /// <summary>Here defaultMuzzles frameDatas for each tower</summary>
    private List<int> GetTowerIndexesForMuzzleRenderData(Dictionary<TowerId, List<ShootVfxAnimationFrameData>> animationTable, int startIndex = 0)
    {
        List<int> result = new(animationTable.Count * 2);
        List<TowerId> dirtyKeys = new List<TowerId>();
        foreach (var key in animationTable.Keys)
        {
            dirtyKeys.Add(key);
        }
        for (int i = 0; i < animationTable.Count; i++)
        {
            try
            {
                var key = dirtyKeys[i];
                result.Add(i == 0 ? startIndex : result[2 * (i - 1) + 1] + 1);
                result.Add(result[2 * i] + (animationTable[key].Count) - 1);
            }
            catch
            {
                Debug.LogError("trouble with DefaultMuzzle towerIndexes");
            }
        }
        return result;
    }

    private void ParseSpriteSheetData(UnityEngine.Object file, bool isMuzzle, out Dictionary<TowerId, List<ShootVfxAnimationFrameData>> animationTableDefault, out Dictionary<AllEnums.TowerId, List<ShootVfxAnimationFrameData>> animationTableEnhunced)
    {
        if (VfxTableData == null || VfxTableData.Count == 0)
            SetDirtyScales();

        animationTableDefault = new();
        animationTableEnhunced = new();

        foreach (TowerId enumID in Enum.GetValues(typeof(TowerId)))
        {
            animationTableDefault.Add(enumID, new List<ShootVfxAnimationFrameData>());
            animationTableEnhunced.Add(enumID, new List<ShootVfxAnimationFrameData>());
        }

        string parseFilePath = AssetDatabase.GetAssetPath(file);

        int sizeX = 0, sizeY = 0;
        bool dataStarted = false;
        string line;
        int maxHeightDefault = 0;
        int maxWidthDefault = 0;
        int maxHeightEnhunced = 0;
        int maxWidthEnhunced = 0;

        StreamReader sr = new StreamReader(parseFilePath);
        line = sr.ReadLine();
        while (line != null)
        {
            if (line.Contains(":size="))
            {
                int startIndex = line.IndexOf('=') + 1;
                int length = line.LastIndexOf('x') - startIndex;
                sizeX = int.Parse(line.Substring(startIndex, length));
                startIndex = line.LastIndexOf('x') + 1;
                length = line.Length - startIndex;
                sizeY = int.Parse(line.Substring(startIndex, length));
            }

            if (dataStarted)
            {
                SplitLineToData(line, sizeX, sizeY,
                    ref maxHeightDefault, ref maxWidthDefault, animationTableDefault,
                    ref maxHeightEnhunced, ref maxWidthEnhunced, animationTableEnhunced);
            }

            if (line == "")
            {
                dataStarted = true;
            }
            line = sr.ReadLine();
        }
        sr.Close();

        foreach (TowerId enumID in Enum.GetValues(typeof(TowerId)))
        {
            for (int i = 0; i < animationTableDefault[enumID].Count; i++)
            {
                float max = math.max(maxWidthDefault, maxHeightDefault);
                float scaleX = animationTableDefault[enumID][i].Scale.x / max;
                float scaleY = animationTableDefault[enumID][i].Scale.y / max;
                animationTableDefault[enumID][i] = new ShootVfxAnimationFrameData()
                {
                    UV = animationTableDefault[enumID][i].UV,
                    Scale = new float2(scaleX, scaleY),
                    PositionOffset = new float2(animationTableDefault[enumID][i].PositionOffset.x * scaleX, animationTableDefault[enumID][i].PositionOffset.y * scaleY),
                    ScaleModifier = GetMuzzleScale(enumID, isMuzzle)
                };
            }
            for (int i = 0; i < animationTableEnhunced[enumID].Count; i++)
            {
                float max = math.max(maxWidthEnhunced, maxHeightEnhunced);
                float scaleX = animationTableEnhunced[enumID][i].Scale.x / max;
                float scaleY = animationTableEnhunced[enumID][i].Scale.y / max;
                animationTableEnhunced[enumID][i] = new ShootVfxAnimationFrameData()
                {
                    UV = animationTableEnhunced[enumID][i].UV,
                    Scale = new float2(scaleX, scaleY),
                    PositionOffset = new float2(animationTableEnhunced[enumID][i].PositionOffset.x * scaleX, animationTableEnhunced[enumID][i].PositionOffset.y * scaleY),
                    ScaleModifier = GetMuzzleScale(enumID, isMuzzle)
                };
            }
        }
        
        float GetMuzzleScale(TowerId towerType, bool isMuzzle)
        {
            foreach (var line in VfxTableData)
            {
                if (line.TowerTypes == towerType)
                    return isMuzzle? line.Muzzle_S: line.Impact_S;
            }
            Debug.LogError($"Cant find {towerType} in VfxTableData");
            return 1;
        }
    }

    private static void SplitLineToData(string line, int sizeX, int sizeY,
        ref int maxHeightDefault, ref int maxWidthDefault, Dictionary<AllEnums.TowerId, List<ShootVfxAnimationFrameData>> animationTableDefault,//List<AnimationFrameData> animationTableRun,
        ref int maxHeightEnhunced, ref int maxWidthEnhunced, Dictionary<AllEnums.TowerId, List<ShootVfxAnimationFrameData>> animationTableEnhunced)
    {
        string[] splitted = line.Split(';');

        int xPos = int.Parse(splitted[1].Trim());
        int yPos = int.Parse(splitted[2].Trim());
        int width = int.Parse(splitted[3].Trim());
        int height = int.Parse(splitted[4].Trim());

        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

        float texScaleX = (float)width / sizeX;
        float texScaleY = (float)height / sizeY;
        float texOffsetX = (float)xPos / sizeX;
        float texOffsetY = (float)yPos / sizeY;

        float frameScaleY = (float)height;
        float frameScaleX = (float)width;

        float pivotX = float.Parse(splitted[5].Trim(), culture);
        float pivotY = float.Parse(splitted[6].Trim(), culture);

        float frameOffsetX = (.5f - pivotX);
        float frameOffsetY = (.5f - pivotY);

        float4 uv = new float4(texScaleX, texScaleY, texOffsetX, texOffsetY);
        float2 scale = new float2(frameScaleX, frameScaleY);
        float2 offset = new float2(frameOffsetX, frameOffsetY);

        var towerType = CheckTowerId<TowerId>(splitted[0]);

        if (towerType != 0)
        {
            if (!splitted[0].Contains("enhanced"))
            {
                maxHeightDefault = math.max(maxHeightDefault, height);
                maxWidthDefault = math.max(maxWidthDefault, width);

                animationTableDefault[towerType].Add(new ShootVfxAnimationFrameData() { UV = uv, Scale = scale, PositionOffset = offset });
            }
            else
            {
                maxHeightEnhunced = math.max(maxHeightEnhunced, height);
                maxWidthEnhunced = math.max(maxWidthEnhunced, width);

                animationTableEnhunced[towerType].Add(new ShootVfxAnimationFrameData() { UV = uv, Scale = scale, PositionOffset = offset });
            }

            //dirty mortar muzzle == rocket muzzle
            if (towerType == TowerId.Mortar)
            {
                if (!splitted[0].Contains("enhanced"))
                    animationTableDefault[TowerId.Rocket].Add(new ShootVfxAnimationFrameData() { UV = uv, Scale = scale, PositionOffset = offset });
                else
                    animationTableEnhunced[TowerId.Rocket].Add(new ShootVfxAnimationFrameData() { UV = uv, Scale = scale, PositionOffset = offset });
            }

            if (towerType == TowerId.Laser)
                animationTableEnhunced[TowerId.Laser].Add(new ShootVfxAnimationFrameData() { UV = uv, Scale = scale, PositionOffset = offset });
        }
        else
        {
            Debug.LogError("Cant parse TowerId => chack that file name contains TowerId str");
        }


        T CheckTowerId<T>(string splitter0) where T : struct, Enum
        {
            foreach (T enumID in Enum.GetValues(typeof(T)))
            {
                if (splitter0.Contains(enumID.ToString()))
                    return enumID;
            }
            Debug.LogError(splitter0);
            return default;
        }
    }

    //[Button, BoxGroup("dict serialize")]
    //public void SetDirtyScales(float size = 5)
    //{
    //    DirtyScalesByTower = new();
    //    foreach (AllEnums.TowerId item in Enum.GetValues(typeof(AllEnums.TowerId)))
    //        DirtyScalesByTower.Add(item, size);
    //    EditorUtility.SetDirty(this);
    //}
    //[Button, BoxGroup("dict serialize")]
    //public void SetDirtyTimeBetweenFrames(float time = 0.1f)
    //{
    //    MuzzleTimeBetweenFrames.Clear();
    //    foreach (AllEnums.TowerId item in Enum.GetValues(typeof(AllEnums.TowerId)))
    //        MuzzleTimeBetweenFrames.Add(time);
    //    EditorUtility.SetDirty(this);
    //}

    [Button, BoxGroup("VfxTableData managed")]
    public void SetDirtyScales(float size = 5, float time = 0.05f)
    {
        VfxTableData = new();
        foreach (AllEnums.TowerId item in Enum.GetValues(typeof(AllEnums.TowerId)))
            VfxTableData.Add(new VfxAnimationParams() { TowerTypes = item, Muzzle_S = size, Impact_S = size, Muzzle_T = time, Impact_T = time });
        EditorUtility.SetDirty(this);
    }

#endif
}