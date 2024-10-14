using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class PlayerPref
    {
        [MenuItem("PlayerData/Clear Saves")]
        private static void Clear()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
        
        [MenuItem("PlayerData/Set default data")]
        private static void SetDefault()
        {
            if(!DataManager.Instance.GameData.IsDefaultGameData) 
                DataManager.Instance.GameData.SetDefaultData();
        }
    }
}