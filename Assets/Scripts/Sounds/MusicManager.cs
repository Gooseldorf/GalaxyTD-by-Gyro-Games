using DarkTonic.MasterAudio;
using DG.Tweening;
using Sounds.Attributes;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MusicManager
{
    private const string prefabDirectoryPath = "MasterAudio";
    private const string prefabName = "MasterAudioPrefab";
    private const float battleMusicReplayDelay = 20;
    
    private static List<string> currentPlayList = new();
    private static bool enableMusic = true;
    private static bool enableSound = true;

    private static readonly List<string> soundBusesForMuteOnPause = new() {"ShootProjectile", "Muzzle", "Impact"};

    private static bool masterAudioCreated;
    private static bool hasCamera;
    private static Camera camera;

    private static PlaylistController battleController;
    private static PlaylistController battleControllerIntense;

    public static bool IsIntenseBattleMusicOn;
    
    public static bool EnableMusic
    {
        get => enableMusic;
        set
        {
            enableMusic = value;
            if (value) ContinuePlayingBackground();
            else StopPlayingBackground();

            MusicSettings.EnableMusic = enableMusic;
        }
    }

    public static bool EnableSound
    {
        get => enableSound;
        set
        {
            enableSound = value;
            MusicSettings.EnableSound = enableSound;
        }
    }

    public static bool IsReady
    {
        get
        {
            if (PlaylistController.Instances.Count == 0) return false;
            foreach (PlaylistController playlistController in PlaylistController.Instances)
            {
                if (!playlistController.ControllerIsReady)
                    return false;
            }
            if(!MasterAudio.SoundsReady) return false;
            return true;
        }
    }

    [RuntimeInitializeOnLoadMethod]
    private static void InitMasterAudioIfNeedIt()
    {
        EnableMusic = MusicSettings.EnableMusic;
        EnableSound = MusicSettings.EnableSound;

        if(Object.FindAnyObjectByType<MasterAudio>()!=null)
            return;
        
        Object masterAudio = Object.Instantiate(Resources.Load($"{prefabDirectoryPath}/{prefabName}"));
        Object.DontDestroyOnLoad(masterAudio);
        masterAudioCreated = true;
    }
    
    public static void StopAllSounds()
    {
        MasterAudio.StopBus("UI");
        MasterAudio.StopBus("Map");
        MasterAudio.StopBus("Creep");
        MasterAudio.StopBus("Impact");
        MasterAudio.StopBus("Muzzle");
        MasterAudio.StopBus("TowerChange");
        MasterAudio.StopBus("EMPTY BUS");
    }
    
    public static void Clear()
    {
        hasCamera = false;
    }

    public static void MuteOnPauseEvent(bool isPaused)
    {
        if(Object.FindAnyObjectByType<MasterAudio>()==null)
            return;
        
        if(MasterAudio.Instance==null)
            return;
        
        List<GroupBus> buses = MasterAudio.GroupBuses;

        foreach (GroupBus bus in buses)
        {
            if (soundBusesForMuteOnPause.Contains(bus.busName))
            {
                if (isPaused)
                    MasterAudio.MuteBus(bus.busName);
                else
                    MasterAudio.UnmuteBus(bus.busName);
            }
        }
    }

    #region public 2DSounds

    public static void PlayMainMenuBackground()
    {
        ChangeBackgroundPlaylist(new List<string> { SoundKey.MainMenuControllerName });
        UnsubBattleController();
    }

    public static void PlayBattleSceneBackground()
    {
        ChangeBackgroundPlaylist(new List<string> { SoundKey.BattleControllerName, SoundKey.BattleControllerIntenseName, SoundKey.AmbientControllerName });
        if (battleController == null || battleControllerIntense == null)
        {
            battleController = PlaylistController.Instances.Find(x => x.ControllerName == SoundKey.BattleControllerName);
            battleControllerIntense = PlaylistController.Instances.Find(x => x.ControllerName == SoundKey.BattleControllerIntenseName);
        }
        UnsubBattleController();
        battleController.SongEnded += ReplayBattlePlaylistWithDelay;
        battleControllerIntense.PlaylistVolume = 0.01f;
        IsIntenseBattleMusicOn = false;
    }

    public static void IncreaseBattleMusicIntensity()
    {
        IsIntenseBattleMusicOn = true;
        battleControllerIntense.FadeToVolume(1, 2);
    }

    public static void DecreaseBattleMusicIntensity()
    {
        IsIntenseBattleMusicOn = false;
        battleControllerIntense.FadeToVolume(0.01f, 2);
    }

    public static void PlayWinSound()
    {
        if(!EnableMusic) return;
        StopPlayingBackground();
        UnsubBattleController();
        PlaySound2D(SoundKey.Interface_victory);
    }

    public static void PlayLoseSound()
    {
        if(!EnableMusic) return;
        StopPlayingBackground();
        UnsubBattleController();
        PlaySound2D(SoundKey.Interface_defeat);
    }

    public static void PlaySound2D(string soundKey)
    {
        if(CanPlaySound(soundKey))
            MasterAudio.PlaySound(soundKey);
    }

    public static void PlayTypewriterSound() => PlaySound2D(SoundKey.Typewriter);

    public static void StopTypewriterSound() => StopSound2D(SoundKey.Typewriter);
    
    #endregion

    #region public 3DSounds

    public static void Play3DSoundOnTransform(string soundType, Transform target)
    {
        if (!CanPlaySound(soundType) || target == null) 
            return;
        if(IsVisibleToCamera(new float2(target.position.x, target.position.y)))
            MasterAudio.PlaySound3DAtTransform(soundType, target);
    }

    public static void StopSound3D(string soundType, Transform soundTransformHolder)
    {
        if (!CanPlaySound(soundType) || soundTransformHolder == null)
            return;
        
        MasterAudio.StopSoundGroupOfTransform(soundTransformHolder, soundType);
    }

    public static void PlayTeleportSound(Transform inPos, Transform outPos)
    {
        if(CanPlaySound(SoundKey.TeleportIn) && IsVisibleToCamera(new float2(inPos.position.x, inPos.position.y)))
            Play3DSoundOnTransform(SoundKey.TeleportIn, inPos);
        if(CanPlaySound(SoundKey.TeleportOut) && IsVisibleToCamera(new float2(outPos.position.x, outPos.position.y)))    
            Play3DSoundOnTransform(SoundKey.TeleportOut, outPos);
    }

    public static void PlayCreepHit(AllEnums.FleshType fleshType, float2 position)
    {
        if(IsVisibleToCamera(position))
            Play3DSoundOnPosition(SoundKey.CreepHitPrefics + fleshType, position);
    }

    public static void PlayCreepDeath(AllEnums.FleshType fleshType, AllEnums.ArmorType armorType, float2 position)
    {
        if(IsVisibleToCamera(position))
            Play3DSoundOnPosition(SoundKey.CreepDeathPrefics + fleshType + "_" + armorType, position);
    }

    public static void PlayMuzzleOrImpact(AllEnums.TowerId towerType, bool isMuzzle, float2 position)
    {
        if(!IsVisibleToCamera(position)) return;

        string type = isMuzzle ? "muzzle" : "impact";
        string soundKey = towerType + "_" + type;

        Play3DSoundOnPosition(soundKey, position);
    }
    
    public static void PlayGatlingMuzzleSound(Transform gatlingTransform)
    {
        if(!CanPlaySound(AllEnums.TowerId.Gatling + "_muzzle")) return;
        
        if (!MasterAudio.IsTransformPlayingSoundGroup(AllEnums.TowerId.Gatling + "_muzzle", gatlingTransform))
        {
            Play3DSoundOnTransform(AllEnums.TowerId.Gatling + "_muzzle", gatlingTransform);
        }
    }

    public static void StopGatlingMuzzleSound(Transform gatlingTransform)
    {
        if(!CanPlaySound(AllEnums.TowerId.Gatling + "_muzzle")) return;

        MasterAudio.StopSoundGroupOfTransform(gatlingTransform, AllEnums.TowerId.Gatling + "_muzzle");
        
        if(!MasterAudio.IsTransformPlayingSoundGroup("Gatling_end", gatlingTransform))
            Play3DSoundOnTransform("Gatling_end", gatlingTransform);
    }

    #endregion

    private static void UnsubBattleController()
    {
        if(battleController != null) battleController.SongEnded -= ReplayBattlePlaylistWithDelay;
    }
    
    private static void StopSound2D(string soundKey)
    {
        if(CanPlaySound(soundKey))
            MasterAudio.StopAllOfSound(soundKey);
    }
    
    private static void ReplayBattlePlaylistWithDelay(string songId)
    { 
        if(!EnableMusic) return;
        
        DOVirtual.DelayedCall(battleMusicReplayDelay,()=>
        {
            battleController.StartPlaylist(battleController.CurrentPlaylist.playlistName);
            battleControllerIntense.StartPlaylist(battleControllerIntense.CurrentPlaylist.playlistName);
        }).SetUpdate(true);
    }
    
    private static bool CanPlaySound(string soundKey) //TODO: Check if this needed
    {
        if (EnableSound == false || string.IsNullOrEmpty(soundKey) || soundKey == SoundConstants.EmptyKey)
            return false;
        
        if(!masterAudioCreated && Object.FindAnyObjectByType<MasterAudio>() == null)
            return false;
        
        return true;
    }

    private static void Play3DSoundOnPosition(string soundType, float2 position)
    {
        if (!CanPlaySound(soundType))
            return;
        
        MasterAudio.PlaySound3DAtVector3AndForget(soundType, new Vector3(position.x, position.y, 0));
    }

    private static bool IsVisibleToCamera(float2 position)
    {
        if (!TryGetCamera()) return false;

        try
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(new Vector3(position.x, position.y, 0));
            return viewportPoint.z > 0 && viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1;
        }
        catch
        {
            return false;
        }
    }
    
    private static bool TryGetCamera()
    {
        if (hasCamera)
            return true;

        if (TouchCamera.Instance == null)
            return false;
        
        camera = TouchCamera.Instance.MainCamera;
        if (camera != null)
        {
            hasCamera = true;
            return true;
        }

        return false;
    }

    private static void ContinuePlayingBackground()
    {
        foreach (PlaylistController controller in PlaylistController.Instances)
            if (currentPlayList.Contains(controller.name))
                controller.StartPlaylist(controller.CurrentPlaylist.playlistName);
    }

    private static void StopPlayingBackground()
    {
        foreach (PlaylistController controller in PlaylistController.Instances)
            if (controller.ActiveAudioSource.isPlaying)
                controller.StopPlaylist();
    }

    private static void ChangeBackgroundPlaylist(List<string> playListControllerNames)
    {
        currentPlayList = playListControllerNames;

        if (EnableMusic == false)
            return;

        List<PlaylistController> controllers = PlaylistController.Instances;
        foreach (PlaylistController controller in controllers)
        {
            if (!currentPlayList.Contains(controller.name))
            {
                if (controller.ActiveAudioSource.isPlaying)
                    controller.StopPlaylist();
            }
            else if (!controller.ActiveAudioSource.isPlaying)
            {
                if (controller.CurrentPlaylist != null)
                {
                    controller.StartPlaylist(controller.CurrentPlaylist.playlistName);
                }
            }
        }
    }
}

public static class MusicSettings
{
    public static bool EnableMusic
    {
        get => PlayerPrefs.GetInt(nameof(EnableMusic), 1) == 1;
        set
        {
            PlayerPrefs.SetInt(nameof(EnableMusic), value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool EnableSound
    {
        get => PlayerPrefs.GetInt(nameof(EnableSound), 1) == 1;
        set
        {
            PlayerPrefs.SetInt(nameof(EnableSound), value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}