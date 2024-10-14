using Data.Managers;
using I2.Loc;
using Managers;
using System;
using UnityEngine;

namespace UI
{
    public class MainMenuStarter : MonoBehaviour
    {
        public static MainMenuStarter Instance;

        private void Awake()
        {
            MusicManager.Clear();

            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            // Application.targetFrameRate = -1;

            Instance = this;

#if !UNITY_EDITOR
            if (!PlayerPrefs.HasKey(PrefKeys.FirstStart))
                SetDefaultDataOnFirstStart();
            DataManager.Instance.GameData.LoadFromDisk();
#endif
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
#if !UNITY_STANDALONE
            try
            {
                IAPManager.InitServices();
                AdsManager.InitServices();
            }
            catch (Exception e)
            {
                Debug.LogError($"e {e}");
            }
#endif
        }
        
#if UNITY_EDITOR
        private void OnDestroy()
        {
            UnityEditor.EditorUtility.SetDirty(DataManager.Instance.GameData);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(DataManager.Instance.GameData);
        }

#endif

        private static void SetDefaultDataOnFirstStart()
        {
            GameData.SaveDefaultDataToDisk();
            PlayerPrefs.SetInt(PrefKeys.FirstStart, 1);
            PlayerPrefs.SetInt(PrefKeys.SkipOldDialogs, 1);
            if (LocalizationManager.GetAllLanguages().Contains(Application.systemLanguage.ToString()))
                LocalizationManager.CurrentLanguage = "English" ;
            else
                LocalizationManager.CurrentLanguage = "English";
        }
    }
}