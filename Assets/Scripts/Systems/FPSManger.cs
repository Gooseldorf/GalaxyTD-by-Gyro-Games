using System.Collections;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace Systems
{
    public class FPSManger : MonoBehaviour
    {
        [SerializeField] private float startDelay = 5f;
        [SerializeField] private float checkDelay = 60f;
        [SerializeField] private float captureTime = 5f;
        [SerializeField] private int frameCaptureCount = 50;

        private WaitForSeconds checkTimeDelay;
        private WaitForSeconds captureTimeDelay;

        private ProfilerRecorder mainThreadTimeRecorder;

        private long[] frameTimes;

        private int upRateReqest;

        private int targetFrameRate = 60;

        private void Start()
        {
            checkTimeDelay = new WaitForSeconds(checkDelay);
            captureTimeDelay = new WaitForSeconds(captureTime / frameCaptureCount);
            frameTimes = new long[frameCaptureCount];
            StartCoroutine(StarterCoroutine());
        }

        private IEnumerator StarterCoroutine()
        {
            yield return new WaitForSeconds(startDelay);
            StartCoroutine(CaptureCoroutine());
        }

        private IEnumerator CaptureCoroutine()
        {
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
            for (int i = 0; i < frameCaptureCount; i++)
            {
                frameTimes[i] = mainThreadTimeRecorder.LastValue;
                yield return captureTimeDelay;
            }
            mainThreadTimeRecorder.Dispose();
            CheckChangeFrameRate();
        }

        private void CheckChangeFrameRate()
        {
            float avgTime = 0;
            for (int i = 0; i < frameTimes.Length; i++)
                avgTime += frameTimes[i] / 1000000;

            avgTime /= frameTimes.Length;

            if (Time.timeScale != 0)
            {
                switch (avgTime)
                {
                    //attempt to switch to 60 fps (1000/60 = 16.66f)
                    case < 15f when targetFrameRate != 60:
                        if (upRateReqest > 3)
                        {
                            if (targetFrameRate > 30)
                                ChangeFrameRate(60);
                            else
                                ChangeFrameRate(45);
                        }
                        else
                            upRateReqest++;
                        break;
                    //attempt to switch to 45 fps (1000/45 = 22.22f)
                    case < 20 when targetFrameRate != 45:
                        if (targetFrameRate > 45 || upRateReqest > 3)
                            ChangeFrameRate(45);
                        else
                            upRateReqest++;
                        break;
                    //reset request stack
                    case < 20 when targetFrameRate == 45:
                        upRateReqest = 0;
                        break;
                    //switch to 30 fps
                    case >= 20 when targetFrameRate != 30:
                        ChangeFrameRate(30);
                        break;
                    //reset request stack
                    case >= 20 when targetFrameRate == 30:
                        upRateReqest = 0;
                        break;
                }

                void ChangeFrameRate(int targetFrameRate)
                {
                    this.targetFrameRate = targetFrameRate;
                    upRateReqest = 0;
                    Application.targetFrameRate = targetFrameRate;
                    World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().RateManager.Timestep = (1.0f / targetFrameRate);
                    // Debug.Log($"Change target frame rate to {targetFrameRate}, couse avg frame time: {avgTime}");
                }
            }

            StartCoroutine(DelayCorotinue());
        }

        private IEnumerator DelayCorotinue()
        {
            yield return checkTimeDelay;
            StartCoroutine(CaptureCoroutine());
        }
    }
}