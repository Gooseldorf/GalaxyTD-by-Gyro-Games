#if UNITY_EDITOR

using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DropZoneIdHelper : SerializedScriptableObject
{
    [SerializeField] private List<Mission> missions;

    [Button]
    public void UpdatePrototypesAndUpgradeCosts()
    {
        foreach (var mission in missions)
        {
            EditorUtility.SetDirty(mission);

            for(int i = 0; i < mission.DropZones.Length; i++)
            {
                mission.DropZones[i].Id = 130000 + i;
            }
        }
        AssetDatabase.SaveAssets();
    }
}
#endif