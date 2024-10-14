using CardTD.Utilities;
using Data.Managers;
using DG.Tweening;
using I2.Loc;
using Managers;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class PauseWindow : VisualElement
    {
        #region UxmlStaff
        
        public new class UxmlFactory : UxmlFactory<PauseWindow, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlAssetAttributeDescription<Sprite> ticketIcon = new() { name = "TicketIcon", defaultValue = null };
            UxmlAssetAttributeDescription<Sprite> adIcon = new() { name = "AdIcon", defaultValue = null };
            UxmlAssetAttributeDescription<Sprite> adButtonBackground = new(){ name = "Ad_Button_Background", defaultValue = null };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ticketIcon.TryGetValueFromBag(bag, cc, out Sprite value))
                    ((PauseWindow)ve).TicketIcon = value;
                if (adIcon.TryGetValueFromBag(bag, cc, out Sprite value1))
                    ((PauseWindow)ve).AdIcon = value1;
                if (adButtonBackground.TryGetValueFromBag(bag, cc, out Sprite value2))
                    ((PauseWindow)ve).AdButtonBackground = value2;
            }
        }
        
        public Sprite TicketIcon { get; set; }
        public Sprite AdIcon { get; set; }
        public Sprite AdButtonBackground { get; set; }
        
        #endregion

        private Label title;
        private VisualElement windowContainer;
        private CommonButton restartButton;
        private CommonButton menuButton;
        private ClickableVisualElement closeButton;
        private SettingsWidget settingsWidget;
        private VisualElement ticketsContainer;
        private Label ticketsLabel;
        private UIHelper uiHelper;

        private GameData gameData;

        public void Init()
        {
            uiHelper = UIHelper.Instance;
            windowContainer = this.Q<VisualElement>("Container");

            title = this.Q<Label>("Title");

            restartButton = this.Q<TemplateContainer>("RestartButton").Q<CommonButton>();
            restartButton.Init();
            restartButton.RegisterCallback<ClickEvent>(OnRestartButtonClick);

            menuButton = this.Q<TemplateContainer>("MenuButton").Q<CommonButton>();
            menuButton.Init();
            menuButton.RegisterCallback<ClickEvent>(OnMenuButtonClick);

            closeButton = this.Q<ClickableVisualElement>("CloseButton");
            closeButton.SoundName = SoundKey.Interface_exitButton;
            closeButton.Init();
            closeButton.RegisterCallback<ClickEvent>(OnCloseButtonClick);

            settingsWidget = this.Q<SettingsWidget>("SettingsWidget");
            settingsWidget.Init();
            
            ticketsContainer = this.Q<VisualElement>("TicketsContainer");
            ticketsLabel = ticketsContainer.Q<Label>("TicketsLabel");
            ticketsContainer.style.display = GameServices.Instance.IsHard ? DisplayStyle.Flex : DisplayStyle.None;

            gameData = DataManager.Instance.GameData;
            style.display = DisplayStyle.None;
        }

        public void Dispose()
        {
            restartButton.UnregisterCallback<ClickEvent>(OnRestartButtonClick);
            restartButton.Dispose();

            menuButton.UnregisterCallback<ClickEvent>(OnMenuButtonClick);
            menuButton.Dispose();

            closeButton.UnregisterCallback<ClickEvent>(OnCloseButtonClick);
            closeButton.Dispose();

            settingsWidget.Dispose();
        }

        public void Show(bool show)
        {
            menuButton.SetBackground(DataManager.Instance.GameData.LastCompletedMissionIndex < 0 ? UIHelper.Instance.LockedCommonButtonBackground : UIHelper.Instance.AvailableCommonButtonBackground);

            if (GameServices.Instance.IsHard && !DataManager.Instance.GameData.SkipAds)
            {
                restartButton.SetIcon(gameData.Tickets > 0 ? TicketIcon.texture : AdIcon.texture);
                restartButton.SetBackground(gameData.Tickets > 0 ? UIHelper.Instance.AvailableCommonButtonBackground : AdButtonBackground);
                ticketsLabel.text = DataManager.Instance.GameData.Tickets < 99 ? DataManager.Instance.GameData.Tickets.ToString() : "99+";
            }
            
            Tween animation = show ? UIHelper.Instance.GetShowWindowTween(windowContainer, this) : UIHelper.Instance.GetHideWindowTween(windowContainer, this);
            if (show)
                GameServices.Instance.SetPause(true);
            animation.OnComplete(() =>
            {
                TouchCamera.Instance.CanDrag = !show;
            });
            animation.Play().SetUpdate(true);
            
            if (!show)
                return;

            if (DataManager.Instance.GameData.Tickets <= 0)
                AdsManager.LoadReward(AdsRewardType.GetTicket);
            
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void OnCloseButtonClick(ClickEvent clk)
        {
            GameServices.Instance.SetPause(false);
            Show(false);
        }

        private void OnRestartButtonClick(ClickEvent clk)
        {
            OnRestartButtonClick(restartButton, () =>
            {
                GameServices.Instance.SetPause(false);
                Show(false);
                TouchCamera.Instance.CanDrag = true;
                GameServices.Instance.Restart();
            });
        }

        public static void OnRestartButtonClick(CommonButton button, Action OnRestart)
        {
            if (button.State != AllEnums.UIState.Available) return;

            if (GameServices.Instance.IsHard)
            {
                var gameData = DataManager.Instance.GameData;
                if (gameData.Tickets <= 0)
                {
                    AdsManager.TryShowReward(null, () =>
                    {
                        OnRestart?.Invoke();
                    });
                    return;
                }

                gameData.SpendTicket();
            }

            OnRestart?.Invoke();
        }

        private void OnMenuButtonClick(ClickEvent clk)
        {
            if (DataManager.Instance.GameData.LastCompletedMissionIndex < 0)
                return;

            GameServices.Instance.ReturnToMenu();
        }

        public void UpdateLocalization()
        {
            title.text = LocalizationManager.GetTranslation("Pause");
            uiHelper.SetLocalizationFont(title);
            restartButton.SetText(LocalizationManager.GetTranslation("Restart"));
            menuButton.SetText(LocalizationManager.GetTranslation("Menu"));
            settingsWidget.UpdateLocalization();
            uiHelper.SetLocalizationFont(settingsWidget);
        }
    }
}