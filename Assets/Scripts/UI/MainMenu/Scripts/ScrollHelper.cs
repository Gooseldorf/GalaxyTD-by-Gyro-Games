using System;
using UI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class ScrollHelper : MonoBehaviour
{
    [SerializeField] private MenuUIManager menuUIManager;
    
    [SerializeField] private float dragThreshold = 5f;
    
    private MissionPanel missionPanel;
    private ScrollView missionsScroll;
    private VisualElement backgroundPlanet;

    private ShopPanel shopPanel;
    private VisualElement shopScroll;
    
    private Vector2 dragOriginPosition;
    private bool isDragging = false;
    private bool isOriginInsideScrollView = false;

    public void Init(MissionPanel missionPanel, ShopPanel shopPanel, VisualElement backgroundPlanet)
    {
        this.missionPanel = missionPanel;
        missionsScroll = missionPanel.Q<ScrollView>("MissionsScroll");

        this.shopPanel = shopPanel;
        shopScroll = shopPanel.Q<ScrollView>();

        this.backgroundPlanet = backgroundPlanet;
    }
    
    private void Update()
    {
        if (missionPanel == null || shopPanel == null)
            return;
        CheckScroll(menuUIManager.IsNoActiveWindows, backgroundPlanet.parent, missionPanel.SelectClosestMissionWidget, missionPanel.SnapToClosest);
        CheckScroll(shopPanel.style.display == DisplayStyle.Flex, shopScroll, shopPanel.UpdateCurrentTabIndex, shopPanel.SnapToClosest);
        ParallaxBackgroundPlanet();
    }

    private void CheckScroll(bool canScroll, VisualElement referenceElement, Action duringScroll, Action postScroll)
    {
        if(!canScroll) return;
        
        CheckInputForScroll(referenceElement, duringScroll, postScroll);
    }

    private void CheckInputForScroll(VisualElement referenceElement, Action duringScroll, Action postScroll)
    {
#if UNITY_EDITOR
        CheckMouseInputForScroll(referenceElement, duringScroll, postScroll);
#endif
        CheckTouchInputForScroll(referenceElement, duringScroll, postScroll);
    }

    private void CheckMouseInputForScroll(VisualElement referenceElement, Action duringScroll, Action postScroll)
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOriginPosition = Input.mousePosition;
            isOriginInsideScrollView = IsPointInsideElement(dragOriginPosition, referenceElement);
        }
        else if (Input.GetMouseButton(0))
        {
            if (isOriginInsideScrollView && Vector2.Distance(dragOriginPosition, Input.mousePosition) > dragThreshold)
            {
                isDragging = true;
                duringScroll.Invoke();
            }
        }
        else if (isDragging && isOriginInsideScrollView)
        {
            isDragging = false;
            isOriginInsideScrollView = false;
            postScroll.Invoke();
        }
    }

    private void CheckTouchInputForScroll(VisualElement referenceElement, Action duringScroll, Action postScroll)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    dragOriginPosition = touch.position;
                    isOriginInsideScrollView = IsPointInsideElement(dragOriginPosition, referenceElement);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isOriginInsideScrollView && Vector2.Distance(dragOriginPosition, touch.position) > dragThreshold)
                    {
                        isDragging = true;
                        duringScroll.Invoke();
                    }
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging && isOriginInsideScrollView)
                    {
                        isDragging = false;
                        isOriginInsideScrollView = false;
                        postScroll.Invoke();
                    }
                    break;
            }
        }
    }

    private bool IsPointInsideElement(Vector2 point, VisualElement element)
    {
        Vector2 localPoint = element.WorldToLocal(point);

        bool isInside = localPoint.x >= 0 &&
                        localPoint.y >= 0 &&
                        localPoint.x <= element.layout.width &&
                        localPoint.y <= element.layout.height;

        return isInside;
    }

    private void ParallaxBackgroundPlanet()
    {
        float2 currentOffset = missionsScroll.scrollOffset;
        float parallaxFactor = 0.1f;

        Vector3 newPosition = new Vector3(-currentOffset.x * parallaxFactor, -currentOffset.y * parallaxFactor, backgroundPlanet.transform.position.z);
    
        backgroundPlanet.transform.position = newPosition;
    }
}