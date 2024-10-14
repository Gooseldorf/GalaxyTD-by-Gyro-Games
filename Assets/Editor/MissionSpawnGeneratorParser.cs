#if UNITY_EDITOR

using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Systems;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class MissionSpawnGeneratorParser : SerializedScriptableObject
{
    [SerializeField] private List<Mission> missions;
    [SerializeField] private MissionSpawnGenerator missionSpawnGenerator;

    [Button]
    private void FindMissionSpawnGenerator()
    {
        missionSpawnGenerator =
            (MissionSpawnGenerator)EditorWindow.GetWindow(typeof(MissionSpawnGenerator));
    }

    public bool IsOnlyHpModifiers;
    public bool IncludeMissionsWithMoreThenOneSpawnGroups = false;

    private string result;

    [InfoBox("Before parsing, copy MissionSpawnGenerator table to clipboard!!!")]
    [Button]
    public void SetDataToMissions()
    {
        Debug.LogError("Generation works different now. This requires fixes to work");
        result = GUIUtility.systemCopyBuffer;
        if (result == String.Empty)
        {
            Debug.LogError("Clipboard is empty!");
            return;
        }

        //result = result.Replace("\t\t", "\t-1\t");
        result = result.Replace(',', '.');

        result = Regex.Replace(result, @"(\d+)%", m => (int.Parse(m.Groups[1].Value) / 100.0).ToString());

        string[] allLines = result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        List<string[]> splitData = new();
        
        foreach (string line in allLines)
        {
            string[] split = line.Split('\t');
            if (split.Length > 1)
                splitData.Add(split);
        }

        foreach (var mission in missions)
        {
            EditorUtility.SetDirty(mission);
            if (mission.IgnoreByMissionSpawnGeneratorParser)
            {
                Debug.LogWarning($"Mission {mission.MissionIndex + 1} has marked as ignored");
                continue;
            }
            int missionNumer = mission.MissionIndex + 1;

            UpdateMissionHpModifier(mission, missionNumer, splitData);
            if (IsOnlyHpModifiers)
                continue;

            if (mission.SpawnData.Length != 1 && !IncludeMissionsWithMoreThenOneSpawnGroups)
            {
                Debug.LogWarning($"Mission {missionNumer} has {mission.SpawnData.Length} spawn groups! Should manual prepare");
                continue;
            }

            UpdateMissionSpawnGroupsByWaveGenerator(mission, missionNumer, splitData);
        }
        AssetDatabase.SaveAssets();
    }

    [InfoBox("When prepared, CountWave from table will be inserted into all SpawnGroups!!!")]
    [Button]
    public void DirtyPrepareMissionWaveCount()
    {
        Debug.LogError("Generation works different now. This requires fixes to work");
        result = GUIUtility.systemCopyBuffer;
        if (result == String.Empty)
        {
            Debug.LogError("Clipboard is empty!");
            return;
        }

        //result = result.Replace("\t\t", "\t-1\t");
        result = result.Replace(',', '.');

        result = Regex.Replace(result, @"(\d+)%", m => (int.Parse(m.Groups[1].Value) / 100.0).ToString());

        string[] allLines = result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        List<string[]> splitData = new();

        foreach (string line in allLines)
        {
            string[] split = line.Split('\t');
            if (split.Length > 1)
                splitData.Add(split);
        }


        foreach (var mission in missions)
        {
            if (mission.IgnoreByMissionSpawnGeneratorParser)
            {
                Debug.LogWarning($"Mission {mission.MissionIndex + 1} has marked as ignored");
                continue;
            }

            if (mission.SpawnData.Length != 1)
                Debug.LogWarning($"Mission {mission.MissionIndex + 1} has {mission.SpawnData.Length} spawn groups! Should manual prepare");

            missionSpawnGenerator.Mission = mission;

            missionSpawnGenerator.CountWave = (int)float.Parse(splitData[mission.MissionIndex + 1][4], CultureInfo.InvariantCulture);
            missionSpawnGenerator.DirtyCreateEmptyWaves();
        }
    }

    private void UpdateMissionHpModifier(Mission mission, int missionNumer, List<string[]> splitData)
    {
        EditorUtility.SetDirty(mission);

        mission.HpModifier = float.Parse(splitData[missionNumer][1], CultureInfo.InvariantCulture);
        AssetDatabase.SaveAssets();
    }

    private void UpdateMissionSpawnGroupsByWaveGenerator(Mission mission, int missionNumer, List<string[]> splitData)
    {
        EditorUtility.SetDirty(mission);
        missionSpawnGenerator.Mission = mission;
        Debug.Log(missionNumer);

        missionSpawnGenerator.DistanceToFirstTower = (int)float.Parse(splitData[missionNumer][2], CultureInfo.InvariantCulture);
        missionSpawnGenerator.DistanceToLastTower = (int)float.Parse(splitData[missionNumer][3], CultureInfo.InvariantCulture);
        missionSpawnGenerator.CountWave = (int)float.Parse(splitData[missionNumer][4], CultureInfo.InvariantCulture);
        missionSpawnGenerator.FirstWaveCash = (int)float.Parse(splitData[missionNumer][6], CultureInfo.InvariantCulture);
        missionSpawnGenerator.FillCashes();

        missionSpawnGenerator.CountUnarmoredUnits = new int2(
            (int)float.Parse(splitData[missionNumer][7], CultureInfo.InvariantCulture),
            (int)float.Parse(splitData[missionNumer][8], CultureInfo.InvariantCulture));
        missionSpawnGenerator.CountLightUnits = new int2(
            (int)float.Parse(splitData[missionNumer][9], CultureInfo.InvariantCulture),
            (int)float.Parse(splitData[missionNumer][10], CultureInfo.InvariantCulture));
        missionSpawnGenerator.CountHeavyUnits = new int2(
            (int)float.Parse(splitData[missionNumer][11], CultureInfo.InvariantCulture),
            (int)float.Parse(splitData[missionNumer][12], CultureInfo.InvariantCulture));

        bool[] armorTypes = new bool[3]
        {
            float.Parse(splitData[missionNumer][13], CultureInfo.InvariantCulture) == 1,
            float.Parse(splitData[missionNumer][14], CultureInfo.InvariantCulture) == 1,
            float.Parse(splitData[missionNumer][15], CultureInfo.InvariantCulture) == 1
        };
        missionSpawnGenerator.ArmorTypes = new AllEnums.ArmorType();// fix parse flags !!

        if (armorTypes[0])//
                          //fix parse flags !!
            missionSpawnGenerator.ArmorTypes |= AllEnums.ArmorType.Unarmored;
        if (armorTypes[1])
            missionSpawnGenerator.ArmorTypes |= AllEnums.ArmorType.Light;
        if (armorTypes[2])
            missionSpawnGenerator.ArmorTypes |= AllEnums.ArmorType.Heavy;

        bool[] fleshTypes = new bool[3]
        {
            float.Parse(splitData[missionNumer][16], CultureInfo.InvariantCulture) == 1,
            float.Parse(splitData[missionNumer][17], CultureInfo.InvariantCulture) == 1,
            float.Parse(splitData[missionNumer][18], CultureInfo.InvariantCulture) == 1
        };
        missionSpawnGenerator.PermittedFleshTypes = new AllEnums.FleshType();// fix parse flags !!

        if (fleshTypes[0])//
                          //fix parse flags !!
            missionSpawnGenerator.PermittedFleshTypes |= AllEnums.FleshType.Bio;
        if (fleshTypes[1])
            missionSpawnGenerator.PermittedFleshTypes |= AllEnums.FleshType.Mech;
        if (fleshTypes[2])
            missionSpawnGenerator.PermittedFleshTypes |= AllEnums.FleshType.Energy;

        missionSpawnGenerator.GenerateSource();
        missionSpawnGenerator.Generate();
        missionSpawnGenerator.PrepareDataForMission();
        missionSpawnGenerator.SetDataToMission();
        AssetDatabase.SaveAssets();
    }
}
#endif
