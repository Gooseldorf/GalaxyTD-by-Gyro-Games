using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class TestFrameTimeChecker : MonoBehaviour
{
    private GUIStyle mStyle;
    private readonly FrameTiming[] mFrameTimings = new FrameTiming[1];

    private ProfilerRecorder mainThreadTimeRecorder;
    private ProfilerRecorder renderThreadTimeRecorder;

    void OnEnable()
    {
        // Create ProfilerRecorder and attach it to a counter
        mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
        renderThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time");
    }

    void OnDisable()
    {
        // Recorders must be explicitly disposed after use
        mainThreadTimeRecorder.Dispose();
    }

    void Awake()
    {
        mStyle = new GUIStyle();
        mStyle.fontSize = 50;
        mStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        CaptureTimings();

        var reportMsg =
            $"\nCPU: {mFrameTimings[0].cpuFrameTime}" +
            $"\nMain Thread: {mFrameTimings[0].cpuMainThreadFrameTime:00.00}" +
            $"\nRender Thread: {mFrameTimings[0].cpuRenderThreadFrameTime:00.00}" +
            $"\nGPU: {mFrameTimings[0].gpuFrameTime:00.00}";

        var reportMsg2 =
            $"\nCPU: {mainThreadTimeRecorder.LastValue}" +
            $"\nGPU: {renderThreadTimeRecorder.LastValue}";

        var oldColor = GUI.color;
        GUI.color = new Color(1, 1, 1, 1);
        float w = 600, h = 610;

        GUILayout.BeginArea(new Rect(32, 50, w, h), "Frame Stats", GUI.skin.window);
        GUILayout.Label(reportMsg, mStyle);
        GUILayout.Label(reportMsg2, mStyle);
        GUILayout.EndArea();

        GUI.color = oldColor;
    }

    private void CaptureTimings()
    {
        FrameTimingManager.CaptureFrameTimings();
        FrameTimingManager.GetLatestTimings((uint)mFrameTimings.Length, mFrameTimings);
    }
}
