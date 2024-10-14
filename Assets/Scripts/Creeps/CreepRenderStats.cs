using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Mathematics;
using ECSTest.Components;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewCreepRenderStats", menuName = "ScriptableObjects/CreepRenderStats")]
public class CreepRenderStats : ScriptableObject
{
    [BoxGroup("Render")]
    public Material CreepMaterial;
    [BoxGroup("Render")]
    public int SortingOrder;
    [BoxGroup("Render")]
    public float Scale;
    [BoxGroup("Animation"), ShowInInspector]
    public int RunFrames => AnimationTableRun.Count;
    [BoxGroup("Animation"), ShowInInspector]
    public int DieFrames => AnimationTableDie.Count;
    [BoxGroup("Animation")]
    public float TimeBetweenRunFrames;
    [BoxGroup("Animation")]
    public float TimeBetweenDieFrames;
    [BoxGroup("Animation"), ShowInInspector]
    public Color DeathColor = Color.white;
    [BoxGroup("HP Bar")]
    public float HpBarOffset;
    [BoxGroup("HP Bar")]
    public float HpBarWidth;
    [BoxGroup("AnimationTable")]
    public List<AnimationFrameData> AnimationTableRun = new();
    [BoxGroup("AnimationTable")]
    public List<AnimationFrameData> AnimationTableDie = new();

#if UNITY_EDITOR

    [Button, BoxGroup("AnimationTable")]
    private void ParseSpriteSheetData(UnityEngine.Object file, bool addExtraDieFrame = true)
    {
        ParseSpriteSheetData(file, out AnimationTableRun, out AnimationTableDie, addExtraDieFrame);
        EditorUtility.SetDirty(this);
    }

    private static void ParseSpriteSheetData(UnityEngine.Object file, out List<AnimationFrameData> animationTableRun, out List<AnimationFrameData> animationTableDie, bool addExtraDieFrame)
    {
        animationTableRun = new();
        animationTableDie = new();

        string parseFilePath = AssetDatabase.GetAssetPath(file);

        int sizeX = 0, sizeY = 0;
        bool dataStarted = false;
        string line;
        int maxHeightRun = 0;
        int maxWidthRun = 0;
        int maxHeightDie = 0;
        int maxWidthDie = 0;

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
                //Debug.Log("Texture size: " + sizeX + "x" + sizeY);
            }

            if (dataStarted)
            {
                SplitLineToData(line, sizeX, sizeY,
                    ref maxHeightRun, ref maxWidthRun, animationTableRun,
                    ref maxHeightDie, ref maxWidthDie, animationTableDie);
            }

            if (line == "")
            {
                //Debug.Log("found start of data");
                dataStarted = true;
            }
            line = sr.ReadLine();
        }
        sr.Close();

        for (int i = 0; i < animationTableRun.Count; i++)
        {
            float max = math.max(maxWidthRun, maxHeightRun);
            float scaleX = animationTableRun[i].Scale.x / max;
            float scaleY = animationTableRun[i].Scale.y / max;
            animationTableRun[i] = new AnimationFrameData()
            {
                UV = animationTableRun[i].UV,
                Scale = new float2(scaleX, scaleY),
                PositionOffset = new float2(animationTableRun[i].PositionOffset.x * scaleX, animationTableRun[i].PositionOffset.y * scaleY)
            };
        }

        for (int i = 0; i < animationTableDie.Count; i++)
        {
            float max = math.max(maxWidthDie, maxHeightDie);
            float scaleX = animationTableDie[i].Scale.x / max;
            float scaleY = animationTableDie[i].Scale.y / max;
            animationTableDie[i] = new AnimationFrameData()
            {
                UV = animationTableDie[i].UV,
                Scale = new float2(scaleX, scaleY),
                PositionOffset = new float2(animationTableDie[i].PositionOffset.x * scaleX, animationTableDie[i].PositionOffset.y * scaleY)
            };
        }

        if (addExtraDieFrame)
            animationTableDie.Add(new AnimationFrameData() { UV = float4.zero, Scale = new float2(1, 1), PositionOffset = float2.zero });
    }

    private static void SplitLineToData(string line, int sizeX, int sizeY,
        ref int maxHeightRun, ref int maxWidthRun, List<AnimationFrameData> animationTableRun,
        ref int maxHeightDie, ref int maxWidthDie, List<AnimationFrameData> animationTableDie)
    {
        string[] splitted = line.Split(';');

        //AnimationFrameId frameId = GetSpriteName(splitted[0].Trim());
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

        //AnimationTableKeys.Add(frameId);
        if (splitted[0].Contains("Death"))
        {
            maxHeightDie = math.max(maxHeightDie, height);
            maxWidthDie = math.max(maxWidthDie, width);

            animationTableDie.Add(new AnimationFrameData() { UV = uv, Scale = scale, PositionOffset = offset });
        }
        else
        {
            maxHeightRun = math.max(maxHeightRun, height);
            maxWidthRun = math.max(maxWidthRun, width);

            animationTableRun.Add(new AnimationFrameData() { UV = uv, Scale = scale, PositionOffset = offset });
        }
    }

    //private static AnimationFrameId GetSpriteName(string line)
    //{
    //    int indexOf_ = line.LastIndexOf('_');
    //    byte frameNumber = (byte)int.Parse(line.Substring(indexOf_ + 1, line.Length - indexOf_ - 1));
    //    //AllEnums.AnimationState state = GetDirection(line, line.Contains("Death"));
    //    AllEnums.AnimationState state = line.Contains("Death") ? AllEnums.AnimationState.Death : AllEnums.AnimationState.Run;
    //    //Debug.Log(result);
    //    return new AnimationFrameId() { FrameNumber = frameNumber, State = state };
    //}
#endif
}
