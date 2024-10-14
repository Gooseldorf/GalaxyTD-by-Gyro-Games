using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplayHook : MonoBehaviour
{
    [SerializeField, Sirenix.OdinInspector.ReadOnly]
    private bool delayedStart = false;
    [SerializeField]
    private Replay replay;

    public void Init(Replay replay, bool delayedStart)
    {
        this.replay = replay;
        this.delayedStart = delayedStart;
        if (!delayedStart)
            replay.Play(this);
    }

    public void Start()
    {
        //Debug.LogError("Start replay hook " + delayedStart);
        if (delayedStart)
            replay.Play(this);
    }

    public void InitMission() => replay.InitMission();
}
