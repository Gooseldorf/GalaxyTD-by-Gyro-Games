using CardTD.Utilities;
using DG.Tweening;
using ECSTest.Components;
using Managers;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class GameUIManager : MonoBehaviour
    {
        [SerializeField] private UIDocument topPanel;
        [SerializeField] private UIDocument pauseWindow;
        [SerializeField] private UIDocument winWindow;
        [SerializeField] private UIDocument loseWindow;
        [SerializeField] private UIDocument cashWidget;
        [SerializeField] private UIDocument towerBuildPanel;
        [SerializeField] private UIDocument towerInfoPanel;
        [SerializeField] private UIDocument waveWidget;
        [SerializeField] private UIDocument livesWidget;
        [SerializeField] private UIDocument fpsCounterPanel;
        [SerializeField] private UIDocument cheatPanel;
        [SerializeField] private UIDocument dialogWindow;
        [SerializeField] private UIDocument raycastBlockerUI;
        [SerializeField] private UIDocument secondChanceUI;
        [SerializeField] private UIDocument notificationUI;
        [SerializeField] private UIDocument tutorialWindowUI;
        private List<UIDocument> elementsToResolve;
        private int toResolveCount = 0;
        
        [Space][SerializeField] private VisualTreeAsset buildTowerWidgetPrefab;
        [SerializeField] private VisualTreeAsset energyCorePrefab;
        [SerializeField] private VisualTreeAsset powerCellPrefab;
        [SerializeField] private TutorialManager tutorialManager;

        private TopPanel topPanelElement;
        private PauseWindow pauseWindowElement;
        private WinWindow winWindowElement;
        private LoseWindow loseWindowElement;
        private CashWidget cashWidgetElement;
        private TowerBuildPanel towerBuildPanelElement;
        private TowerInfoPanel towerInfoPanelElement;
        private WaveWidget waveWidgetElement;
        private LivesWidget livesWidgetElement;
        private FPSCounterPanel fpsCounterPanelElement;
        private CheatPanel cheatPanelElement;
        private DialogWindow dialogWindowElement;
        private VisualElement raycastBlocker;
        private Notification notification;
        private SecondChanceAnnouncement secondChanceAnnouncement;
        private TutorialWindow tutorialWindow;

        private List<int> cheatSequence = new ();
        private readonly int[] cheatCode = { 1, 2, 1, 2, 4, 2, 3, 1 };

        private void RegisterUIDocsToResolve()
        {
            elementsToResolve = GetComponentsInChildren<UIDocument>().ToList();
            toResolveCount = elementsToResolve.Count;
            foreach (var UIdoc in elementsToResolve)
                UIdoc.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnUIDocumentResolved);
        }

        private void OnUIDocumentResolved(GeometryChangedEvent geom)
        {
            VisualElement target = geom.currentTarget as VisualElement;    
            target?.UnregisterCallback<GeometryChangedEvent>(OnUIDocumentResolved);
            toResolveCount--;
            //elementsToResolve.Remove(target.);
            //Debug.Log(toResolveCount);
            if (toResolveCount <= 0)
            {
                LoadingScreen.Instance.UIDocumentsResolved = true;
                elementsToResolve.Clear();
            }
        }
        
        private void Start()
        {
            RegisterUIDocsToResolve();
            
            pauseWindowElement = pauseWindow.rootVisualElement.Q<PauseWindow>("PauseWindow");
            pauseWindowElement.Init();

            topPanelElement = topPanel.rootVisualElement.Q<TopPanel>("TopPanel");
            topPanelElement.Init(pauseWindowElement);
            topPanelElement.PauseButton.RegisterCallback<ClickEvent>(OnPauseCheatClick);
            topPanelElement.AccelerateButton.RegisterCallback<ClickEvent>(OnAccelerateCheatClick);

            livesWidgetElement = livesWidget.rootVisualElement.Q<LivesWidget>("LivesWidget");
            livesWidgetElement.Init(energyCorePrefab, powerCellPrefab);

            winWindowElement = winWindow.rootVisualElement.Q<WinWindow>("WinWindow");
            winWindowElement.Init(livesWidgetElement);

            loseWindowElement = loseWindow.rootVisualElement.Q<LoseWindow>("LoseWindow");
            loseWindowElement.Init();

            cashWidgetElement = cashWidget.rootVisualElement.Q<CashWidget>("CashWidget");
            cashWidgetElement.Init();
            cashWidgetElement.CheatButton.RegisterCallback<ClickEvent>(OnCashCheatClick);

            towerBuildPanelElement = towerBuildPanel.rootVisualElement.Q<TowerBuildPanel>("TowerBuildPanel");
            towerBuildPanelElement.Init(buildTowerWidgetPrefab);

            towerInfoPanelElement = towerInfoPanel.rootVisualElement.Q<TowerInfoPanel>("TowerInfoPanel");
            towerInfoPanelElement.Init();

            waveWidgetElement = waveWidget.rootVisualElement.Q<WaveWidget>("WaveWidget");
            waveWidgetElement.Init();
            waveWidgetElement.CheatButton.RegisterCallback<ClickEvent>(OnWaveCheatClick);

            raycastBlocker = raycastBlockerUI.rootVisualElement.Q<VisualElement>("RaycastBlocker");
            raycastBlocker.style.display = DisplayStyle.None;

            dialogWindowElement = dialogWindow.rootVisualElement.Q<DialogWindow>("DialogWindow");
            dialogWindowElement.Init();

            notification = notificationUI.rootVisualElement.Q<Notification>("Notification");
            notification.Init();

            secondChanceAnnouncement = secondChanceUI.rootVisualElement.Q<SecondChanceAnnouncement>();
            secondChanceAnnouncement.Init();
            loseWindowElement.ShowSecondChanceAnnouncement += secondChanceAnnouncement.Show;

            tutorialWindow = tutorialWindowUI.rootVisualElement.Q<TutorialWindow>();
            tutorialWindow.Init(transform);
            tutorialManager.Init(SceneEnum.GameScene, transform, GameServices.Instance.CurrentMission.MissionIndex, tutorialWindow);
            tutorialManager.Enable();

            fpsCounterPanelElement = fpsCounterPanel.rootVisualElement.Q<FPSCounterPanel>("FPSCounterPanel");
            cheatPanelElement = cheatPanel.rootVisualElement.Q<CheatPanel>("CheatPanel");
            fpsCounterPanelElement.Init(cheatPanelElement);
            cheatPanelElement.Init(fpsCounterPanelElement);

            UpdateLocalization();
            ShowDialogIfNeeded();

            Messenger<Entity>.AddListener(UIEvents.ObjectSelected, OnObjectSelected);
            Messenger.AddListener(GameEvents.Lost, ShowLose);
            Messenger<int, int>.AddListener(GameEvents.Win, ShowWin);
            Messenger.AddListener(GameEvents.Restart, ResetWidgets);
            Messenger.AddListener(GameEvents.Restart, ShowDialogIfNeeded);
            /*Messenger.AddListener(UIEvents.OnElementResolved, CompleteLoading);*/
            Messenger<float>.AddListener(UIEvents.OnUIAnimation, EnableRaycastBlocker);
            Messenger<Entity>.AddListener(GameEvents.TowerTeleported, CheckDropzoneAfterTeleportation);


#if UNITY_EDITOR
            AdsManager.InitServices();
#endif
        }

        /*private void CompleteLoading()
        {
            if (LoadingScreen.Instance.IsResolved)
                LoadingScreen.Instance.Hide();
            else
                LoadingScreen.Instance.LoadingScreenElement.RegisterCallback<GeometryChangedEvent>(LoadingScreen.Instance.HideOnResolve);
            Messenger.RemoveListener(UIEvents.OnElementResolved, CompleteLoading);
        }*/

        private void Update()
        {
            if (fpsCounterPanelElement != null)
                fpsCounterPanelElement.UpdateFPS();

            if (cashWidgetElement != null)
                cashWidgetElement.Update();
            //if selected dropzone is going to powerOff => broadcast for RangeVisualizator and this.OnObjectSelected()
            var manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (towerBuildPanelElement.dropZone != Entity.Null && manager.Exists(towerBuildPanelElement.dropZone) && towerBuildPanelElement.ClassListContains("Show") &&
                !manager.GetComponentData<PowerableComponent>(towerBuildPanelElement.dropZone).IsTurnedOn)
                Messenger<Entity>.Broadcast(UIEvents.ObjectSelected, Entity.Null, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void OnDestroy()
        {
            pauseWindowElement.Dispose();
            topPanelElement.Dispose();
            winWindowElement.Dispose();
            loseWindowElement.Dispose();
            cashWidgetElement.Dispose();
            towerBuildPanelElement.Dispose();
            towerInfoPanelElement.Dispose();
            livesWidgetElement.Dispose();
            dialogWindowElement.Dispose();
            waveWidgetElement.Dispose();
            notification.Dispose();
            tutorialManager.Dispose();
            tutorialWindow.Dispose();

            cheatPanelElement.Dispose();
            fpsCounterPanelElement.Dispose();

            loseWindowElement.ShowSecondChanceAnnouncement -= secondChanceAnnouncement.Show;

            Messenger<Entity>.RemoveListener(UIEvents.ObjectSelected, OnObjectSelected);
            Messenger.RemoveListener(GameEvents.Lost, ShowLose);
            Messenger<int, int>.RemoveListener(GameEvents.Win, ShowWin);
            Messenger.RemoveListener(GameEvents.Restart, ResetWidgets);
            Messenger.RemoveListener(GameEvents.Restart, ShowDialogIfNeeded);
            Messenger<float>.RemoveListener(UIEvents.OnUIAnimation, EnableRaycastBlocker);
            Messenger<Entity>.RemoveListener(GameEvents.TowerTeleported, CheckDropzoneAfterTeleportation);

            cashWidgetElement.CheatButton.UnregisterCallback<ClickEvent>(OnCashCheatClick);
            waveWidgetElement.CheatButton.UnregisterCallback<ClickEvent>(OnWaveCheatClick);
            topPanelElement.PauseButton.UnregisterCallback<ClickEvent>(OnPauseCheatClick);
            topPanelElement.AccelerateButton.UnregisterCallback<ClickEvent>(OnAccelerateCheatClick);
        }

        private void OnObjectSelected(Entity selected)
        {
            if (selected == Entity.Null)
            {
                towerBuildPanelElement.Hide();
                towerInfoPanelElement.Hide();
                return;
            }

            var manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (manager.HasComponent<DropZoneComponent>(selected))
            {
                ShowBuildPanel(selected);
                return;
            }

            if (manager.HasComponent<AttackerComponent>(selected))
                ShowInfoPanel(selected);
        }

        private void ShowDialogIfNeeded()
        {
            //            bool skipDialogPref = PlayerPrefs.GetInt(PrefKeys.SkipOldDialogs) == 1 && DataManager.Instance.GameData.LastDialog >= GameServices.Instance.CurrentMission.MissionIndex;

            //            bool skipDialog =
            //#if UNITY_EDITOR
            //                GameServices.Instance.SkipAllDialogs || skipDialogPref;
            //#else
            //                skipDialogPref;
            //#endif

            //            if (!skipDialog)
            //            {
            //                Dialog dialog = GameServices.Instance.Get<DialogHolder>().GetDialog(GameServices.Instance.CurrentMission.MissionIndex);

            //                if (dialog != null)
            //                    dialogWindowElement.Show(dialog, true, null);
            //            }
        }

        private void ShowInfoPanel(Entity tower)
        {
            towerInfoPanelElement.Show(tower);
            towerBuildPanelElement.Hide();
        }

        private void ShowBuildPanel(Entity dropZone)
        {
            towerBuildPanelElement.Show(dropZone);
            towerInfoPanelElement.Hide();
        }

        private void ShowWin(int stars, int maxPowerCells) => winWindowElement.Show(true, stars, maxPowerCells, () =>
        {
            towerBuildPanelElement.Hide();
            towerInfoPanelElement.Hide();
            GameServices.Instance.SetPause(true);
        });

        private void ShowLose()
        {
            loseWindowElement.Show(true, () =>
            {
                towerBuildPanelElement.Hide();
                towerInfoPanelElement.Hide();
                GameServices.Instance.SetPause(true);
            });
        }

        private void CheckDropzoneAfterTeleportation(Entity dropZone)
        {
            if (towerBuildPanelElement.dropZone.Equals(dropZone))
                towerBuildPanelElement.Hide();
        }

        private void ResetWidgets()
        {
            waveWidgetElement.ShowReset();
            topPanelElement.Reset();
            cashWidgetElement.Reset();
            livesWidgetElement.Reset();
            towerBuildPanelElement.Reset();
            loseWindowElement.Reset();
            winWindowElement.Reset();
        }

        private void UpdateLocalization()
        {
            loseWindowElement.UpdateLocalization();
            pauseWindowElement.UpdateLocalization();
            winWindowElement.UpdateLocalization();
            cashWidgetElement.UpdateLocalization();
            towerInfoPanelElement.UpdateLocalization();
            
            UIHelper.Instance.SetLocalizationFont(cashWidgetElement);
            UIHelper.Instance.SetLocalizationFont(loseWindowElement);
            UIHelper.Instance.SetLocalizationFont(pauseWindowElement);
            UIHelper.Instance.SetLocalizationFont(winWindowElement);
            UIHelper.Instance.SetLocalizationFont(tutorialWindow);
            UIHelper.Instance.SetLocalizationFont(towerInfoPanelElement);
        }
        #region CHEATS
        private void OnCashCheatClick(ClickEvent clk)
        {
            HandleClick(1);
        }

        private void OnWaveCheatClick(ClickEvent clk)
        {
            HandleClick(2);
        }

        private void OnAccelerateCheatClick(ClickEvent clk)
        {
            HandleClick(3);
        }

        private void OnPauseCheatClick(ClickEvent clk)
        {
            HandleClick(4);
        }

        private void EnableRaycastBlocker(float duration)
        {
            raycastBlocker.style.display = DisplayStyle.Flex;
            DOVirtual.DelayedCall(duration, () => raycastBlocker.style.display = DisplayStyle.None);
        }

        private void HandleClick(int clickValue)
        {
            cheatSequence.Add(clickValue);

            if (cheatSequence.Count > cheatCode.Length)
            {
                cheatSequence.Clear();
            }
            else
            {
                for (int i = 0; i < cheatSequence.Count; i++)
                {
                    if (cheatSequence[i] != cheatCode[i])
                    {
                        cheatSequence.Clear();
                        break;
                    }
                    else if (i == cheatSequence.Count - 1 && cheatSequence.Count == cheatCode.Length)
                    {
                        cheatPanelElement.Show(true);
                        GameServices.Instance.SetPause(true);
                        Debug.Log($"<color=blue><b>HESOYAM</b></color>");

                        cheatSequence.Clear();
                        break;
                    }
                }
            }
        }
        #endregion
#if UNITY_EDITOR
        [Button]
        private void ShowLivesWidgetPosition() => Debug.Log(livesWidgetElement.worldTransform.GetPosition());
        
        [Button]
        private void TranslateTextField(float2 position)
        {
            tutorialWindow.Q<DialogWindow>().SetTextAreaPosition(position);
        }

        [Button]
        private void SetMoneyToZero()
        {
            var cashComponent = GameServices.Instance.GetCashComponent();
            cashComponent.AddCash(-cashComponent.Cash);
        }
#endif
    }
}