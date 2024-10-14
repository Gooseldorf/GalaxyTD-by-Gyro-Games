using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MissionList : ScriptableObject
{
    [SerializeField] private List<Mission> missions;
    [SerializeField] private List<Mission> missionsHard;
    public IReadOnlyList<Mission> Missions => missions;
    public IReadOnlyList<Mission> MissionsHard => missionsHard;

    public Mission GetMissionByIndex(int index)
    {
        return missions.Find(x => x.MissionIndex == index);
    }

    public bool IsHardMission(Mission mission)
    {
        return !missions.Contains(mission);
    }

#if UNITY_EDITOR

    [Button]
    public void SetIndex()
    {
        for (int i = 0; i < missions.Count; i++)
        {
            missions[i].MissionIndex = i;
            EditorUtility.SetDirty(missions[i]);
        }
        
        for (int i = 0; i < missionsHard.Count; i++)
        {
            missionsHard[i].MissionIndex = i;
            EditorUtility.SetDirty(missionsHard[i]);
        }
       
        AssetDatabase.SaveAssets();
    }

    [SerializeField] private List<Mission> incorrectMissions;
    [Button]
    private void CheckFloorOffsets()
    {
        incorrectMissions.Clear();
        foreach (Mission mission in missions)
        {
            if (mission.LevelMatrix.FloorOrigin == Vector3Int.zero)
            {
                incorrectMissions.Add(mission);
            }
        }

        foreach (Mission mission in missionsHard)
        {
            if (mission.LevelMatrix.FloorOrigin == Vector3Int.zero)
            {
                incorrectMissions.Add(mission);
            }
        }
    }
#endif
}
