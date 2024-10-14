using CardTD.Utilities;
using Data.Managers;
using DG.Tweening;
using ECSTest.Components;
using I2.Loc;
using Managers;
using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class LoseWindow : VisualElement
    {
        private const float rangeToCreepDestroy = 10f;
        private const int countPowerCell = 5;

        #region UxmlStaff

        public new class UxmlFactory : UxmlFactory<LoseWindow, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlAssetAttributeDescription<Sprite> ticketIcon = new() {name = "TicketIcon", defaultValue = null};
            UxmlAssetAttributeDescription<Sprite> adIcon = new() {name = "AdIcon", defaultValue = null};
            UxmlAssetAttributeDescription<Sprite> adButtonBackground = new() {name = "Ad_Button_Background", defaultValue = null};

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ticketIcon.TryGetValueFromBag(bag, cc, out Sprite value))
                    ((LoseWindow)ve).TicketIcon = value;
                if (adIcon.TryGetValueFromBag(bag, cc, out Sprite value1))
                    ((LoseWindow)ve).AdIcon = value1;
                if (adButtonBackground.TryGetValueFromBag(bag, cc, out Sprite value2))
                    ((LoseWindow)ve).AdButtonBackground = value2;
            }
        }

        public Sprite TicketIcon { get; set; }
        public Sprite AdIcon { get; set; }
        public Sprite AdButtonBackground { get; set; }

        #endregion

        private Label title;
        private VisualElement windowContainer;
        private VisualElement ticketsContainer;
        private Label ticketsLabel;
        private CommonButton restartButton;
        private CommonButton menuButton;
        private CommonButton secondChanceButton;
        private Label secondChanceLabel;
        private UIHelper uiHelper;
        private GameData gameData;

        private VisualElement adsIcon;

        private bool isShowed = false;

        public event Action ShowSecondChanceAnnouncement;

        public void Init()
        {
            title = this.Q<Label>("Title");
            windowContainer = this.Q<VisualElement>("Container");

            ticketsContainer = this.Q<VisualElement>("TicketsContainer");
            ticketsLabel = ticketsContainer.Q<Label>("TicketsLabel");
            ticketsContainer.style.display = GameServices.Instance.IsHard ? DisplayStyle.Flex : DisplayStyle.None;

            restartButton = this.Q<TemplateContainer>("RestartButton").Q<CommonButton>();
            restartButton.Init();
            restartButton.RegisterCallback<ClickEvent>(OnRestartClick);

            menuButton = this.Q<TemplateContainer>("MenuButton").Q<CommonButton>();
            menuButton.Init();
            menuButton.RegisterCallback<ClickEvent>(OnMenuClick);

            secondChanceButton = this.Q<CommonButton>("SecondChanceButton");
            adsIcon = secondChanceButton.Q<VisualElement>("Icon");
            secondChanceButton.Init();
            secondChanceLabel = secondChanceButton.Q<Label>();
            secondChanceButton.RegisterCallback<ClickEvent>(OnSecondChanceClick);

            style.display = DisplayStyle.None;

            uiHelper = UIHelper.Instance;
            gameData = DataManager.Instance.GameData;

            // Messenger.AddListener(GameEvents.ShowRewardVideo,ShowReward);
        }

        public void Dispose()
        {
            // Messenger.RemoveListener(GameEvents.ShowRewardVideo,ShowReward);
            restartButton.Dispose();
            restartButton.UnregisterCallback<ClickEvent>(OnRestartClick);

            menuButton.Dispose();
            menuButton.UnregisterCallback<ClickEvent>(OnMenuClick);

            secondChanceButton.Dispose();
            secondChanceButton.UnregisterCallback<ClickEvent>(OnSecondChanceClick);
        }

        public void Show(bool show, Action onAnimationComplete)
        {
            menuButton.SetBackground(DataManager.Instance.GameData.LastCompletedMissionIndex < 0 ? UIHelper.Instance.LockedCommonButtonBackground : UIHelper.Instance.AvailableCommonButtonBackground);
            isShowed = show;
            PlayLoseSound();

            if (GameServices.Instance.IsHard)
            {
                restartButton.SetIcon(gameData.Tickets > 0 ? TicketIcon.texture : AdIcon.texture);
                if (!DataManager.Instance.GameData.SkipAds)
                    restartButton.SetBackground(gameData.Tickets > 0 ? uiHelper.AvailableCommonButtonBackground : AdButtonBackground);
                ticketsLabel.text = DataManager.Instance.GameData.Tickets < 99 ? DataManager.Instance.GameData.Tickets.ToString() : "99+";
                restartButton.ShowIcon(!DataManager.Instance.GameData.SkipAds);
            }
            else
                restartButton.ShowIcon(false);


            Tween animation = show ? UIHelper.Instance.GetShowWindowTween(windowContainer, this) : UIHelper.Instance.GetHideWindowTween(windowContainer, this);
            animation.OnComplete(() =>
            {
                onAnimationComplete?.Invoke();
            });
            animation.Play();
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);

            if (!show)
                return;

            Messenger<bool>.Broadcast(GameEvents.ShowLoseWindow, secondChanceButton.visible, MessengerMode.DONT_REQUIRE_LISTENER);

            if (!DataManager.Instance.GameData.SkipAds)
            {
                if (GameServices.Instance.IsHard && DataManager.Instance.GameData.Tickets <= 0)
                    AdsManager.LoadReward(AdsRewardType.GetTicket);
                else if (secondChanceButton.visible)
                    AdsManager.LoadReward(AdsRewardType.SecondChance);
            }
        }

        private void OnRestartClick(ClickEvent clk)
        {
            PauseWindow.OnRestartButtonClick(restartButton, Restart);
        }

        private void Restart()
        {
            GameServices.Instance.SetPause(false);
            Show(false, () =>
            {
                Reset();
                GameServices.Instance.Restart();
            });
        }

        public void Reset()
        {
            adsIcon.visible = !DataManager.Instance.GameData.SkipAds;
            secondChanceButton.visible = true;
        }

        private void OnMenuClick(ClickEvent clk)
        {
            if (DataManager.Instance.GameData.LastCompletedMissionIndex < 0)
                return;

            GameServices.Instance.SetPause(false);
            GameServices.Instance.ReturnToMenu();
        }

        private void OnSecondChanceClick(ClickEvent clk)
        {
            AdsManager.TryShowReward(
                () =>
                {
                    adsIcon.visible = false;
                    secondChanceButton.visible = false;
                },
                () =>
                {
                    ShowReward();
                });
        }

        private void ShowReward()
        {
            if (!isShowed)
                return;

            int missionIndexIncrease = GameServices.Instance.CurrentMission.MissionIndex + 1;
            Messenger<AdsRewardType, int>.Broadcast(GameEvents.ShowAdsReward, AdsRewardType.SecondChance,missionIndexIncrease, MessengerMode.DONT_REQUIRE_LISTENER);

            GameServices.Instance.SetPause(false);
            Show(false, () => { DOVirtual.DelayedCall(1, () => ShowSecondChanceAnnouncement?.Invoke()); });
            var manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            Entity eventEntity = manager.CreateEntity();
            manager.SetName(eventEntity, "SecondChance");
            manager.AddComponentData(eventEntity, new SecondChanceEvent {Range = rangeToCreepDestroy, CountPowerCells = countPowerCell});

            MusicManager.PlayBattleSceneBackground();
        }

        public void UpdateLocalization()
        {
            title.text = LocalizationManager.GetTranslation("Defeat");
            uiHelper.SetLocalizationFont(title);
            restartButton.SetText(LocalizationManager.GetTranslation("Restart"));
            menuButton.SetText(LocalizationManager.GetTranslation("Menu"));
            secondChanceLabel.text = (LocalizationManager.GetTranslation("SecondChance"));
            uiHelper.SetLocalizationFont(secondChanceLabel);
        }
    }
}