using ECSTest.Components;
using Sirenix.Utilities;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using static MusicManager;

public class ConveyorBeltVisual : EnvironmentVisual, IPowerableVisual
{
    [SerializeField] private GameObject start;
    [SerializeField] private GameObject end;

    public int2 ConveyorDirection
    {
        get
        {
            int2 result = new int2();
            switch (transform.rotation.eulerAngles.z)
            {
                case 0:
                    result = new int2(0, 1);
                    break;
                case 90:
                    result = new int2(-1, 0);
                    break;
                case 180:
                    result = new int2(0, -1);
                    break;
                case 270:
                    result = new int2(1, 0);
                    break;
                default:
                    Debug.LogError($"Conveyor belt id:{Id} wrong rotation!");
                    break;
            }

            return result;
        }
        set
        {
            int angle = 0;
            if (value.x == 0 && value.y == 1)
            {
                angle = 0;
            }
            else if (value.x == -1 && value.y == 0)
            {
                angle = 90;
            }
            else if (value.x == 0 && value.y == -1)
            {
                angle = 180;
            }
            else if (value.x == 1 && value.y == 0)
            {
                angle = 270;
            }

            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
    }

    public float Speed;
    public int Id { get; set; }
    [field: SerializeField] public bool IsPowered { get; set; }

    public Conveyor GetConveyorBeltData(int2 gridPositionOffset)
    {
        return new Conveyor(GridPosition + gridPositionOffset + new int2(-1, -1), GridSize, ConveyorDirection, Id, IsPowered, Speed);
    }

    public override void InitVisual(object data)
    {
        if (data is Conveyor conveyorData)
        {
            Id = conveyorData.Id;
            IsPowered = conveyorData.IsPowered;
            InitPosition(new GridPosition(conveyorData.GridPos, conveyorData.GridSize));
            ConveyorDirection = conveyorData.ConveyorDirection;
        }
    }

    public void InitVisual(ConveyorComponent componentData, GridPositionComponent gridPositionComponent, PowerableComponent powerableComponent)
    {
        ConveyorDirection = componentData.Direction;
        Speed = componentData.Speed;
        IsPowered = powerableComponent.IsPowered;
        InitPosition(new GridPosition(gridPositionComponent.Value.GridPos, gridPositionComponent.Value.GridSize));
    }

    public void TogglePower() => SetPowered(!IsPowered);
    public void SetPowered(bool isPowered)
    {
        IsPowered = isPowered;

        if (IsPowered)
        {
            TryPlaySound(transform);
        }
        else
        {
            StopSound3D(sound, transform);
        }
    }

    //private void FixedUpdate()
    //{
    //    if (!IsPowered) return;

    //    Icon.sprite = frames[currentFrame];
    //    currentFrame = (currentFrame + 1) % frames.Length;
    //}

    private static bool DrawTable(Rect rect, bool value)
    {
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            value = !value;
            GUI.changed = true;
            Event.current.Use();
        }
#if UNITY_EDITOR
        EditorGUI.DrawRect(rect.Padding(1), value ? Color.green : Color.red);
#endif
        return value;
    }
}