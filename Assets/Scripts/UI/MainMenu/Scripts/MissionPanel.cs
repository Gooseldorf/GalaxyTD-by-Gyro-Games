using CardTD.Utilities;
using DG.Tweening;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class MissionPanel : Selector
    {
        public new class UxmlFactory : UxmlFactory<MissionPanel>{}

        private ScrollView scroll;
        
        //Widgets:
        private UQueryBuilder<TemplateContainer> widgetTemplatesQueryBuilder;
        private List<MissionWidget> widgets;
        private List<float2> widgetPositions;
        private float2 currentWidgetPosition;
        
        /*//Pointers:
        private UQueryBuilder<VisualElement> pointersQueryBuilder;
        private List<float2> pointerPositions;
        private float2 currentPointerPosition;*/
        
        //Lines:
        private UQueryBuilder<VisualElement> linesQueryBuilder;
        
        //Cross-pointer:
        /*private VisualElement crossPointer;
        private VisualElement crossPointerTop;
        private VisualElement crossPointerBottom;
        private VisualElement crossPointerLeft;*/

        //Side buttons:
        private ClickableVisualElement leftSideButton;
        private ClickableVisualElement rightSideButton;
        
        //Fields:
        private int currentMissionIndex;
        private float2 screenCenter;
        private bool isHardMode = false;
        
        //Data:
        private List<Mission> missionsData;
        private List<Mission> missionsHardData;
        private IReadOnlyDictionary<int, int> stars;
        private IReadOnlyDictionary<int, int> hardStars;

        //Dependencies:
        private UIHelper uiHelper;
        private DataManager dataManager;
        private MissionInfoWindow missionInfoWindow;
        
        public void Init(MissionInfoWindow missionInfoWindow)
        {
            this.missionInfoWindow = missionInfoWindow;
            scroll = this.Q<ScrollView>("MissionsScroll");
            CashData();
            InitWidgets();
            UpdateCurrentMissionIndex();
            InitSideButtons();
            InitLines();
            
            //There are some logic that depends on resolved style, so continue initialization after element resolved
            this.RegisterCallback<GeometryChangedEvent>(OnResolve); 
        }

        public void Dispose()
        {
            foreach (MissionWidget widget in widgets)
            {
                widget.UnregisterCallback<ClickEvent>(OnWidgetClick);
                widget.OnWidgetClick -= ShowMissionInfoWindow;
                widget.Dispose();
            };
            
            leftSideButton.UnregisterCallback<ClickEvent>(OnScrollArrowClick);
            rightSideButton.UnregisterCallback<ClickEvent>(OnScrollArrowClick);
        }

        public void SnapToClosest()
        {
            Vector2 closestPoint = GetClosestInXYCoordinates(widgetPositions, scroll.scrollOffset, out currentMissionIndex);
            MoveScrollToCurrentIndex();
        }

        public void SelectClosestMissionWidget()
        {
            float2 target = scroll.scrollOffset;
            GetClosestInXYCoordinates(widgetPositions, target, out int closestIndex);
            if(currentMissionIndex == closestIndex) return;
            currentMissionIndex = closestIndex;
            Select(widgets[closestIndex]);
        }

        protected override void Select(SelectableElement selectable)
        {
            if(selectable == LastSelected) return;
                base.Select(selectable);
        }

        public void UpdateLocalization()
        {
            if(widgets.IsNullOrEmpty()) return;
            
            foreach (MissionWidget missionWidget in widgets)
                missionWidget.UpdateLocalization();
        }

        private void CashData()
        {
            uiHelper = UIHelper.Instance;
            dataManager = DataManager.Instance;
            
            missionsData = (List<Mission>)dataManager.Get<MissionList>().Missions;
            missionsHardData = (List<Mission>)dataManager.Get<MissionList>().MissionsHard; 
            stars = dataManager.GameData.Stars;
            hardStars = dataManager.GameData.HardStars;
        }

        private void InitWidgets()
        {
            widgetTemplatesQueryBuilder = this.Q<VisualElement>("MissionsGroup").Query<TemplateContainer>();
            UQueryState<TemplateContainer> widgetTemplatesQuery = widgetTemplatesQueryBuilder.Build();
            
            widgets = new();
            int i = 0;
            widgetTemplatesQuery.ForEach((widgetTemplate) =>
            {
                MissionWidget widget = widgetTemplate.Q<MissionWidget>();
                widget.Init();
                widget.SetMission(missionsData[i], missionsHardData[i]);
                widget.name = $"MissionWidget_{missionsData[i].MissionIndex}";
                if (i <= stars.Count)
                {
                    widget.SetState(i < stars.Count ? AllEnums.UIState.Available : AllEnums.UIState.Active);
                    
                    widget.RegisterCallback<ClickEvent>(OnWidgetClick);
                    widget.OnWidgetClick += ShowMissionInfoWindow;
                    
                    /*if (i > hardStars.Count)
                        widget.SetHardState(AllEnums.UIState.Locked);
                    else
                        widget.SetHardState(i < hardStars.Count ? AllEnums.UIState.Available : AllEnums.UIState.Active);*/
                }
                else
                {
                    widget.SetState(AllEnums.UIState.Locked);
                    //widget.SetHardState(AllEnums.UIState.Locked);
                    widget.AddToClassList("noAnimation");
                }
                
                widget.UpdateLocalization();
                widgets.Add(widget);
                i++;
            });
        }
        
        private void UpdateCurrentMissionIndex()
        {
            currentMissionIndex = dataManager.GameData.LastCompletedMissionIndex;
            
            if (!stars.ContainsKey(currentMissionIndex + 1) && currentMissionIndex + 1 < widgets.Count)
                currentMissionIndex++;
            
            if (currentMissionIndex >= widgets.Count)
                currentMissionIndex = widgets.Count - 1;
        }

        private void InitLines()
        {
            linesQueryBuilder = this.Q<VisualElement>("MissionsGroup").Query<VisualElement>("Line");
            int i = 0;
            bool currentSetFlag = false; //flag for currentMission
            linesQueryBuilder.Build()
                .ForEach((line) =>
                {
                    if (!stars.ContainsKey(i + 1))
                    {
                        if (!currentSetFlag)
                        {
                            line.style.backgroundImage = new StyleBackground(uiHelper.ActiveMissionLine);
                            currentSetFlag = true;
                        }
                        else
                            line.style.opacity = 0.3f;
                    }
                    else
                    {
                        line.style.backgroundImage = new StyleBackground(uiHelper.PassedMissionLine);
                    }
                    i++;
                });
        }

        private void InitSideButtons()
        {
            leftSideButton = this.Q<ClickableVisualElement>("LeftScrollButton");
            leftSideButton.Init();
            leftSideButton.SoundName = String.Empty;
            leftSideButton.RegisterCallback<ClickEvent>(OnScrollArrowClick);

            rightSideButton = this.Q<ClickableVisualElement>("RightScrollButton");
            rightSideButton.Init();
            rightSideButton.SoundName = String.Empty;
            rightSideButton.RegisterCallback<ClickEvent>(OnScrollArrowClick);
        }

        private void OnResolve(GeometryChangedEvent geom)
        {
            this.UnregisterCallback<GeometryChangedEvent>(OnResolve);
            screenCenter = new float2(this.worldBound.center.x, this.worldBound.center.y);

            widgetPositions = GetMissionWidgetPositionsList();
            
            MoveScrollToCurrentIndex();
        }

        private List<float2> GetMissionWidgetPositionsList()
        {
            UQueryState<TemplateContainer> widgetTemplatesQuery = widgetTemplatesQueryBuilder.Build();
            List<float2> result = new();
            float x, y;

            widgetTemplatesQuery.ForEach((widget) =>
            {
                if (widget.Q<MissionWidget>().State != AllEnums.UIState.Locked)
                {
                    x = widget.worldBound.center.x - screenCenter.x;
                    y = widget.worldBound.center.y - screenCenter.y;
                    result.Add(new float2(x, y));
                }
            });

            return result;
        }
        
        private void OnWidgetClick(ClickEvent clk)
        {
            MissionWidget target = (MissionWidget)clk.currentTarget;
            currentMissionIndex = widgets.IndexOf(target);
            
            MoveScrollToCurrentIndex();
        }

        private void OnScrollArrowClick(ClickEvent clk)
        {
            VisualElement target = (VisualElement)clk.currentTarget;

            if (target.name == rightSideButton.name)
            {
                currentMissionIndex++;
                if (currentMissionIndex >= widgetPositions.Count)
                    currentMissionIndex = 0;
            }
            else if (target.name == leftSideButton.name)
            {
                currentMissionIndex--;
                if (currentMissionIndex < 0)
                    currentMissionIndex = widgetPositions.Count - 1;
            }

            MoveScrollToCurrentIndex();
        }

        private void ShowMissionInfoWindow(Mission mission, bool isHard) => DOVirtual.DelayedCall(0.3f, () => missionInfoWindow.Show(mission, isHard));

        public void ShowScrollButtons(bool show)
        {
            leftSideButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            rightSideButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void MoveScrollToCurrentIndex()
        {
            Select(widgets[currentMissionIndex]);
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, 0.4f);
            Vector2 currentScrollOffset = scroll.scrollOffset;
            DOTween.To(() => currentScrollOffset, x => currentScrollOffset = x, widgetPositions[currentMissionIndex], 0.4f)
                .OnUpdate(() => scroll.scrollOffset = currentScrollOffset)
                .SetTarget(scroll).Play();
        }

        private float2 GetClosestInXYCoordinates(List<float2> array, float2 position, out int closestIndex)
        {
            float2 closest = array[0];
            closestIndex = 0;
            float smallestDistance = Vector2.Distance(position, array[0]);

            for (int i = 1; i < array.Count; i++)
            {
                float distance = Vector2.Distance(position, array[i]);

                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    closest = array[i];
                    closestIndex = i;
                }
            }

            return closest;
        }

        /*private void ToggleMode()
        {
            isHardMode = !isHardMode;
            SnapToActiveIfNeeded(out float duration);
            DOVirtual.DelayedCall(duration, ChangeWidgetsMode);
        }

        private void SnapToActiveIfNeeded(out float duration)
        {
            duration = 0;
            if (isHardMode)
            {
                MissionWidget activeWidget = widgets.Find(x => x.HardState == AllEnums.UIState.Active);
                currentMissionIndex = widgets.IndexOf(activeWidget);
                if (uiHelper.ElementIsBehind(scroll, activeWidget))
                {
                    MoveScrollToCurrentIndex();
                }
            }
        }

        private void ChangeWidgetsMode()
        {
            /*float delay = 0;
            foreach (MissionWidget missionWidget in widgets)
            {
                bool isAnimated = uiHelper.ElementIsVisible(scroll, missionWidget);

                if (isAnimated)
                {
                    DOVirtual.DelayedCall(delay, () => missionWidget.ToggleMode(true));
                    delay += 0.05f;
                }
                else
                {
                    missionWidget.ToggleMode(false);
                }
            }#1#
        }*/
        
        /*private List<float2> GetPointerPositionsList()
        {
            UQueryState<VisualElement> pointers = pointersQueryBuilder.Build();
            List<float2> result = new();
            float x, y;

            pointers.ForEach((pointer) =>
            {
                x = pointer.worldBound.center.x;
                y = pointer.worldBound.center.y;
                result.Add(new float2(x, y));
            });

            return result;
        }*/
        
        /*private void OnPointerClick(ClickEvent clk)
        {
            VisualElement target = (VisualElement)clk.currentTarget;

            currentMissionIndex = pointersQueryBuilder.Build().ToList().IndexOf(target);
            MoveScrollToCurrentIndex();
            MoveCrossPointerToCurrentIndex();
        }*/
        
        /*
        private void MoveCrossPointerToCurrentIndex()
        {
            float2 target = new(widgetPositions[currentMissionIndex].x - 40, widgetPositions[currentMissionIndex].y - 40);
            DOTween.To(() => currentWidgetPosition, x => currentWidgetPosition = x, target, 0.6f)
                .OnUpdate(() =>
                {
                    SetCrossPointerPosition(currentWidgetPosition);
                })
                .SetTarget(crossPointer).Play().OnComplete(()=>
                {
                    currentWidgetPosition = target;
                });
        }*/

        /*private void SetCrossPointerPosition(float2 position)
        {
            float x = math.clamp(position.x, 0, crossPointer.resolvedStyle.width - 80);
            float y = math.clamp(position.y, 0, crossPointer.resolvedStyle.height - 80);

            crossPointerLeft.style.width = x;
            crossPointerTop.style.height = y;

            crossPointerTop.style.left = x + 40;
            crossPointerBottom.style.left = x + 40;

            currentWidgetPosition = new float2(x,y);
        }*/
                
        //public void UpdateLastMissionWidget(Mission mission) => widgets.Find(x=> x.MissionIndex == mission.MissionIndex).SetMission(mission);

        /*public void UpdateScrollButtonArrows()
        {
            Debug.Log(nameof(UpdateScrollButtonArrows) + "not implemented");
        }*/
        
        /*public void MoveCrossPointerToScreenCenter()
        {
            float2 target = (float2)scroll.scrollOffset + screenCenter;

            float speed = 600.0f;

            float2 temp = Vector2.MoveTowards(currentWidgetPosition, target, speed * Time.deltaTime);

            SetCrossPointerPosition(temp);
        }*/
        
        /*public void SetHardMode()
      {
          Debug.Log("Hard Mode visuals is not implemented yet");

          /*isHardMode = true;
          foreach (MissionWidget missionWidget in widgets)
          {
              missionWidget.SetMode(true, true);
          }#1#
      }*/
        
        /*private void InitCrossPointer()
     {
         crossPointer = this.Q<VisualElement>("CrossPointer");
         crossPointerLeft = crossPointer.Q<VisualElement>("Left");
         crossPointerTop = crossPointer.Q<VisualElement>("Top");
         crossPointerBottom = crossPointer.Q<VisualElement>("Bottom");
     }*/
    }
}
