using ECSTest.Components;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.IO;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using FileMode = System.IO.FileMode;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;
using UnityEngine.Rendering;
using System.Collections;
using System;

public class ReplayManager : ScriptableObjSingleton<ReplayManager>
{

    [SerializeField]
    private bool autoFocusOnSave;

    [ShowInInspector]
    public bool RecordReplay { get; set; } = true;
    [SerializeField]
    private int timeLapseDelay = 30;
    [SerializeField]
    private float screenshotDelay;
    public int MinTimeForAutosave = 30;
    public int MinCommandsToSave = 3;


    private Replay currentReplay;
    private ReplayHook currentHook;
    private ReplayHook replayHook
    {
        get
        {
            if (currentHook == null)
            {
                GameObject go = new GameObject();
                currentHook = go.AddComponent<ReplayHook>();
            }
            return currentHook;
        }
    }

    private const string replayFolder = "Assets/Replays";
    private const string autoReplayFolder = "Assets/Replays/AutoReplays";
    private const string replayInProgressPath = "Assets/Replays/ReplayInProgress.asset";


    #region LogMethods
    internal void LogMissionInit(Mission mission, Tower[] towers)
    {
        if (!RecordReplay) return;

#if UNITY_EDITOR
        if (AssetDatabase.AssetPathExists(replayInProgressPath))
        {
            currentReplay = AssetDatabase.LoadAssetAtPath<Replay>(replayInProgressPath);
            if (IsValidForAutoSave(currentReplay))
            {
                string replayName = AutoSaveReplay();
                Debug.LogError($"Saving Leftover Replay - possible crash: {replayName}");
            }
            currentReplay.Clear();
        }
        else
        {
            currentReplay = CreateInstance<Replay>();
            AssetDatabase.CreateAsset(currentReplay, replayInProgressPath);
            AssetDatabase.SaveAssets();
        }
#else
        currentReplay = CreateInstance<Replay>();
#endif

        currentReplay.Name = "Mission_" + mission.MissionIndex;
        currentReplay.MissionIndex = mission.MissionIndex;
        currentReplay.TowerPrototypes = towers;
        currentReplay.TimeStarted = Time.time;
        replayHook.StartCoroutine(Timelapse());
        replayHook.StartCoroutine(CaptureScreenshot(screenshotDelay));
        //currentReplay.LogCommand(new Replay.Command(0, Replay.CommandType.InitMission, 0));
        SaveAsset();
    }

    internal void LogBuildTower(Tower towerPrototype, Entity dropZone)
    {
        if (!RecordReplay) return;
        Identifiable identifiable = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Identifiable>(dropZone);
        currentReplay.LogCommand(new Replay.BuildTowerCommand(currentReplay.RelativeTime, identifiable.Id, towerPrototype.TowerId));
        replayHook.StartCoroutine(CaptureScreenshot(screenshotDelay));
        SaveAsset();
    }

    internal void LogSellTower(Entity tower)
    {
        if (!RecordReplay) return;
        Identifiable identifiable = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Identifiable>(tower);
        currentReplay.LogCommand(new Replay.Command(currentReplay.RelativeTime, Replay.CommandType.SellTower, identifiable.Id));
        replayHook.StartCoroutine(CaptureScreenshot(screenshotDelay));
        SaveAsset();

    }

    internal void LogUpgradeTower(Entity tower)
    {
        Identifiable identifiable = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Identifiable>(tower);
        currentReplay.LogCommand(new Replay.Command(currentReplay.RelativeTime, Replay.CommandType.UpgradeTower, identifiable.Id));
        replayHook.StartCoroutine(CaptureScreenshot(screenshotDelay));
        SaveAsset();
    }
    internal void LogChangeFiringMode(Entity tower)
    {
        if (!RecordReplay) return;
        Identifiable identifiable = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Identifiable>(tower);
        currentReplay.LogCommand(new Replay.Command(currentReplay.RelativeTime, Replay.CommandType.ChangeFiringMode, identifiable.Id));
        replayHook.StartCoroutine(CaptureScreenshot(screenshotDelay));
        SaveAsset();
    }

