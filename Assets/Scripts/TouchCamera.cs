using CardTD.Utilities;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchCamera : MonoBehaviour
{
    public static TouchCamera Instance;
    private const float x_offsetBorder = 2;
    private const float y_offsetBorder = 2;

    [SerializeField] private Camera mainCamera;

    [SerializeField] private float sqrtDeltaThreshold = 8;

    [ReadOnly] public bool CanDrag = true;

    [SerializeField] private bool useBounds = true;

    [SerializeField, ShowIf("useBounds")] private Vector4 bounds;

    private bool touchStarted = false;
    private bool wasDeltaInTouch = false;
    private bool isBubbleTapDawn = false;

    private Vector3 startTouchPosition;
    private Vector3 lastFramePosition;
    private Vector3 currentPosition;

    private Vector3 tmpCameraPosition;

    private float worldUnitInPixels;

    private Touch touch;

    public Camera MainCamera => mainCamera;

    private void Awake()
    {
        if (Instance != null)
            Debug.LogError("There are two or more instances of TouchCamera, fix it!");
        else
            Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void Init(int width, int height)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        worldUnitInPixels = 1.0f / (Screen.height / (mainCamera.orthographicSize * 2));

        bounds.x = x_offsetBorder; //x_offset;
        bounds.y = width - x_offsetBorder + 1;// - x_offset;
        bounds.z = y_offsetBorder; //y_offset;
        bounds.w = height - y_offsetBorder + 1;// - y_offset;

        mainCamera.transform.position = new Vector3((bounds.x + bounds.y) / 2, (bounds.z + bounds.w) / 2, mainCamera.transform.position.z);
    }

    private void LateUpdate()
    {
        if (!CanDrag) return;

        // if we have touch in this frame
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButton(0))
        {
            currentPosition = Input.mousePosition;
#else
        if (Input.touchCount>0)
        {
             currentPosition = Input.GetTouch(0).position;
#endif
            // if it's start of touch
            if (!touchStarted)
            {
                isBubbleTapDawn = TryGetBubble(mainCamera.ScreenToWorldPoint(currentPosition));
                touchStarted = true;
                wasDeltaInTouch = false;
                lastFramePosition =
                    startTouchPosition = currentPosition;
            }
            // if it's continuation of previous frame touch
            else
            {
                if (!wasDeltaInTouch)
                    wasDeltaInTouch = WasDelta(currentPosition);
                if (wasDeltaInTouch && !isBubbleTapDawn)
                {
                    MoveCamera(currentPosition - lastFramePosition);
                    lastFramePosition = currentPosition;
                }
            }
        }
        // if we haven't touch in this frame
        else
        {
            //if it's end of touch
            if (touchStarted)
            {
                touchStarted = false;
                OnTouchEnded();
            }
        }

        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TileDecalSystem>().UpdateDecalRenderer();
    }

    private void MoveCamera(Vector3 delta)
    {
        tmpCameraPosition = mainCamera.transform.position - delta * worldUnitInPixels;

        if (useBounds)
        {
            tmpCameraPosition.x = Mathf.Clamp(tmpCameraPosition.x, bounds.x, bounds.y);
            tmpCameraPosition.y = Mathf.Clamp(tmpCameraPosition.y, bounds.z, bounds.w);
        }

        tmpCameraPosition.z = mainCamera.transform.position.z;

        mainCamera.transform.position = tmpCameraPosition;
    }

    private void OnTouchEnded()
    {
        if (IsPointerOverUIObject()) return;//EventSystem.current.IsPointerOverGameObject()

        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(lastFramePosition);

        if (IsBubbleClicked(worldPoint) || wasDeltaInTouch)
            return;

        Entity selectedEntity = GameServices.Instance.Select(worldPoint);
        Messenger<Entity>.Broadcast(UIEvents.ObjectSelected, selectedEntity, MessengerMode.DONT_REQUIRE_LISTENER);
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    private bool WasDelta(Vector3 currentPosition)
    {
        return (startTouchPosition - currentPosition).sqrMagnitude > sqrtDeltaThreshold;
    }

    private BubbleVisual TryGetBubble(Vector3 worldPoint)
    {
        if (Physics.Raycast(worldPoint, Vector3.forward, out RaycastHit hitInfo, 100))
        {
            BubbleVisual bubbleVisual = hitInfo.transform.gameObject.GetComponent<BubbleVisual>();
            return bubbleVisual;
        }
        return null;
    }

    private bool IsBubbleClicked(Vector3 worldPoint)
    {
        BubbleVisual bubbleVisual = TryGetBubble(worldPoint);
        if (bubbleVisual != null)
        {
            bubbleVisual.OnClick();
            return true;
        }

        return false;
    }

    public void Reset()
    {
        Messenger<Entity>.Broadcast(UIEvents.ObjectSelected, Entity.Null, MessengerMode.DONT_REQUIRE_LISTENER);
    }

    public Tweener MoveToPosition(float2 position)
    {
        return MainCamera.transform.DOMove(new Vector3(position.x, position.y, MainCamera.transform.position.z), 45f)
        .SetUpdate(true)
        .SetEase(Ease.Linear)
        .SetSpeedBased();
    }

#if UNITY_EDITOR
    [Button]
    private void ShowCamerasCulling()
    {
        foreach (Camera cam in GetComponentsInChildren<Camera>())
            Debug.Log($"{cam.name}: {cam.cullingMask}");
    }
#endif
}