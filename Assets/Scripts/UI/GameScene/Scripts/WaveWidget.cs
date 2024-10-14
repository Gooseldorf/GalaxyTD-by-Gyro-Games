using CardTD.Utilities;
using DG.Tweening;
using ECSTest.Systems;
using I2.Loc;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class WaveWidget : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<WaveWidget>
        {
        }

        private NextWaveAnnouncement nextWaveAnnouncement;
        private VisualElement waveNumberContainer;
        private Label waveLabel;

        private VisualElement progressBar;
        private VisualElement progressBarFilling;
        private Sequence progressBarSeq;

        private readonly List<WaveLine> waveLines = new();
        private List<TemplateContainer> waveLineTemplates = new();
        private Sequence waveChangeSeq;
        private int resolveCounter;

        private int currentWave = 0;
        private int maxWave;

        private List<CreepStats> creepStats = new();
        private int[] creepCounts;
        private int[] creepHps;
        private readonly List<float2> positions = new();

        private readonly List<float> scales = new()
        {
            1,
            1,
            0.85f,
            0.7f,
            0.7f
        };

        private Mission mission;
        private int unitsLeftToSpawnInCurrentWave;
        private int totalUnitsCount;
        private UIHelper uiHelper;

        public ClickableVisualElement CheatButton;

        private string MaxCount => GameServices.Instance.IsRoguelike ? "?" : $"{maxWave}";

        public void Init()
        {
            nextWaveAnnouncement = this.parent.Q<NextWaveAnnouncement>("NextWaveAnnouncement");
            nextWaveAnnouncement.Init();

            waveNumberContainer = this.Q<VisualElement>("WaveNumberContainer");
            waveLabel = this.Q<Label>("WaveLabel");
            progressBar = this.Q<VisualElement>("WaveProgressBar");
            progressBarFilling = progressBar.Q<VisualElement>("Filling");

            waveLineTemplates = this.Query<TemplateContainer>("WaveLineTemplate").ToList();
            resolveCounter = 0;
            foreach (TemplateContainer waveLineTemplate in waveLineTemplates)
            {
                waveLineTemplate.RegisterCallback<GeometryChangedEvent>(OnWaveLineTemplateResolve);
                waveLines.Add(waveLineTemplate.Q<WaveLine>());
            }

            foreach (WaveLine waveLine in waveLines) waveLine.Init();

            Messenger<int>.AddListener(GameEvents.UpdateUnitsLeftToSpawn, UpdateUnitsLeftToSpawn);
            Messenger<int>.AddListener(GameEvents.NextWave, OnNextWaveEvent);
            Messenger<int>.AddListener(GameEvents.UnitSpawned, UpdateUnitsCount);
            SetupData();
            waveLabel.text = $"1/{MaxCount}";
            SetWavesToWaveLines();

            CheatButton = this.Q<ClickableVisualElement>("CheatButton");
        }

        public void Dispose()
        {
            Messenger<int>.RemoveListener(GameEvents.UpdateUnitsLeftToSpawn, UpdateUnitsLeftToSpawn);
            Messenger<int>.RemoveListener(GameEvents.NextWave, OnNextWaveEvent);
            Messenger<int>.RemoveListener(GameEvents.UnitSpawned, UpdateUnitsCount);
            progressBarSeq?.Kill();
            waveChangeSeq?.Kill();
        }

        public void ShowReset()
        {
            Sequence resetSeq = DOTween.Sequence();
            nextWaveAnnouncement.Reset();
            resetSeq.Append(uiHelper.TranslateXTween(this, 0, resolvedStyle.width, 1).OnComplete(Reset));
            resetSeq.Append(uiHelper.TranslateXTween(this, resolvedStyle.width, 0, 1));
            resetSeq.SetUpdate(true).Play();
        }


        private void Reset()
        {
            currentWave = 0;
            nextWaveAnnouncement.Reset();
            SetWavesToWaveLines();
            foreach (WaveLine waveLine in waveLines)
                waveLine.Show();
            waveLabel.text = $"1/{MaxCount}";
            progressBarSeq?.Kill(true);
            waveChangeSeq?.Kill(true);
            
            totalUnitsCount = 0;
            foreach (int count in creepCounts)
                totalUnitsCount += count;
            unitsLeftToSpawnInCurrentWave = creepCounts[currentWave];
        }

        private void Show(bool show) => uiHelper.TranslateXTween(this, show ? resolvedStyle.width : 0, show ? 0 : resolvedStyle.width, 1).Play();

        private void SetupData()
        {
            maxWave = GameServices.Instance.CurrentMission.WavesCount;
            mission = GameServices.Instance.CurrentMission;
            uiHelper = UIHelper.Instance;

            creepStats = mission.CreepStatsPerWave;
            creepCounts = GetCreepCounts();
            totalUnitsCount = 0;
            foreach (int count in creepCounts)
                totalUnitsCount += count;
            unitsLeftToSpawnInCurrentWave = creepCounts[currentWave];
            creepHps = GetCreepHps();
        }


        private void UpdateUnitsLeftToSpawn(int count)
        {
            totalUnitsCount = count;
        }

        private void UpdateUnitsCount(int unitsLeftToSpawn)
        {
            int spawnedUnitsCount = totalUnitsCount - unitsLeftToSpawn;
            waveLines[1].UpdateUnitsCount(unitsLeftToSpawnInCurrentWave - spawnedUnitsCount);
        }

        private void OnWaveLineTemplateResolve(GeometryChangedEvent geom)
        {
            TemplateContainer target = (TemplateContainer)geom.currentTarget;
            target.UnregisterCallback<GeometryChangedEvent>(OnWaveLineTemplateResolve);
            resolveCounter++;

            if (resolveCounter >= waveLineTemplates.Count)
            {
                for (int i = 0; i < waveLineTemplates.Count; i++)
                {
                    positions.Add(new float2(waveLineTemplates[i].resolvedStyle.left, waveLineTemplates[i].resolvedStyle.top));
                    waveLineTemplates[i].style.scale = new Scale(new Vector2(scales[i], scales[i]));
                }
            }

            Show(true);
        }

        private void OnNextWaveEvent(int waveNum)
        {
            if (waveNum >= maxWave && !GameServices.Instance.IsRoguelike) return;

            currentWave = waveNum;
            AnimateWaveIndex();
            AnimateProgressBar();
            if (waveNum > 0)
                AnimateWaveLines();
            ShowNextWaveAnnouncement();
        }

        private void AnimateProgressBar()
        {
            progressBarSeq = DOTween.Sequence();

            progressBar.style.width = Length.Percent(0);
            progressBarFilling.style.opacity = 0.3f;

            float duration = (SpawnerSystem.WaveTimeLength);

            Tween waveProgressBarTween = DOTween.To(
                () => progressBar.style.width.value.value,
                x => progressBar.style.width = Length.Percent(x),
                100,
                duration
            );
            waveProgressBarTween.SetEase(Ease.Linear);
            Tween fillingFadingTween = uiHelper.FadeTween(progressBarFilling, 0.3f, 0, 2);

            progressBarSeq.Append(waveProgressBarTween);
            progressBarSeq.Append(fillingFadingTween);
            progressBarSeq.SetDelay(UIHelper.WaveAnnouncementOffset);
        }

        private void AnimateWaveIndex()
        {
            waveLabel.text = $"{currentWave + 1}/{MaxCount}";
            uiHelper.InOutScaleTween(waveNumberContainer, 1, 1.2f, 1);
        }

        private void SetWavesToWaveLines()
        {
            for (int i = 1; i < waveLines.Count; i++)
            {
                int waveIndex = (currentWave + i - 1);

                if (GameServices.Instance.IsRoguelike)
                    waveIndex %= maxWave;

                if (waveIndex >= creepStats.Count || waveIndex >= creepCounts.Length)
                {
                    waveLines[i].Hide();
                    continue;
                }

                waveLines[i].SetWave(creepStats[waveIndex], creepCounts[waveIndex]);
            }
        }

        private void AnimateWaveLines()
        {
            waveChangeSeq = DOTween.Sequence();
            for (int i = 1; i < waveLines.Count; i++)
            {
                waveChangeSeq.Insert(0, uiHelper.MoveWithLeftTopOffsets(waveLineTemplates[i], positions[i - 1], 1));
                waveChangeSeq.Insert(0, uiHelper.ScaleTween(waveLineTemplates[i], scales[i], scales[i - 1], 1));
                if (i == 2)
                    waveChangeSeq.Insert(0, uiHelper.FadeTween(waveLineTemplates[i], 0.5f, 1, 1));
            }

            waveChangeSeq.OnComplete(() =>
            {
                SetWavesToWaveLines();
                for (int i = 0; i < waveLines.Count; i++)
                {
                    waveLineTemplates[i].style.left = positions[i].x;
                    waveLineTemplates[i].style.top = positions[i].y;
                    waveLineTemplates[i].style.scale = new Scale(new Vector2(scales[i], scales[i]));
                }

                totalUnitsCount -= unitsLeftToSpawnInCurrentWave;
                unitsLeftToSpawnInCurrentWave = creepCounts[currentWave % maxWave];
                waveLineTemplates[2].style.opacity = new StyleFloat(0.5f);
            });
            waveChangeSeq.SetUpdate(true);
            waveChangeSeq.Play();
        }

        private void ShowNextWaveAnnouncement()
        {
            string nextWaveText = currentWave >= maxWave && !GameServices.Instance.IsRoguelike
                ? $"{LocalizationManager.GetTranslation("GameScene/LastWave")}"
                : $"{LocalizationManager.GetTranslation("GameScene/Wave")}";

            int waveIndex = currentWave % maxWave;
            float hpModifer = (GameServices.Instance.IsRoguelike) ? mission.GetRoguelikeHpModifer(waveIndex, currentWave / maxWave) : mission.HpModifier;

            int hp = (int)(creepHps[waveIndex] * hpModifer);

            nextWaveAnnouncement.SetUp(creepStats[waveIndex], creepCounts[waveIndex], hp, nextWaveText, currentWave + 1);
            nextWaveAnnouncement.Show();
        }

        private int[] GetCreepCounts()
        {
            int[] result = new int[mission.WavesCount];
            foreach (SpawnGroup spawnGroup in mission.SpawnData)
            {
                foreach (Wave wave in spawnGroup.Waves)
                    result[wave.WaveNum] += wave.Count;
            }

            return result;
        }

        private int[] GetCreepHps()
        {
            int[] result = new int[mission.WavesCount];
            foreach (SpawnGroup spawnGroup in mission.SpawnData)
            {
                foreach (Wave wave in spawnGroup.Waves)
                    result[wave.WaveNum] = wave.CreepHp;
            }

            return result;
        }
    }
}