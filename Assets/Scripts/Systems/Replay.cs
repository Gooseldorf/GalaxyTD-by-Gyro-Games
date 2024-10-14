using ECSTest.Components;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class Replay : SerializedScriptableObject
{

    [HideInInspector]
    public string Name;

    [HideInInspector]
    public int MissionIndex;
    [OdinSerialize, HideInInspector]
    public Tower[] TowerPrototypes;
    [HideInInspector]
    public float TimeStarted;
    [ShowInInspector, LabelText("Replay Length (actionswise)")]
    public float TimePlayed
    {
        get
        {
            if (commands != null && commands.Count > 0)
                return commands.Peek().TimeStamp;
            else return 0;
        }
    }

    public float RelativeTime => Time.time - TimeStarted;
    public int CommandsPlayed => commands.Count;

    public class Command
    {
        public Command(float timeStamp, CommandType commandType, int id)
        {
            TimeStamp = timeStamp;
            CommandType = commandType;
            Id = id;
        }
        public readonly float TimeStamp;
        public readonly CommandType CommandType;
        public readonly int Id;
    }

    public class BuildTowerCommand : Command
    {
        public AllEnums.TowerId TowerId;

        public BuildTowerCommand(float timeStamp, int id, AllEnums.TowerId towerId) : base(timeStamp, CommandType.BuildTower, id)
        {
            TowerId = towerId;
        }
    }
    [SerializeField]
    private Queue<Command> commands = new Queue<Command>();

    [NonSerialized]
    private Queue<Command> runtimeCommands;

    public void LogCommand(Command command) => commands.Enqueue(command);


    [Button]
    public void Play()
    {
        ReplayManager.Instance.RecordReplay = false;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/ECSScene.unity");
        }
#endif
        GameObject go = new GameObject("ReplayHook");
        ReplayHook replayHook = go.AddComponent<ReplayHook>();
        replayHook.Init(this, !Application.isPlaying);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.EnterPlaymode();
#endif
    }

    public void Play(ReplayHook replayHook)
    {
        replayHook.StartCoroutine(Playback(Time.time, replayHook.gameObject));
    }

    private IEnumerator Playback(float playbackStartTime, GameObject go)
    {
        runtimeCommands = new Queue<Command>(commands);

        while (runtimeCommands.Count > 0)
        {
            while (runtimeCommands.TryPeek(out Command command) && command.TimeStamp < (Time.time - playbackStartTime))
            {
                PlayCommand(command);
                runtimeCommands.Dequeue();
            }
            yield return null;
        }
        Destroy(go, .5f);
    }

    public void InitMission()
    {
        Mission mission = DataManager.Instance.Get<MissionList>().Missions.First(x => x.MissionIndex == MissionIndex);
        if (mission == null)
        {
            Debug.LogError("Mission List does not contain mission with: " + MissionIndex);
        }
        GameServices.Instance.InitMission(mission, TowerPrototypes);
    }

    private void PlayCommand(Command command)
    {
        switch (command.CommandType)
        {
            // --------> moved to InitMission()
            //
            //case CommandType.InitMission:
            //    Mission mission = DataManager.Instance.Get<MissionList>().Missions.First(x => x.MissionIndex == MissionIndex);
            //    if (mission == null)
            //    {
            //        Debug.LogError("Mission List does not contain mission with: " + MissionIndex);
            //    }
            //    GameServices.Instance.InitMission(mission, TowerPrototypes);
            //    break;
            case CommandType.BuildTower:
                BuildTowerCommand buildTowerCommand = (BuildTowerCommand)command;
                Tower tower = Array.Find(TowerPrototypes, x => x.TowerId == buildTowerCommand.TowerId);
                GameServices.Instance.BuildTower(tower, FindEntityById<DropZoneComponent>(command.Id));
                break;
            case CommandType.UpgradeTower:
                GameServices.Instance.UpgradeTower(FindEntityById<AttackerComponent>(command.Id));
                break;
            case CommandType.SellTower:
                GameServices.Instance.SellTower(FindEntityById<AttackerComponent>(command.Id));
                break;
            case CommandType.ChangeFiringMode:
                GameServices.Instance.ChangeFiringModel(FindEntityById<AttackerComponent>(command.Id));
                break;
            case CommandType.PowerCellClicked:
                GameServices.Instance.PowerCellClicked(FindEntityById<PowerCellComponent>(command.Id));
                break;
            case CommandType.StartClicked:
                GameServices.Instance.SkipFirstWaveOffset();
                break;
            case CommandType.ManualReload:
                GameServices.Instance.ManualReload(FindEntityById<PowerCellComponent>(command.Id));
                break;
        }
    }

    private static Entity FindEntityById<T>(int id)
    {
        var query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(Identifiable), typeof(T));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        NativeArray<Identifiable> identifebles = query.ToComponentDataArray<Identifiable>(Allocator.Temp);
        query.Dispose();
        for (int i = 0; i < identifebles.Length; i++)
        {
            if (identifebles[i].Id == id)
            {
                Entity entity = entities[i];
                entities.Dispose();
                identifebles.Dispose();
                return entity;
            }
        }
        throw new ReplayDesyncException("So such Entity");
    }

    internal void Clear()
    {
        commands?.Clear();
        timelapseBytes?.Clear();
        timelapseSizes?.Clear();

        screenShot = new Texture2D(10, 10);
        screenshotBytes = null;

    }
    public enum CommandType
    {
        InitMission = 0,
        BuildTower = 1,
        UpgradeTower = 2,
        SellTower = 3,
        ChangeFiringMode = 4,
        PowerCellClicked = 5,
        StartClicked = 6,
        ManualReload = 7
    }

    #region Screenshots

    [Title("@Name", bold: true, HorizontalLine = true, TitleAlignment = TitleAlignments.Centered)]
    [HideLabel]
    [PropertyOrder(-1), PreviewField(400, Alignment = ObjectFieldAlignment.Center, PreviewGetter = "DrawScreenShot"), NonSerialized, ShowInInspector]
    private Texture2D screenShot;

    [SerializeField, HideInInspector]
    private byte[] screenshotBytes;
    [SerializeField, Sirenix.OdinInspector.ReadOnly]
    private int2 screenshotSize;

    [HideLabel]
    [PropertyOrder(100)]
    [PreviewField(200, Alignment = ObjectFieldAlignment.Center, PreviewGetter = "DrawTimeLapse"), NonSerialized, ShowInInspector]
    private Texture2D timeLapseTexture;
    [SerializeField, HideInInspector]
    private List<byte[]> timelapseBytes = new List<byte[]>();
    [SerializeField, HideInInspector]
    private List<int2> timelapseSizes = new List<int2>();

    [PropertyOrder(101)]
    [ProgressBar(0, "@timelapseSizes.Count - 1", Segmented = true), ShowInInspector, HideLabel]
    private int timelapseIndex;

    private Texture2D DrawTimeLapse()
    {
        if (timelapseBytes != null && timelapseBytes.Count > timelapseIndex)
        {
            timeLapseTexture = new Texture2D(timelapseSizes[timelapseIndex].x, timelapseSizes[timelapseIndex].y, TextureFormat.RGB24, false);

            timeLapseTexture.LoadImage(timelapseBytes[timelapseIndex]);
            timeLapseTexture.Apply();

            return timeLapseTexture;
        }
        else
        {
            return new Texture2D(10, 10);
        }
    }

    private Texture2D DrawScreenShot()
    {
        if (screenshotBytes != null && screenshotBytes.Length > 0)
        {
            screenShot = new Texture2D(screenshotSize.x, screenshotSize.y, TextureFormat.RGB24, false);
            screenShot.LoadImage(screenshotBytes);
            screenShot.Apply();
            return screenShot;
        }
        return new Texture2D(10, 10);
    }

    public void SaveScreenshot(Texture2D screenshot)
    {
        this.screenShot = screenshot;
        screenshotBytes = screenshot.EncodeToJPG(20);
        screenshotSize.x = screenshot.width;
        screenshotSize.y = screenshot.height;
    }

    internal void AddTimeLapse(Texture2D timeLapseScreenshot)
    {
        byte[] bytes = timeLapseScreenshot.EncodeToJPG(20);
        timelapseBytes.Add(bytes);
        timelapseSizes.Add(new int2(timeLapseScreenshot.width, timeLapseScreenshot.height));
    }


    #endregion


    internal Replay Clone()
    {
        Replay result = CreateInstance<Replay>();
        result.commands = new Queue<Command>(commands);
        result.TimeStarted = TimeStarted;
        result.Name = Name;
        result.MissionIndex = MissionIndex;
        result.TowerPrototypes = TowerPrototypes;
        result.screenshotBytes = screenshotBytes;
        result.screenshotSize = screenshotSize;
        result.timelapseSizes = new List<int2>(timelapseSizes);
        result.timelapseBytes = new List<byte[]>(timelapseBytes);
        return result;
    }

    [Serializable]
    private class ReplayDesyncException : Exception
    {
        public ReplayDesyncException()
        {
        }

        public ReplayDesyncException(string message) : base(message)
        {
        }

        public ReplayDesyncException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ReplayDesyncException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