    internal void LogPowerCellClicked(Entity powerCell)
    {
        if (!RecordReplay) return;
        Identifiable identifiable = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Identifiable>(powerCell);
        currentReplay.LogCommand(new Replay.Command(currentReplay.RelativeTime, Replay.CommandType.PowerCellClicked, identifiable.Id));
        replayHook.StartCoroutine(CaptureScreenshot(screenshotDelay));
        SaveAsset();
    }

    internal void LogStartClick()
    {
        if (!RecordReplay) return;
        currentReplay.LogCommand(new Replay.Command(currentReplay.RelativeTime, Replay.CommandType.StartClicked, 0));
        SaveAsset();
    }

    internal void LogManualReload(Entity tower)
    {
        if (!RecordReplay) return;
        Identifiable identifiable = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Identifiable>(tower);
        currentReplay.LogCommand(new Replay.Command(currentReplay.RelativeTime, Replay.CommandType.ManualReload, identifiable.Id));
        SaveAsset();
    }

    #endregion
    private bool IsValidForAutoSave(Replay replay)
    {
        if (replay == null) return false;
        return RecordReplay && replay.TimePlayed >= MinTimeForAutosave && replay.CommandsPlayed >= MinCommandsToSave;
    }
#if UNITY_EDITOR
    internal void OnPlayStateChanged(PlayModeStateChange state)
    {
        if (RecordReplay && state == PlayModeStateChange.ExitingPlayMode)
        {

            // Record To AutoSave
            if (IsValidForAutoSave(currentReplay))
                AutoSaveReplay();
            if (currentReplay != null)
                currentReplay.Clear();

        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            ReplayHook[] hooks = FindObjectsByType<ReplayHook>(FindObjectsSortMode.None);
            for (int i = 0; i < hooks.Length; i++)
                DestroyImmediate(hooks[i].gameObject);
        }
    }
#endif

    #region Save Methods
    private void SaveAsset()
    {
#if UNITY_EDITOR
        AssetDatabase.SaveAssetIfDirty(currentReplay);
#endif
    }

    [Button]
    public string AutoSaveReplay()
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:Replay", new[] { autoReplayFolder });
        int maxCount = -1;
        foreach (string guid in guids)
        {
            string filePath = AssetDatabase.GUIDToAssetPath(guid);
            string filename = filePath.Split('/')[^1];
            string num = filename.Split("_")[0];
            maxCount = Mathf.Max(maxCount, int.TryParse(num, out int result) ? result : 0);
        }

        string path = $"{autoReplayFolder}/{maxCount + 1}_{currentReplay.Name}.asset";
        SaveAsScriptableObject(currentReplay, path);
#else
        string path = "";
#endif
        return path;

    }


#if UNITY_EDITOR
    [Button]
    public void SaveAsScriptableObject()
    {
        if (!AssetDatabase.AssetPathExists(replayFolder))
            AssetDatabase.CreateFolder("Assets", "Replays");
        string savePath = $"{replayFolder}/{currentReplay.Name}.asset";
        if (AssetDatabase.AssetPathExists(savePath))
        {
            int num = 0;
            do
            {
                savePath = $"{replayFolder}/{currentReplay.Name}({num}).asset";
                num++;
            }
            while (AssetDatabase.AssetPathExists(savePath));
        }
        SaveAsScriptableObject(currentReplay, savePath);
    }


    private void SaveAsScriptableObject(Replay replay, string name)
    {
        Replay clone = replay.Clone();
        AssetDatabase.CreateAsset(clone, name);
        AssetDatabase.SaveAssets();

        if (autoFocusOnSave)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = clone;
        }
    }
