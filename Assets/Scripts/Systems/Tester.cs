using CardTD.Utilities;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UI;
using Unity.Mathematics;
using UnityEngine.UIElements;
using UnityEngine;

public class Tester : MonoBehaviour
{
    public UIDocument NewMissionsPanel;

    private MissionPanel missionPanel;
    private ScrollView scroll;
    private UQueryBuilder<TemplateContainer> widgetsQueryBuilder;
    private UQueryBuilder<VisualElement> pointersQueryBuilder;
    
    private List<float2> widgetPositions;
    private List<float2> pointerPositions;
    private float2 currentPointerPosition;
    
    private VisualElement leftButton;
    private VisualElement rightButton;
    private VisualElement pointerContainer;
    private VisualElement pointerTop;
    private VisualElement pointerBottom;
    private VisualElement pointerLeft;

    private int currentIndex;
    private float2 screenCenter;

    [Button]
    private void Init()
    {
        missionPanel = NewMissionsPanel.rootVisualElement.Q<MissionPanel>();
        scroll = missionPanel.Q<ScrollView>(); 
        widgetsQueryBuilder = missionPanel.Q<VisualElement>("MissionsGroup").Query<TemplateContainer>();
        pointersQueryBuilder = missionPanel.Q<VisualElement>("MissionsGroup").Query<VisualElement>("Pointer");
        
        pointerContainer = missionPanel.Q<VisualElement>("PointerContainer");
        pointerLeft = pointerContainer.Q<VisualElement>("Left");
        pointerTop = pointerContainer.Q<VisualElement>("Top");
        pointerBottom = pointerContainer.Q<VisualElement>("Bottom");

        leftButton = missionPanel.Q<VisualElement>("LeftScrollButton");
        leftButton.RegisterCallback<ClickEvent>(OnSideButtonClick);
        rightButton = missionPanel.Q<VisualElement>("RightScrollButton");
        rightButton.RegisterCallback<ClickEvent>(OnSideButtonClick);

        widgetPositions = GetMissionWidgetPositionsList();
        pointerPositions = GetMissionPointerPositionsList();

        currentIndex = 0;
        MoveScrollToCurrentIndex();
        MovePointerToCurrentIndex();
    }

    private void OnDestroy()
    {
        leftButton.UnregisterCallback<ClickEvent>(OnSideButtonClick);
        rightButton.UnregisterCallback<ClickEvent>(OnSideButtonClick);
        
        widgetsQueryBuilder.Build().ForEach((widget) => widget.UnregisterCallback<ClickEvent>(OnWidgetClick));
        pointersQueryBuilder.Build().ForEach((pointer)=> pointer.UnregisterCallback<ClickEvent>(OnPointerClick));
    }

    private void OnSideButtonClick(ClickEvent clk)
    {
        if (((VisualElement)(clk.currentTarget)).name == rightButton.name)
        {
            currentIndex++;
            if (currentIndex >= widgetPositions.Count)
                currentIndex = 0;
        }
        else if (((VisualElement)(clk.currentTarget)).name == leftButton.name)
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = widgetPositions.Count - 1;
        }

        MoveScrollToCurrentIndex();
        MovePointerToCurrentIndex();
    }

    private void OnWidgetClick(ClickEvent clk)
    {
        TemplateContainer target = (TemplateContainer)clk.currentTarget;
        
        currentIndex = widgetsQueryBuilder.Build().ToList().IndexOf(target);
        MoveScrollToCurrentIndex();
        MovePointerToCurrentIndex();
    }

    private void OnPointerClick(ClickEvent clk)
    {
        VisualElement target = (VisualElement)clk.currentTarget;

        currentIndex = pointersQueryBuilder.Build().ToList().IndexOf(target);
        MoveScrollToCurrentIndex();
        MovePointerToCurrentIndex();
    }

    private void MoveScrollToCurrentIndex()
    {
        Vector2 currentScrollOffset = scroll.scrollOffset;
        DOTween.To(() => currentScrollOffset, x => currentScrollOffset = x, widgetPositions[currentIndex], 0.4f)
            .OnUpdate(() => scroll.scrollOffset = currentScrollOffset)
            .SetTarget(scroll).Play();
    }

    private void MovePointerToCurrentIndex()
    {
        float2 target = new(pointerPositions[currentIndex].x - 40, pointerPositions[currentIndex].y - 40);
        DOTween.To(() => currentPointerPosition, x => currentPointerPosition = x, target, 0.8f)
            .OnUpdate(() =>
            {
                SetPointerPosition(currentPointerPosition);
            })
            .SetTarget(pointerContainer).Play().OnComplete(()=> currentPointerPosition = target);
    }

    private void SetPointerPosition(float2 position)
    {
        pointerLeft.style.width = position.x;
        pointerTop.style.height = position.y;

        pointerTop.style.left = position.x + 40;
        pointerBottom.style.left = position.x + 40;
    }
    
    private List<float2> GetMissionWidgetPositionsList()
    {
        UQueryState<TemplateContainer> widgetTemplatesQuery = widgetsQueryBuilder.Build();
        List<float2> result = new();
        float x, y;
        screenCenter = new(missionPanel.worldBound.center.x, missionPanel.worldBound.center.y);

        widgetTemplatesQuery.ForEach((widget) =>
        {
            widget.RegisterCallback<ClickEvent>(OnWidgetClick);
            x = widget.worldBound.center.x - screenCenter.x;
            y = widget.worldBound.center.y - screenCenter.y;
            result.Add(new float2(x, y));
        });

        return result;
    }
    
    private List<float2> GetMissionPointerPositionsList()
    {
        UQueryState<VisualElement> pointers = pointersQueryBuilder.Build();
        List<float2> result = new();
        float x, y;

        pointers.ForEach((pointer) =>
        {
            pointer.RegisterCallback<ClickEvent>(OnPointerClick);
            x = pointer.worldBound.center.x;
            y = pointer.worldBound.center.y;
            result.Add(new float2(x, y));
        });

        return result;
    }
}