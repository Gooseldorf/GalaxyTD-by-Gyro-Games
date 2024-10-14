using ECSTest.Components;
using System.Collections.Generic;
using System.Text;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class FPSCounterPanel : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<FPSCounterPanel> {}
        
        private Label actualFpsLabel;
        private Label minMaxLabel;
        private Label missionNameLabel;
        private Label waveRewardLabel;
        private Label creepRewardLabel;
        private ClickableVisualElement resetFps;
        private ClickableVisualElement cheatButton;
        
        private float interval = 0.25f;
        //private int targetFrameRate = 9999;
        
        private readonly Vector2 gradientRange = new(15, 60);
        private readonly Gradient gradient = new();
        private readonly StringBuilder stringBuilder = new();
        
        //private int currentTargetFramerate;

        private float currentFPS;
        private float averageFPS;
        private float minimumFPS = 9999;
        private float maximumFPS;

        private int totalFrameCount;
        private int tempFrameCount;
        private float totalTimeStamp;
        private float tempTimeStamp;
        
        private bool isTimeManagerInitialized;

        private float lowestPercentFPS;
        private float lowPercentFPS;
        private Queue<float> fpsValues = new Queue<float>();
        private List<float> fpsValuesList = new List<float>();
        private int maxQueueSize = 2000;
        private bool isFirstSpikeDequeued = false;

        private double missionLoadingTime;

        private CheatPanel cheatPanel;
        
        public void Init(CheatPanel cheatPanel)
        {
            this.cheatPanel = cheatPanel;

            //cashComponent = GameServices.Instance.GetCashComponent();
            
            actualFpsLabel = this.Q<Label>("ActualFpsLabel");
            minMaxLabel = this.Q<Label>("MinMaxLabel");
            missionNameLabel = this.Q<Label>("MissionNameLabel");
            resetFps = this.Q<ClickableVisualElement>("ResetFPS");
            waveRewardLabel = this.Q<Label>("WaveReward");
            creepRewardLabel = this.Q<Label>("CreepReward");
            cheatButton = this.Q<ClickableVisualElement>("CheatButton");
            
            resetFps.RegisterCallback<ClickEvent>(OnResetClick);
            cheatButton.RegisterCallback<ClickEvent>(OnCheatClick);
            
            //Application.targetFrameRate = currentTargetFramerate = targetFrameRate;

            gradient.colorKeys = new GradientColorKey[]
            {
                new(new Color(1, 0, 0, 1), 0),
                new(new Color(1, 1, 0, 1), 0.5f),
                new(new Color(0, 1, 0, 1), 1f)
            };

            isTimeManagerInitialized = GameServices.IsLoaded;

            if (!isTimeManagerInitialized)
            {
                //TODO: need to rework
                // GameServices.ObjectLoaded += OnTimeManagerLoaded;
            }

            void OnTimeManagerLoaded(GameServices _)
            {
                //TODO: need to rework
                // GameServices.ObjectLoaded -= OnTimeManagerLoaded;
                isTimeManagerInitialized = true;
            }
            
            Reset();

            missionNameLabel.text = GameServices.Instance.CurrentMission.name;

            missionLoadingTime = InGameCheats.MissionInitedTime - InGameCheats.MissionStartLoadTime;

#if UNITY_EDITOR
            Show(true);
#else
            if(Debug.isDebugBuild)
                Show(true);
            else
                Show(false);
#endif
        }

        public void Dispose()
        {
            resetFps.UnregisterCallback<ClickEvent>(OnResetClick);
            cheatButton.UnregisterCallback<ClickEvent>(OnCheatClick);
        }

        private void OnResetClick(ClickEvent clk)
        {
            Reset();
        }

        private void OnCheatClick(ClickEvent clk)
        {
            // Debug.Log("CHEATER CHEATER CHEATER");
            cheatPanel.Show(true);
            GameServices.Instance.SetPause(true);
        }

        public void UpdateFPS()
        {
            /*if (!startCount)
                return;*/
            try
            {
                if (!isTimeManagerInitialized)
                    return;

                //if (currentTargetFramerate != targetFrameRate)
                    //Application.targetFrameRate = currentTargetFramerate = targetFrameRate;

                if (!GetFPS())
                    return;

                stringBuilder.Append($"FPS:<color={Rgba2Hex(EvaluateGradient(currentFPS))}><b>{currentFPS:F1}</b></color>\n");
                stringBuilder.Append($"{1000 / currentFPS:F2}ms\n");
                stringBuilder.Append($"Time: {GameServices.Instance.CurrentTime:F1}s\n");
                stringBuilder.Append($"1%:<color={Rgba2Hex(EvaluateGradient(lowestPercentFPS))}><b>{lowestPercentFPS:F1}</b></color>\n");
                stringBuilder.Append($"MissionLoadingTime:<b>{missionLoadingTime:F2}</b>");

                actualFpsLabel.text = stringBuilder.ToString();
                stringBuilder.Clear();

                stringBuilder.Append($"AVG:<color={Rgba2Hex(EvaluateGradient(averageFPS))}><b>{averageFPS:F1}</b></color>\n");
                stringBuilder.Append($"MIN:<color={Rgba2Hex(EvaluateGradient(minimumFPS))}><b>{minimumFPS:F1}</b></color>\n");
                stringBuilder.Append($"MAX:<color={Rgba2Hex(EvaluateGradient(maximumFPS))}><b>{maximumFPS:F1}</b></color>\n");
                stringBuilder.Append($"10%:<color={Rgba2Hex(EvaluateGradient(lowPercentFPS))}><b>{lowPercentFPS:F1}</b></color>");

                minMaxLabel.text = stringBuilder.ToString();
                stringBuilder.Clear();
            
                ShowCashValues();

            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private void ShowCashValues()
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.CompleteDependencyBeforeRO<CashComponent>();
            EntityQuery cashQuery = entityManager.CreateEntityQuery(new ComponentType[] { typeof(CashComponent) });

            cashQuery.TryGetSingleton(out CashComponent cashComponent);
            if (cashComponent.IsCreated)
            {
                creepRewardLabel.text = cashComponent.CashForCreeps.ToString();
                waveRewardLabel.text = cashComponent.CashForWaves.ToString();
            }
        }

        private static string Rgba2Hex(Color color) => $"#{ColorUtility.ToHtmlStringRGB(color)}";

        private bool GetFPS()
        {
            tempFrameCount++;
            totalFrameCount++;

            if (Time.realtimeSinceStartup - tempTimeStamp <= interval)
                return false;

            currentFPS = (float)(tempFrameCount / (Time.realtimeSinceStartup - tempTimeStamp));
            averageFPS = (float)(totalFrameCount / (Time.realtimeSinceStartup - totalTimeStamp));

            //fpsValues.Add(currentFPS);
            if (fpsValues.Count >= maxQueueSize)
                fpsValues.Dequeue();
            fpsValues.Enqueue(currentFPS);

            if (currentFPS < minimumFPS)
                minimumFPS = currentFPS;
            if (currentFPS > maximumFPS)
                maximumFPS = currentFPS;

            lowestPercentFPS = CalculatePercentileFPS(0.01f);
            lowPercentFPS = CalculatePercentileFPS(0.1f);

            tempTimeStamp = Time.realtimeSinceStartup;
            tempFrameCount = 0;
            
            //crutch for excluding first low spike value
            if (!isFirstSpikeDequeued)
            {
                fpsValues.Dequeue();
                minimumFPS = 9999;
                isFirstSpikeDequeued = true;
            }

            return true;
        }
        
        private float CalculatePercentileFPS(float percentile)
        {
            fpsValuesList.AddRange(fpsValues); //trade-off to opportunity of using queue for calculating percentiles
            fpsValuesList.Sort();

            int index = (int)(percentile * fpsValues.Count);
            
            List<float> minPercentileFPSValues =  fpsValuesList.GetRange(0, index + 1);
            
            //float averageMinPercentileFPS = minPercentileFPSValues.Average();

            float sum = 0;
            foreach (float fps in minPercentileFPSValues)
            {
                sum += fps;
            }
            
            float averageMinPercentileFPS = sum / minPercentileFPSValues.Count;

            fpsValuesList.Clear();

            return averageMinPercentileFPS;
        }
        
        private Color EvaluateGradient(float f) => gradient.Evaluate(Mathf.Clamp01((f - gradientRange.x) / (gradientRange.y - gradientRange.x)));
        
        private void Reset()
        {
            totalTimeStamp = Time.realtimeSinceStartup;
            tempTimeStamp = Time.realtimeSinceStartup;

            currentFPS = 0;
            averageFPS = 0;
            minimumFPS = 999.9f;
            maximumFPS = 0;
            lowestPercentFPS = 0;
            lowPercentFPS = 0;

            tempFrameCount = 0;
            totalFrameCount = 0;
            
            fpsValues.Clear();
        }
        
        public void Show(bool show)
        {
            visible = show;
        }
    }
}