#endif


    [Button]
    public void SaveToDisk()
    {
        try
        {
            string savePath = Path.Combine(Application.persistentDataPath, currentReplay.Name);

            if (File.Exists(savePath))
            {
                int num = 0;
                do
                {
                    savePath = Path.Combine(Application.persistentDataPath, currentReplay.Name + $"({num})");
                    num++;
                }
                while (File.Exists(savePath));
            }


            using (FileStream fs = new(savePath, FileMode.Create))
            {
                SerializationContext context = new()
                {
                    StringReferenceResolver = StringReferenceResolver.Instance,
                };
                IDataWriter writer = SerializationUtility.CreateWriter(fs, context, DataFormat.Binary);
                SerializationUtility.SerializeValue(currentReplay, writer);
            }
            Debug.Log("Save Successful: " + currentReplay.Name);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed To Save");
        }
    }

    [Button]
    public void LoadFromDisk(string name)
    {
        currentReplay = null;
        try
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Load");
#endif
            string savePath = Path.Combine(Application.persistentDataPath, name);
            //TODO: rotation of saves
            using (FileStream fs = new FileStream(savePath, FileMode.Open))
            {
                DeserializationContext context = new()
                {
                    StringReferenceResolver = StringReferenceResolver.Instance,
                };

                IDataReader reader = SerializationUtility.CreateReader(fs, context, DataFormat.Binary);
                currentReplay = SerializationUtility.DeserializeValue<Replay>(reader);
            }

            Debug.Log("Load Successful");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed To Load");
        }
    }

    #endregion

    #region ScreenShots
    //RenderTexture texture;
    private IEnumerator Timelapse()
    {

#if UNITY_EDITOR
        while (true)
        {
            Camera.main.targetTexture = new RenderTexture(Screen.width / 4, Screen.height / 4, 32, RenderTextureFormat.ARGB32);
            //texture = RenderTexture.active;
            RenderTexture.active = Camera.main.targetTexture;
            RenderPipelineManager.endCameraRendering += TakeTimeLapseScreenshot;
            yield return new WaitForSeconds(timeLapseDelay);
        }
#endif
        yield return null;
    }

    private IEnumerator CaptureScreenshot(float delay)
    {
#if UNITY_EDITOR
        yield return new WaitForSeconds(delay);
        Camera.main.targetTexture = new RenderTexture(Screen.width / 2, Screen.height / 2, 32, RenderTextureFormat.ARGB32);
        //texture = RenderTexture.active;
        RenderTexture.active = Camera.main.targetTexture;
        RenderPipelineManager.endCameraRendering += TakeScreenshot;
#endif

        yield return null;

    }

    private void TakeTimeLapseScreenshot(ScriptableRenderContext arg1, Camera arg2)
    {
        if (RenderTexture.active != null)
        {
            Texture2D screenShot = new(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);
            screenShot.Apply();
            currentReplay.AddTimeLapse(screenShot);
            SaveAsset();
        }
        else
        {
            Debug.LogWarning("RenderTexture.active is null");
        }

        Camera.main.targetTexture = null;
        RenderTexture.active = null;
        RenderPipelineManager.endCameraRendering -= TakeTimeLapseScreenshot;
    }

    private void TakeScreenshot(ScriptableRenderContext arg1, Camera arg2)
    {
        if (RenderTexture.active != null)
        {
            Texture2D screenShot = new(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0, false);
            screenShot.Apply();
            currentReplay.SaveScreenshot(screenShot);
            SaveAsset();
        }

        Camera.main.targetTexture = null;
        RenderTexture.active = null;
        RenderPipelineManager.endCameraRendering -= TakeScreenshot;
    }

    #endregion

}


#if UNITY_EDITOR

[InitializeOnLoad]
public static class ReplayManagerEditorHook
{
    static ReplayManagerEditorHook()
    {
        EditorApplication.playModeStateChanged += ReplayManager.Instance.OnPlayStateChanged;
    }
}
#endif