using CardTD.Utilities;
using DG.Tweening;
using ECSTest.Components;
using ECSTest.Structs;
using I2.Loc;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class TutorialWindow : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TutorialWindow> { }

        private VisualElement windowContainer;
        private ConfirmWindow confirmWindow;
        
        private VisualElement dialogContainer;
        
        private DialogWindow dialogWindow;
        private UIHighlighter highlighter;
        
        private VisualElement highlightedUI;
        private VisualElement environmentPointer;
        private VisualElement envOuter;
        private VisualElement envInner;
        private VisualElement envInner2;
        private Label envPointerLabel;
        
        private GameObject highlightedEnvironment;
        
        private Tutorial currentTutorial;
        private int currentStageIndex;
        private int dialogLineOffset;
        //private float environmentSizeMultiplier = 0;

        private Transform sceneUIManager;
        
        private EntityManager entityManager;
        private EntityQuery envQuery;
        private Camera mainCamera;

        private bool resolved = false;
        public event Action TutorialEndEvent;
        
        public void Init(Transform sceneUIManager)
        {
            this.sceneUIManager = sceneUIManager;

            windowContainer = this.Q<VisualElement>("WindowContainer");
            confirmWindow = windowContainer.Q<ConfirmWindow>();
            confirmWindow.Init();
            
            dialogContainer = this.Q<VisualElement>("DialogContainer");
            dialogWindow = dialogContainer.Q<DialogWindow>();
            dialogWindow.Init();
            environmentPointer = dialogContainer.Q<VisualElement>("EnvPointer");
            envOuter = environmentPointer.Q<VisualElement>("Outer");
            envInner = environmentPointer.Q<VisualElement>("Inner1");
            envInner2 = environmentPointer.Q<VisualElement>("Inner2");
            envPointerLabel = environmentPointer.Q<Label>();
            /*if (TouchCamera.Instance != null)
            {
                mainCamera = TouchCamera.Instance.MainCamera;
                PanelSettings settings = UIHelper.Instance.UIToolkitPanelSettings;
                float referenceScreenMetric = settings.match == 0 ? settings.referenceResolution.x : settings.referenceResolution.y;
                environmentSizeMultiplier = settings.match == 0 ? referenceScreenMetric / Screen.width : referenceScreenMetric /Screen.height;
            }*/
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            highlighter = this.Q<UIHighlighter>();
            highlighter.Init();

            dialogWindow.OnDelayEvent += OnDelay;
            currentTutorial = null;
            resolved = false;
        }

        public void Dispose()
        {
            dialogWindow.OnDelayEvent -= OnDelay;
            dialogWindow.Dispose();
            confirmWindow.Dispose();
            
            this.UnregisterCallback<GeometryChangedEvent>(OnResolve);
        }

        public void ShowTutorial(Tutorial tutorial, int dialogLineOffset = 0)
        {
            currentTutorial = tutorial;
            TutorialManager.Instance.IsShowingTutorial = true;
            this.dialogLineOffset = dialogLineOffset;
            this.style.display = DisplayStyle.Flex;
            if (!resolved)
            {
                // this.style.visibility = Visibility.Hidden;
                this.RegisterCallback<GeometryChangedEvent>(OnResolve);
                return;
            }
            
            if (TouchCamera.Instance != null)
                TouchCamera.Instance.CanDrag = false;
            if (tutorial.Scene == SceneEnum.GameScene)
                GameServices.Instance.SetPause(true);
            if (tutorial.TutorialType == TutorialType.Dialog)
            {
                dialogContainer.style.display = DisplayStyle.Flex;
                windowContainer.style.display = DisplayStyle.None;
                currentStageIndex = 0;
                dialogWindow.OnNextLineShow += ShowNextStage;
                dialogWindow.OnEndDialog += EndTutorial;
                ShowDialog(dialogLineOffset);
                this.dialogLineOffset = 0;
                //UIHelper.Instance.FadeTween(UIPointer, 0.2f, 1f, 1).SetUpdate(true).SetTarget(UIPointer).SetLoops(-1, LoopType.Yoyo).Pause();
            }
            else if (tutorial.TutorialType == TutorialType.Window)
            {
                dialogContainer.style.display = DisplayStyle.None;
                windowContainer.style.display = DisplayStyle.Flex;
                ShowWindow();
            }
            TutorialManager.Instance.SetTutorialCompleted(currentTutorial.Key);
        }

        private void OnResolve(GeometryChangedEvent geom)
        {
            // this.style.visibility = Visibility.Visible;
            this.UnregisterCallback<GeometryChangedEvent>(OnResolve);
            if(float.IsNaN(this.resolvedStyle.width) || this.resolvedStyle.width != 0)
                resolved = true;
            
            ShowTutorial(currentTutorial, dialogLineOffset);
        }

        private void ShowNextStage()
        {
            TutorialStage stage = currentTutorial.Stages[currentStageIndex];
            CheckAdditionalActions();
            
            Reset();
            if (stage.StageEvents.HasFlag(TutorialStageEvents.EnvironmentSelection))
            {
                if (stage.StageEvents.HasFlag(TutorialStageEvents.AdditionalUISelection))
                    SetUpAdditionalUIHighlight(stage);
                
                ShowEnvironmentSelection(stage);
            }
            else if (stage.StageEvents.HasFlag(TutorialStageEvents.UISelection))
            {
                if (stage.StageEvents.HasFlag(TutorialStageEvents.AdditionalUISelection))
                    SetUpAdditionalUIHighlight(stage);
                
                ShowUISelection(stage);
            }


            if (stage.StageEvents.HasFlag(TutorialStageEvents.Delay))
            {
                dialogWindow.Delay = stage.NextStageDelay;
            }

            currentStageIndex++;
        }

        private void CheckAdditionalActions()
        {
            if (currentTutorial.Key == TutorialKeys.MenuUpgrades)
            {
                if (currentStageIndex == 7)
                {
                    sceneUIManager.Find("TowerCustomizationPanel").gameObject.SetActive(false);
                    sceneUIManager.Find("MenuPanelBase").gameObject.SetActive(false);
                }
            }
        }

        private void Reset()
        {
            //ShowEnvPointer(false);
            dialogWindow.RestoreClickAreaSize();
            dialogWindow.SetTextAreaPosition(float2.zero);
            highlighter.Reset();
            if(highlightedUI != null) highlightedUI.UnregisterCallback<ClickEvent>(dialogWindow.OnClick);
        }
        
        private void EndTutorial()
        {
            TutorialManager.Instance.IsShowingTutorial = false;
            //TutorialManager.Instance.SetTutorialCompleted(currentTutorial.Key);
            Reset();
            if (TouchCamera.Instance != null)
                TouchCamera.Instance.CanDrag = true;
            if (currentTutorial.Scene == SceneEnum.GameScene)
                GameServices.Instance.SetPause(false);
            dialogWindow.OnNextLineShow -= ShowNextStage;
            dialogWindow.OnEndDialog -= EndTutorial;
            this.style.display = DisplayStyle.None;
            
            TutorialEndEvent?.Invoke();
        }

        private void ShowWindow()
        {
            confirmWindow.SetUp(currentTutorial.Icon, LocalizationManager.GetTranslation($"SoftLaunch/{currentTutorial.Key}"), () => //TODO: "Tutorials/" replaced with "SoftLaunch/"
            {
                confirmWindow.Hide();
                EndTutorial();
            }, 2.5f);
            confirmWindow.Show();
        }
        
        private void ShowDialog(int dialogLineOffset = 0)
        {
            List<DialogLine> lines = new();
            foreach (TutorialStage stage in currentTutorial.Stages)
            {
                if (stage.StageEvents.HasFlag(TutorialStageEvents.Dialog))
                {
                    lines.Add(stage.DialogLine);
                }
            }

            dialogWindow.ShowDialog(lines, $"SoftLaunch/{currentTutorial.Key}_", false, dialogLineOffset, true); //TODO: "Tutorials/" replaced with "SoftLaunch/"
        }

        private void ShowEnvironmentSelection(TutorialStage stage)
        {
            GridPositionStruct gridPositionStruct = new ();
            if (!stage.IgnoreEnvironment)
            {
                highlightedEnvironment = GetEnvironmentVisualByGridPosition(stage.EnvGridPosition, out  gridPositionStruct);
            }
            if (TouchCamera.Instance != null)
                TouchCamera.Instance.CanDrag = false;
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, 0.5f, MessengerMode.DONT_REQUIRE_LISTENER);
            TouchCamera.Instance.MoveToPosition(new float2(stage.EnvGridPosition)).OnComplete(() =>
            {
                Vector2 sizeInPixels = new (54 * gridPositionStruct.GridSize.x, 54 * gridPositionStruct.GridSize.y);
                if (stage.StageEvents.HasFlag(TutorialStageEvents.Action))
                {
                    dialogWindow.SetClickAreaSize(this.layout.center - new Vector2(0, sizeInPixels.y), sizeInPixels, true);
                    if(!stage.IgnoreEnvironment) dialogWindow.OnClickEvent += SelectEnvironment;
                    
                }

                sizeInPixels = stage.IgnoreEnvironment ? new float2(108, 108) : sizeInPixels;
                highlighter.ShowMainHighlighter(this.LocalToWorld(this.layout.center), sizeInPixels);
                
                /*if (stage.ShowEnvPointer)
                {
                    envPointerLabel.text = LocalizationManager.GetTranslation($"Tutorials/{stage.EnvPointerTextKey}");
                    //ShowEnvPointer(true);
                }*/
            });
        }
        
        private void ShowEnvPointer(bool show)
        {
            if (!show)
            {
                DOTween.Kill(environmentPointer);
                environmentPointer.style.visibility = Visibility.Hidden;
                return;
            }

            environmentPointer.style.visibility = Visibility.Visible;
            Sequence seq = DOTween.Sequence();
            seq.Append(DOTween.To(() => envOuter.transform.rotation.eulerAngles, x => envOuter.transform.rotation = Quaternion.Euler(x), new Vector3(0, 0, 360), 3).SetEase(Ease.Linear));
            seq.Insert(0,DOTween.To(() => envInner.transform.rotation.eulerAngles, x => envInner.transform.rotation = Quaternion.Euler(x), new Vector3(0f, 0f, 360f), 3).SetEase(Ease.Linear));
            seq.Insert(0, DOTween.To(() => envInner2.transform.rotation.eulerAngles, x => envInner2.transform.rotation = Quaternion.Euler(x), new Vector3(0f, 0f, -360f), 3).SetEase(Ease.Linear));
            seq.SetLoops(-1).SetUpdate(true).SetTarget(environmentPointer);
            seq.Play();
        }

        private void SelectEnvironment()
        {
            Entity selectedEntity = GameServices.Instance.Select(new float3(highlightedEnvironment.transform.position.x, highlightedEnvironment.transform.position.y, -1));
            Messenger<Entity>.Broadcast(UIEvents.ObjectSelected, selectedEntity, MessengerMode.DONT_REQUIRE_LISTENER);

            dialogWindow.OnClickEvent -= SelectEnvironment;
        }

        private void ShowUISelection(TutorialStage stage)
        {
            UIDocument targetParent = sceneUIManager.Find(stage.UISelectionParentName).GetComponent<UIDocument>();

            highlightedUI = targetParent.rootVisualElement.Q<VisualElement>(stage.UISelectionElementName);
            float2 position = new float2(highlightedUI.worldBound.position.x, highlighter.layout.height - highlightedUI.worldBound.position.y - highlightedUI.resolvedStyle.height);
            float2 size = new float2(highlightedUI.resolvedStyle.width, highlightedUI.resolvedStyle.height);
            
            highlighter.ShowMainHighlighter(position, size);
            highlightedUI.RegisterCallback<ClickEvent>(dialogWindow.OnClick);
            
            if(stage.StageEvents.HasFlag(TutorialStageEvents.Action)) dialogWindow.SetClickAreaSize(0,0,true);
            else dialogWindow.RestoreClickAreaSize();
            
            dialogWindow.SetTextAreaPosition(stage.TextFieldOffset);
        }

        private void SetUpAdditionalUIHighlight(TutorialStage stage)
        {
            UIDocument targetParent = sceneUIManager.Find(stage.AdditionalUIHighlightParentName).GetComponent<UIDocument>();

            VisualElement additionalHighlightedUI = targetParent.rootVisualElement.Q<VisualElement>(stage.AdditionalUIHighlightElementName);
            float2 position = new float2(additionalHighlightedUI.worldBound.position.x, highlighter.layout.height - additionalHighlightedUI.worldBound.position.y - additionalHighlightedUI.resolvedStyle.height);
            float2 size = new float2(additionalHighlightedUI.resolvedStyle.width, additionalHighlightedUI.resolvedStyle.height);
            
            highlighter.SetUpAdditionalHighlighter(position, size);
        }
        
        private void OnDelay(float delay)
        {
            this.style.visibility = Visibility.Hidden;
            highlighter.Reset();
            DOVirtual.DelayedCall(delay, () => this.style.visibility = Visibility.Visible);
        }

        private GameObject GetEnvironmentVisualByGridPosition(int2 position, out GridPositionStruct gripPos)
        {
            GameObject visual = null;
            
            envQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<EnvironmentVisualComponent>().WithAll<GridPositionComponent>().Build(entityManager);
            NativeArray<Entity> entityArray = envQuery.ToEntityArray(Allocator.Temp);
            NativeArray<GridPositionComponent> positions = envQuery.ToComponentDataArray<GridPositionComponent>(Allocator.Temp);
            gripPos = new();
            for (int i = 0; i < positions.Length; i++)
            {
                if (position.x == positions[i].Value.GridPos.x && position.y == positions[i].Value.GridPos.y)
                {
                    var visualComponent = entityManager.GetComponentData<EnvironmentVisualComponent>(entityArray[i]);
                    if(visualComponent.EnvironmentVisual != null)
                        visual = entityManager.GetComponentData<EnvironmentVisualComponent>(entityArray[i]).EnvironmentVisual.gameObject;
                    gripPos = positions[i].Value;
                    break;
                }
            }

            return visual;
        }
    }
}
