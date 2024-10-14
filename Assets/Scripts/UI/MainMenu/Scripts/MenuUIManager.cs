using CardTD.Utilities;
using Data.Managers;
using DG.Tweening;
using I2.Loc;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI
{
    public class MenuUIManager : MonoBehaviour
    {
        [SerializeField] private UIDocument topPanelUI;
        [SerializeField] private UIDocument settingsButtonUI;
        [SerializeField] private UIDocument dailyRewardButtonUI;
        [SerializeField] private UIDocument menuSettingsWindowUI;
        [SerializeField] private UIDocument confirmWindowUI;
        [SerializeField] private UIDocument confirmWithAdButtonUI;
        [SerializeField] private UIDocument missionsPanelUI;
        [SerializeField] private UIDocument missionInfoWindowUI;
        [SerializeField] private UIDocument menuBottomPanelUI;
        [SerializeField] private UIDocument menuPanelBaseUI;
        [SerializeField] private UIDocument shopPanelUI;
        [SerializeField] private UIDocument directivesShopUI;
        [SerializeField] private UIDocument workshopPanelUI;
        [SerializeField] private UIDocument towerCustomizationPanelUI;
        [SerializeField] private UIDocument raycastBlockerUI;
        [SerializeField] private UIDocument rewardWindowUI;
        [SerializeField] private UIDocument newItemsWindowUI;
        [SerializeField] private UIDocument dailyRewardWindowUI;
        [SerializeField] private UIDocument dialogWindowUI;
        [SerializeField] private UIDocument backgroundUI;
        [SerializeField] private UIDocument tutorialWindowUI;
        private List<UIDocument> elementsToResolve;
        private int toResolveCount = 0;

        [Space] [SerializeField] private VisualTreeAsset partWidgetPrefab;
        [SerializeField] private VisualTreeAsset ammoPartWidgetPrefab;
        [SerializeField] private VisualTreeAsset missionsColumnPrefab;
        [SerializeField] private VisualTreeAsset missionWidgetPrefab;
        [SerializeField] private VisualTreeAsset shopDirectiveWidgetPrefab;
        [SerializeField] private VisualTreeAsset selectorDirectiveWidgetPrefab;
        [SerializeField] private VisualTreeAsset factoryWidgetPrefab;
        [SerializeField] private VisualTreeAsset waveLinePrefab;
        [SerializeField] private ScrollHelper scrollHelper;

        private ConfirmWindow confirmWindow;
        private ConfirmWindowWithAdButton confirmWithAd;
        private ShopPanel shopPanel;
        private MenuSettingsWindow menuSettingsWindow;
        private MenuTopPanel topPanel;
        private MissionInfoWindow missionInfoWindow;
        private MissionPanel missionsPanel;
        private MenuPanelBase menuPanelBase;
        private WorkshopPanel workshopPanel;
        private TowerCustomizationPanel towerCustomizationPanel;
        private DirectivesShop directivesShop;
        private CommonButton workshopButton;
        private CommonButton shopButton;
        private CommonButton directivesButton;
        private CommonButton settingsButton;
        private DailyRewardButton dailyRewardButton;
        private VisualElement raycastBlocker;
        private RewardWindow rewardWindow;
        private NewItemsWindow newItemsWindow;
        private DailyRewardWindow dailyRewardWindow;
        private HardModeButton hardModeButton;
        private VisualElement background;
        private TutorialWindow tutorialWindow;

        private UIHelper uiHelper;
        private DialogWindow dialogWindow;
        private List<IMenuPanel> menuPanels = new();

        public bool IsNoActiveWindows => menuPanels.Count <= 0;
        private Action<int> onPanelsUpdated;

        private void RegisterUIDocsToResolve(bool firstGame)
        {
            elementsToResolve = GetComponentsInChildren<UIDocument>().ToList();
            toResolveCount = firstGame ? 2 : elementsToResolve.Count;

            foreach (var UIdoc in elementsToResolve)
            {
                if (!firstGame || UIdoc == dialogWindowUI || UIdoc == raycastBlockerUI)
                {
                    UIdoc.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnUIDocumentResolved);
                }
                else
                {
                    UIdoc.gameObject.SetActive(false);
                }
                // .rootVisualElement.style.display = DisplayStyle.None;
            }
        }

        private void OnUIDocumentResolved(GeometryChangedEvent geom)
        {
            VisualElement target = geom.currentTarget as VisualElement;
            target?.UnregisterCallback<GeometryChangedEvent>(OnUIDocumentResolved);
            toResolveCount--;
            if (toResolveCount <= 0)
            {
                LoadingScreen.Instance.UIDocumentsResolved = true;
                elementsToResolve.Clear();
            }
        }

        private void Start()
        {
            GameServices.Instance.SetPause(false);
            GameServices.Instance.SetTimeScale(1);

            bool isFirstGame = DataManager.Instance.GameData.LastCompletedMissionIndex < 0;

            RegisterUIDocsToResolve(isFirstGame);

            GameServices.Instance.SaveSystemHandlesBackUp(); //TODO: Remove when we'll get rid of systemHandles
            GameServices.Instance.ToggleSystems(false);

            if (isFirstGame)
            {
                try
                {
                    FirstStartGame();
                }
                catch (Exception e)
                {
                    Debug.Log($"error firstMission manual in init {e}");
                }

                return;
            }

            uiHelper = UIHelper.Instance;
            Init();
        }

        private async void FirstStartGame()
        {
            dialogWindow = dialogWindowUI.rootVisualElement.Q<DialogWindow>("DialogWindow");
            dialogWindow.Init();
            await PlayBackgroundMusic();
            raycastBlocker = raycastBlockerUI.rootVisualElement.Q<VisualElement>("RaycastBlocker");
            raycastBlocker.style.display = DisplayStyle.None;

            dialogWindow.ShowDialog(0, true);
            while (dialogWindow.IsShowing)
                await Awaitable.NextFrameAsync();

            InGameCheats.MissionStartLoadTime = Time.realtimeSinceStartupAsDouble;
            DOTween.KillAll();

            GameServices.Instance.CurrentMission = DataManager.Instance.Get<MissionList>().GetMissionByIndex(0);
            GameServices.Instance.IsHard = false;
            LoadingScreen.Instance.Show(() => SceneManager.LoadScene(1, LoadSceneMode.Single));
        }

        private async Task PlayBackgroundMusic()
        {
            while (!MusicManager.IsReady)
            {
                await Awaitable.NextFrameAsync();
            }
            MusicManager.PlayMainMenuBackground();
        }

        private async void Init()
        {
            try
            {
                confirmWindow = confirmWindowUI.rootVisualElement.Q<ConfirmWindow>("ConfirmWindow");
                confirmWindow.Init();

                confirmWithAd = confirmWithAdButtonUI.rootVisualElement.Q<ConfirmWindowWithAdButton>("ConfirmWindow");
                confirmWithAd.Init();
                confirmWithAd.ShowShopAction = () => ShowShopPanel(AllEnums.CurrencyType.Hard);

                newItemsWindow = newItemsWindowUI.rootVisualElement.Q<NewItemsWindow>("NewItemsWindow");
                newItemsWindow.Init();

                shopButton = menuBottomPanelUI.rootVisualElement.Q<VisualElement>("ShopButton").Q<CommonButton>();
                shopButton.Init();
                shopButton.RegisterCallback<ClickEvent>(OnShopClick);
                shopPanel = shopPanelUI.rootVisualElement.Q<ShopPanel>("ShopPanel");
                shopPanel.Init(confirmWindow, newItemsWindow);
                UpdateShopButtonIsNewNotification(String.Empty);

                menuSettingsWindow = menuSettingsWindowUI.rootVisualElement.Q<MenuSettingsWindow>("MenuSettingsWindow");
                menuSettingsWindow.Init();

                topPanel = topPanelUI.rootVisualElement.Q<MenuTopPanel>("MenuTopPanel");
                topPanel.Init(confirmWindow);
                topPanel.OnCurrencyClick += ShowShopPanel;
                onPanelsUpdated += topPanel.OnPanelsUpdated;

                settingsButton = settingsButtonUI.rootVisualElement.Q<CommonButton>("SettingsButton");
                settingsButton.Init();
                settingsButton.RegisterCallback<ClickEvent>(OnSettingsClick);

                dailyRewardButton = dailyRewardButtonUI.rootVisualElement.Q<DailyRewardButton>("DailyRewardButton");
                dailyRewardButton.Init();
                dailyRewardButton.RegisterCallback<ClickEvent>(OnDailyRewardClick);

                dialogWindow = dialogWindowUI.rootVisualElement.Q<DialogWindow>("DialogWindow");
                dialogWindow.Init();

                missionInfoWindow = missionInfoWindowUI.rootVisualElement.Q<MissionInfoWindow>("MissionInfoWindow");
                missionInfoWindow.Init(waveLinePrefab, confirmWithAd, dialogWindow);

                hardModeButton = menuBottomPanelUI.rootVisualElement.Q<TemplateContainer>("HardModeButton").Q<HardModeButton>();
                if (DataManager.Instance.GameData.Stars.Count >= DataManager.Instance.HardModeMissionCountThreshold)
                    hardModeButton.Init();
                else
                    hardModeButton.style.display = DisplayStyle.None;

                missionsPanel = missionsPanelUI.rootVisualElement.Q<MissionPanel>("MissionsPanel");
                missionsPanel.Init(missionInfoWindow);
                background = backgroundUI.rootVisualElement.Q<VisualElement>("Background");
                scrollHelper.Init(missionsPanel, shopPanel, background.Q<VisualElement>("Planet"));

                directivesButton = menuBottomPanelUI.rootVisualElement.Q<VisualElement>("DirectivesButton").Q<CommonButton>();
                directivesButton.Init();
                directivesButton.RegisterCallback<ClickEvent>(OnDirectivesClick);
                UnlockManager unlocker = DataManager.Instance.Get<UnlockManager>();
                directivesButton.parent.style.display = DataManager.Instance.GameData.Stars.Count >= unlocker.DirectivesUnlockMission ? DisplayStyle.Flex : DisplayStyle.None;

                directivesShop = directivesShopUI.rootVisualElement.Q<DirectivesShop>("DirectivesShop");
                directivesShop.Init(shopDirectiveWidgetPrefab, confirmWindow, newItemsWindow);
                workshopButton = menuBottomPanelUI.rootVisualElement.Q<VisualElement>("WorkshopButton").Q<CommonButton>();
                workshopButton.Init();
                workshopButton.RegisterCallback<ClickEvent>(OnWorkshopClick);

                towerCustomizationPanel = towerCustomizationPanelUI.rootVisualElement.Q<TowerCustomizationPanel>("TowerCustomizationPanel");
                towerCustomizationPanel.Init(selectorDirectiveWidgetPrefab, partWidgetPrefab, ammoPartWidgetPrefab, shopPanel, confirmWindow);
                towerCustomizationPanel.OnDirectiveShopButton += ShowDirectivesShopPanel;

                workshopPanel = workshopPanelUI.rootVisualElement.Q<WorkshopPanel>("WorkshopPanel");

                workshopPanel.Init(towerCustomizationPanel, factoryWidgetPrefab);
                workshopPanel.OnFactoryWidgetClick += ShowTowerCustomizationPanel;

                menuPanelBase = menuPanelBaseUI.rootVisualElement.Q<MenuPanelBase>("MenuPanelBase");
                menuPanelBase.Init();
                menuPanelBase.CloseButton.RegisterCallback<ClickEvent>(OnCloseButtonClick);

                raycastBlocker = raycastBlockerUI.rootVisualElement.Q<VisualElement>("RaycastBlocker");
                raycastBlocker.style.display = DisplayStyle.None;

                rewardWindow = rewardWindowUI.rootVisualElement.Q<RewardWindow>("RewardWindow");
                rewardWindow.Init();


                dailyRewardWindow = dailyRewardWindowUI.rootVisualElement.Q<DailyRewardWindow>("DailyRewardWindow");
                dailyRewardWindow.Init(newItemsWindow, dailyRewardButton);

                tutorialWindow = tutorialWindowUI.rootVisualElement.Q<TutorialWindow>();
                tutorialWindow.Init(transform);
            }
            catch (Exception e)
            {
                Debug.Log($"error 1 in init {e}");
            }

            try
            {
                UpdateBottomPanelIsNewNotifications();
                UpdateLocalization();
            }
            catch (Exception e)
            {
                Debug.Log($"error 1 in init update: {e}");
            }

            try
            {
                Messenger<float>.AddListener(UIEvents.OnUIAnimation, EnableRaycastBlocker);
                Messenger.AddListener(UIEvents.LanguageChanged, UpdateLocalization);
                Messenger.AddListener(UIEvents.OnNewItemsUpdated, UpdateBottomPanelIsNewNotifications);
                Messenger<AllEnums.CurrencyType>.AddListener(UIEvents.GoToShop, ShowShopPanel);
                Messenger<string>.AddListener(UIEvents.PurchaseCompleted, UpdateShopButtonIsNewNotification);
                Messenger.AddListener(UIEvents.ModeChanged, ToggleMode);

                /*if (LoadingScreen.Instance.IsResolved)
                    LoadingScreen.Instance.Hide();
                else
                    LoadingScreen.Instance.LoadingScreenElement.RegisterCallback<GeometryChangedEvent>(LoadingScreen.Instance.HideOnResolve);*/
            }
            catch (Exception e)
            {
                Debug.Log($"e in finish : {e}");
            }

            await PlayBackgroundMusic();
            CheckPopupWindows();
            if (GameServices.Instance.IsHard) SetHardMode();
        }

        private async void CheckPopupWindows()
        {
            GameData gameData = DataManager.Instance.GameData;

            bool skipDialog =
#if UNITY_EDITOR
                GameServices.Instance.SkipAllDialogs ||
#endif
                (PlayerPrefs.GetInt(PrefKeys.SkipOldDialogs) == 1 && gameData.LastDialogAfter >= gameData.LastCompletedMissionIndex || GameServices.Instance.IsHard);

            TutorialManager.Instance.Init(SceneEnum.Menu, transform, DataManager.Instance.GameData.LastCompletedMissionIndex, tutorialWindow);

            if (!skipDialog && gameData.ShouldShowDialog)
            {
                dialogWindow.ShowDialog(gameData.LastCompletedMissionIndex, false);
                while (dialogWindow.IsShowing)
                    await Awaitable.NextFrameAsync();
            }

            bool hasRewards = gameData.Stars.ContainsKey(gameData.LastCompletedMissionIndex) && gameData.Stars[gameData.LastCompletedMissionIndex] == 3 &&
                              !gameData.SelectedRewards.ContainsKey(gameData.LastCompletedMissionIndex);

            //No rewards for choose
            if (hasRewards && false)
            {
                //rewardWindow.OnRewardSelected += missionsPanel.UpdateLastMissionWidget;
                rewardWindow.Show(DataManager.Instance.Get<MissionList>().GetMissionByIndex(gameData.LastCompletedMissionIndex));
                while (rewardWindow.IsShowing)
                    await Awaitable.NextFrameAsync();
            }

            bool hasUnlockedItems = gameData.HasNewUnlockedItems;
            if (hasUnlockedItems)
            {
                newItemsWindowUI.gameObject.SetActive(true);
                newItemsWindow.ShowNewItems();
                while (newItemsWindow.IsShowing)
                    await Awaitable.NextFrameAsync();
            }

            bool isRewardReadyToTake = false;
            MainMenuDaily.GetRewards().ForEach(r =>
            {
                if (r.StatusType == DailyRewardStatusType.ReadyToTake)
                    isRewardReadyToTake = true;
            });

            if (isRewardReadyToTake)
            {
                dailyRewardWindow.Show();
                while (dailyRewardWindow.IsShowing)
                    await Awaitable.NextFrameAsync();
            }

            while (TutorialManager.Instance.IsShowingTutorial)
                await Awaitable.NextFrameAsync();

            TutorialManager.Instance.Enable();
        }

        private void OnDailyRewardClick(ClickEvent evt)
        {
            dailyRewardButton.OnClick();
            dailyRewardWindow.Show();
        }

        private void OnDestroy()
        {
            if (DataManager.Instance.GameData.LastCompletedMissionIndex < 0)
            {
                dialogWindow.Dispose();
                return;
            }

            TutorialManager.Instance.Dispose();

            dailyRewardButton.Dispose();
            confirmWindow.Dispose();

            shopButton.Dispose();
            shopButton.UnregisterCallback<ClickEvent>(OnShopClick);
            shopPanel.Dispose();

            menuSettingsWindow.Dispose();

            topPanel.Dispose();
            onPanelsUpdated -= topPanel.OnPanelsUpdated;

            settingsButton.Dispose();
            settingsButton.UnregisterCallback<ClickEvent>(OnSettingsClick);

            missionInfoWindow.Dispose();
            missionsPanel.Dispose();

            directivesButton.Dispose();
            directivesButton.UnregisterCallback<ClickEvent>(OnDirectivesClick);
            directivesShop.Dispose();

            towerCustomizationPanel.OnDirectiveShopButton -= ShowDirectivesShopPanel;
            towerCustomizationPanel.Dispose();

            workshopButton.Dispose();
            workshopButton.UnregisterCallback<ClickEvent>(OnWorkshopClick);
            workshopPanel.Dispose();
            workshopPanel.OnFactoryWidgetClick -= ShowTowerCustomizationPanel;

            menuPanelBase.CloseButton.UnregisterCallback<ClickEvent>(OnCloseButtonClick);
            menuPanelBase.Dispose();

            rewardWindow.Dispose();
            //rewardWindow.OnRewardSelected -= missionsPanel.UpdateLastMissionWidget;

            dailyRewardWindow.Dispose();

            dailyRewardButton.UnregisterCallback<ClickEvent>(OnDailyRewardClick);

            dialogWindow.Dispose();

            Messenger<float>.RemoveListener(UIEvents.OnUIAnimation, EnableRaycastBlocker);
            Messenger.RemoveListener(UIEvents.LanguageChanged, UpdateLocalization);
            Messenger.RemoveListener(UIEvents.OnNewItemsUpdated, UpdateBottomPanelIsNewNotifications);
            Messenger<AllEnums.CurrencyType>.RemoveListener(UIEvents.GoToShop, ShowShopPanel);
            Messenger<string>.RemoveListener(UIEvents.PurchaseCompleted, UpdateShopButtonIsNewNotification);
            Messenger.RemoveListener(UIEvents.ModeChanged, ToggleMode);
        }

        private void OnCloseButtonClick(ClickEvent clk)
        {
            HideLastPanel();
        }

        private void ShowPanel(IMenuPanel panel)
        {
            Messenger<IMenuPanel>.Broadcast(GameEvents.ShowPanel,panel,MessengerMode.DONT_REQUIRE_LISTENER);

            Sequence seq = DOTween.Sequence();
            if (panel == workshopPanel)
            {
                workshopPanel.UpdateFactoryWidgets();
                workshopPanel.RestoreScrollOffset();
            }

            if (menuPanels.Count > 0)
            {
                if (panel == menuPanels[^1]) return;

                //Transition animation
                seq.Append(uiHelper.GetMenuPanelFadeTween((VisualElement)menuPanels[^1], false));
                seq.Append(menuPanelBase.GetTransitionTween(((VisualElement)panel).name));
                if (panel == towerCustomizationPanel) seq.Append(DOVirtual.DelayedCall(0, () => towerCustomizationPanel.SetInfo(towerCustomizationPanel.FactoryWidget.LastSelected)));
                else if (panel == directivesShop) seq.Append(DOVirtual.DelayedCall(0, () => directivesShop.UpdateInfo()));
                seq.Append(uiHelper.GetMenuPanelFadeTween((VisualElement)panel, true));
            }
            else
            {
                //Show animation
                seq.Append(menuPanelBase.GetShowTween(((VisualElement)panel).name));
                if (panel == directivesShop) seq.Append(DOVirtual.DelayedCall(0, () => directivesShop.UpdateInfo()));
                seq.Append(uiHelper.GetMenuPanelFadeTween((VisualElement)panel, true));
            }

            seq.SetDelay(0.1f);
            EnableRaycastBlocker(seq.Duration());
            seq.Play();

            menuPanels.Add(panel);
            onPanelsUpdated.Invoke(menuPanels.Count);
            missionsPanel.ShowScrollButtons(false);
        }

        private void ShowTowerCustomizationPanel() => ShowPanel(towerCustomizationPanel);

        private void ShowDirectivesShopPanel() => ShowPanel(directivesShop);

        private void ShowShopPanel(AllEnums.CurrencyType type)
        {
            if (shopPanel.style.display == DisplayStyle.Flex)
            {
                shopPanel.SetCurrencyType(type);
            }
            else
            {
                shopPanel.SetCurrencyType(type);
                ShowPanel(shopPanel);
            }
        }

        private void HideLastPanel()
        {
            if (menuPanels.Count <= 0) return;

            Messenger<IMenuPanel>.Broadcast(GameEvents.HidePanel,menuPanels[^1],MessengerMode.DONT_REQUIRE_LISTENER);

            Sequence seq = DOTween.Sequence();

            if (menuPanels[^1] == workshopPanel)
                workshopPanel.SaveScrollOffset();

            if (menuPanels.Count == 1)
            {
                //Hide animation
                seq.Append(uiHelper.GetMenuPanelFadeTween((VisualElement)menuPanels[0], false));
                seq.Append(menuPanelBase.GetHideTween());
                missionsPanel.ShowScrollButtons(true);
            }
            else
            {
                //Transition animation
                seq.Append(uiHelper.GetMenuPanelFadeTween((VisualElement)menuPanels[^1], false));
                seq.Append(menuPanelBase.GetTransitionTween(((VisualElement)menuPanels[^2]).name));
                if (menuPanels[^2] == towerCustomizationPanel) seq.Append(DOVirtual.DelayedCall(0, () => towerCustomizationPanel.SetInfo(towerCustomizationPanel.FactoryWidget.LastSelected)));
                else if (menuPanels[^2] == directivesShop) seq.Append(DOVirtual.DelayedCall(0, () => directivesShop.UpdateInfo()));
                seq.Append(uiHelper.GetMenuPanelFadeTween((VisualElement)menuPanels[^2], true));

                if (menuPanels[^2] is WorkshopPanel panel)
                {
                    panel.UpdateFactoryWidgets();
                }
            }

            seq.OnComplete((() =>
            {
                menuPanels.RemoveAt(menuPanels.Count - 1);
                onPanelsUpdated.Invoke(menuPanels.Count);
                // if (menuPanels.Count <= 0)
                //     EnableAdButton(true);
            }));
            seq.SetDelay(0.1f);

            EnableRaycastBlocker(seq.Duration());
            seq.Play();
        }

        public void EnableRaycastBlocker(float duration)
        {
            raycastBlocker.style.display = DisplayStyle.Flex;
            DOVirtual.DelayedCall(duration, () => raycastBlocker.style.display = DisplayStyle.None);
        }

        private void OnShopClick(ClickEvent clk)
        {
            ShowPanel(shopPanel);
        }

        private void OnWorkshopClick(ClickEvent clk)
        {
            ShowPanel(workshopPanel);
        }

        private void OnDirectivesClick(ClickEvent clk)
        {
            ShowPanel(directivesShop);
        }

        private void UpdateBottomPanelIsNewNotifications()
        {
            GameData gameData = DataManager.Instance.GameData;
            bool newItemsInWorkshop = gameData.NewFactories.Count > 0 || gameData.NewItems.Exists(x =>
                (x.PartType & (AllEnums.PartType.Barrel | AllEnums.PartType.Magazine | AllEnums.PartType.RecoilSystem | AllEnums.PartType.TargetingSystem)) != 0);
            bool newItemsInDirectivesShop = gameData.NewItems.Exists(x => x.PartType == AllEnums.PartType.Directive);

            workshopButton.parent.Q<VisualElement>("IsNewNotification").style.display = newItemsInWorkshop ? DisplayStyle.Flex : DisplayStyle.None;
            directivesButton.parent.Q<VisualElement>("IsNewNotification").style.display = newItemsInDirectivesShop ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateShopButtonIsNewNotification(string productId)
        {
            shopButton.parent.Q<VisualElement>("IsNewNotification").style.display = IAPHelper.Instance.HasOncePerDayItems() ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnSettingsClick(ClickEvent clk)
        {
            menuSettingsWindow.Show();
        }

        private void ToggleMode()
        {
            Debug.Log("HardMode is not implemented after redesign");

            /*if (background.ClassListContains("Normal"))
                background.RemoveFromClassList("Normal");
            else
                background.AddToClassList("Normal");*/
        }

        private void SetHardMode()
        {
            Debug.Log("HardMode is not implemented after redesign");
            /*hardModeButton.SetHardMode();
            background.RemoveFromClassList("Normal");
            missionsPanel.SetHardMode();*/
        }

        private void UpdateLocalization()
        {
            shopButton.SetText(LocalizationManager.GetTranslation("Menu/ShopButton"));
            directivesButton.SetText(LocalizationManager.GetTranslation("Menu/DirectivesButton"));
            workshopButton.SetText(LocalizationManager.GetTranslation("Menu/WorkshopButton"));
            
            confirmWindow.UpdateLocalization();
            shopPanel.UpdateLocalization();
            menuSettingsWindow.UpdateLocalization();
            missionInfoWindow.UpdateLocalization();
            missionsPanel.UpdateLocalization();
            directivesShop.UpdateLocalization();
            workshopPanel.UpdateLocalization();
            dailyRewardWindow.UpdateLocalization();
            rewardWindow.UpdateLocalization();
            
            uiHelper.SetLocalizationFont(confirmWindow);
            uiHelper.SetLocalizationFont(shopPanel);
            uiHelper.SetLocalizationFont(menuSettingsWindow);
            uiHelper.SetLocalizationFont(missionInfoWindow);
            uiHelper.SetLocalizationFont(missionsPanel);
            uiHelper.SetLocalizationFont(directivesShop);
            uiHelper.SetLocalizationFont(workshopPanel);
            uiHelper.SetLocalizationFont(towerCustomizationPanel);
            uiHelper.SetLocalizationFont(dailyRewardWindow);
            uiHelper.SetLocalizationFont(dailyRewardButton);
            uiHelper.SetLocalizationFont(dialogWindow);
            uiHelper.SetLocalizationFont(tutorialWindow);
            uiHelper.SetLocalizationFont(rewardWindow);
            uiHelper.SetLocalizationFont(newItemsWindow);
        }
#if UNITY_EDITOR
        //[Button]
        private void Test()
        {
            Messenger.Broadcast(UIEvents.Test, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        [Button]
        private void ShowTutorial(string key)
        {
            TutorialManager.Instance.ShowIsolatedTutorial(key);
        }

        [Button]
        private void TranslateTextField(float2 position)
        {
            tutorialWindow.Q<DialogWindow>().SetTextAreaPosition(position);
        }

        [Button]
        private void TestTypewriterSound() => MusicManager.PlayTypewriterSound();

        [Button]
        private void StopTypewriterSound() => MusicManager.StopTypewriterSound();
#endif
    }
}