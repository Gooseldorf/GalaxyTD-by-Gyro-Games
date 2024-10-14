using CardTD.Utilities;
using DG.Tweening;
using ECSTest.Components;
using I2.Loc;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class LivesWidget : VisualElement
    {
        private VisualElement leftTopCorner;
        private VisualElement leftBotCorner;
        private VisualElement rightBotCorner;
        private VisualElement rightTopCorner;
        private VisualElement energyCoresContainer;
        private CommonButton startButton;

        private UIHelper uiHelper;
        private Sequence cornersSequence;
        private Sequence onStartClickSeq;
        private bool gameStarted;
        private Tweener moveTweener;

        private float cornersAlpha
        {
            get => leftTopCorner.style.unityBackgroundImageTintColor.value.a;
            set
            {
                leftTopCorner.style.unityBackgroundImageTintColor =
                     leftBotCorner.style.unityBackgroundImageTintColor =
                      rightBotCorner.style.unityBackgroundImageTintColor =
                       rightTopCorner.style.unityBackgroundImageTintColor = new Color(1, 1, 1, value);
            }
        }

        public new class UxmlFactory : UxmlFactory<LivesWidget> { }

        private List<EnergyCoreWidget> energyCores;

        public void Init(VisualTreeAsset energyCorePrefab, VisualTreeAsset powerCellPrefab)
        {
            uiHelper = UIHelper.Instance;
            energyCoresContainer = this.Q<VisualElement>("EnergyCoresContainer");
            startButton = this.Q<VisualElement>("StartButton").Q<CommonButton>();
            startButton.SoundName = SoundKey.Interface_exitButton;
            startButton.Init();
            startButton.SetText(LocalizationManager.GetTranslation("Start"));
            startButton.RegisterCallback<ClickEvent>(OnStartButtonClick);
            ShowStartButton();
            
            leftTopCorner = parent.Q("LeftTopCorner");
            leftBotCorner = parent.Q("LeftBotCorner");
            rightBotCorner = parent.Q("RightBotCorner");
            rightTopCorner = parent.Q("RightTopCorner");
            
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery coreQuery = manager.CreateEntityQuery(new ComponentType[] { typeof(EnergyCoreComponent) });
            var cores = coreQuery.ToEntityArray(Allocator.Temp);
            energyCores = new List<EnergyCoreWidget>(cores.Length);
            for (int i = 0; i < cores.Length; i++)
            {
                EnergyCoreWidget core = energyCorePrefab.Instantiate().Q<EnergyCoreWidget>("EnergyCoreWidget");
                energyCores.Add(core);
                energyCoresContainer.Q().Add(energyCores[i]);
                energyCores[i].Init(cores[i], powerCellPrefab);
                energyCores[i].RegisterCallback<ClickEvent>(OnCoreClick);
                energyCores[i].visible = true;

            }
            cores.Dispose();
            cornersAlpha = 0;

            Messenger<PowerCellEvent>.AddListener(GameEvents.CellDetached, OnDetachCell);
            Messenger<PowerCellEvent>.AddListener(GameEvents.CellDestroyed, OnDestroyCell);
            Messenger<PowerCellEvent>.AddListener(GameEvents.CellAttached, OnReturnCell);

            Messenger<PowerCellEvent>.AddListener(GameEvents.CellDestroyedAll, OnDestroyedAll);
            Messenger<PowerCellEvent>.AddListener(GameEvents.CellAttachedNew, OnAddPowerCells);

            Messenger<Entity, bool>.AddListener(GameEvents.UpdateVisualWarning, OnWarningUpdate);
            
            Messenger.AddListener(GameEvents.Restart, ShowStartButton);
            Messenger<int>.AddListener(GameEvents.NextWave, OnWaveStarted);
        }

        private void OnDetachCell(PowerCellEvent powerCellEvent)
        {
            GetCoreWidget(powerCellEvent.Core)?.DetachCell();

            if (cornersSequence != null)
                cornersSequence.Kill();

            cornersAlpha = 0;
            cornersSequence = DOTween.Sequence();
            cornersSequence.Append(DOTween.To(() => cornersAlpha, x => cornersAlpha = x, 1, .5f));
            cornersSequence.Append(DOTween.To(() => cornersAlpha, x => cornersAlpha = x, 0, .5f));
            cornersSequence.OnComplete(() => cornersSequence = null);
            MusicManager.PlaySound2D(SoundKey.Cell_detached);
        }
        private void OnDestroyCell(PowerCellEvent powerCellEvent) => GetCoreWidget(powerCellEvent.Core)?.DestroyCell();
        private void OnReturnCell(PowerCellEvent powerCellEvent)
        {
            GetCoreWidget(powerCellEvent.Core)?.ReturnCell();
            MusicManager.PlaySound2D(SoundKey.Cell_backToCore);
        }
        private void OnDestroyedAll(PowerCellEvent powerCellEvent) => GetCoreWidget(powerCellEvent.Core)?.DestroyAll();
        private void OnAddPowerCells(PowerCellEvent powerCellEvent) => GetCoreWidget(powerCellEvent.Core)?.AddPowerCells(powerCellEvent);
        private void OnWarningUpdate(Entity entity, bool needToShow) => GetCoreWidget(entity)?.ActivateAlert(needToShow);

        private EnergyCoreWidget GetCoreWidget(Entity core)
        {
            for (int i = 0; i < energyCores.Count; i++)
                if (energyCores[i].EnergyCore == core)
                    return energyCores[i];
            return null;
        }

        public void Dispose()
        {
            startButton.Dispose();
            foreach (var core in energyCores)
            {
                core.UnregisterCallback<ClickEvent>(OnCoreClick);
                core.Dispose();
            }

            Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellDetached, OnDetachCell);
            Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellDestroyed, OnDestroyCell);
            Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellAttached, OnReturnCell);

            Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellDestroyedAll, OnDestroyedAll);
            Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellAttachedNew, OnAddPowerCells);

            Messenger<Entity, bool>.RemoveListener(GameEvents.UpdateVisualWarning, OnWarningUpdate);

            Messenger.RemoveListener(GameEvents.Restart, ShowStartButton);
            Messenger.RemoveListener<int>(GameEvents.NextWave, OnWaveStarted);
        }

        public void Reset()
        {
            foreach (var core in energyCores)
            {
                core.Reset();
            }

            gameStarted = false;
        }

        private void OnCoreClick(ClickEvent clk)
        {
            EnergyCoreWidget clickedCore = (EnergyCoreWidget)clk.currentTarget;
            uiHelper.InOutScaleTween(clickedCore, 1f, 1.05f, 0.3f).SetUpdate(true).Play();
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            PositionComponent pos = manager.GetComponentData<PositionComponent>(clickedCore.EnergyCore);

            if (moveTweener != null)
                moveTweener.Kill();

            moveTweener = TouchCamera.Instance.MoveToPosition(pos.Position)
                .OnComplete(() => moveTweener = null);
        }

        public List<VisualElement> GetActivePowerCellsForAnimation()
        {
            List<VisualElement> powerCells = new();
            foreach (var core in energyCores)
            {
                powerCells.AddRange(core.GetActiveCellsForAnimation());
            }

            return powerCells;
        }

        private void OnStartButtonClick(ClickEvent evt)
        {
            startButton.pickingMode = PickingMode.Ignore;
            gameStarted = true;
            DOTween.Kill(startButton, true);
            onStartClickSeq = DOTween.Sequence();
            //onStartClickSeq.Append(uiHelper.InOutScaleTween(startButton, startButton.resolvedStyle.scale.value.x, 1.1f, 0.6f));
            onStartClickSeq.Append(uiHelper.FadeTween(startButton, 1, 0, .2f));
            onStartClickSeq.Append(uiHelper.FadeTween(energyCoresContainer, 0, 1, .2f));
            onStartClickSeq.SetUpdate(true).SetTarget(startButton).OnComplete(() => startButton.style.display = DisplayStyle.None).Play();
            GameServices.Instance.SkipFirstWaveOffset();
            MusicManager.PlaySound2D(SoundKey.Interface_start);
            foreach (EnergyCoreWidget core in energyCores)
                core.StartProgressBar();
        }

        private void ShowStartButton()
        {
            gameStarted = false;
            startButton.style.display = DisplayStyle.Flex;
            startButton.style.opacity = 0;
            Sequence showSeq = DOTween.Sequence();
            showSeq.Append(uiHelper.FadeTween(energyCoresContainer, energyCoresContainer.resolvedStyle.opacity, 0, .2f));
            showSeq.Append(uiHelper.FadeTween(startButton, 0, 1, .2f));
            showSeq.OnComplete(() => startButton.pickingMode = PickingMode.Position);
            showSeq.SetUpdate(true).SetTarget(startButton).Play();
            uiHelper.InOutScaleTween(startButton, 1, 1.03f, 2).SetLoops(-1, LoopType.Restart).SetUpdate(true).SetTarget(startButton).Play();
        }

        private void OnWaveStarted(int waveNum = 0)
        {
            if (!gameStarted)
                OnStartButtonClick(new ClickEvent());
        }
    }
}