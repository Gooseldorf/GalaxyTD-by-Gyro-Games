using ECSTest.Structs;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GateVisual : EnvironmentVisual, IPowerableVisual
{
    [SerializeField] private Transform startPart;
    [SerializeField] private Transform forceField;
    [SerializeField] private Transform middlePart;
    [SerializeField] private Transform endPart;
    [SerializeField] private Transform endPartBackground;

    private readonly float offset = 0.5f;

    public Transform StartPart => startPart;
    public Transform EndPart => endPart;

    [ShowInInspector] public int Id { get; set; }
    [field: SerializeField] public bool IsPowered { get; set; }

    public int MinGateLength = 3;

    [Button]
    public void TogglePower() => SetPowered(!IsPowered);

    public void SetPowered(bool isPowered)
    {
        IsPowered = isPowered;
        forceField.gameObject.SetActive(IsPowered);
    }

    public void InitPosition(GridPositionStruct gridPosition, bool isPowered)
    {
        float2 position = gridPosition.GridPos + new float2(.5f, .5f);
        InitPosition(position, gridPosition.GridSize);
        SetPowered(isPowered);
    }

    public override void InitVisual(object data)
    {
        Gate gate = data as Gate;

        Id = gate.Id;
        InitPosition(gate);
        SetOffset();
    }

    public override void InitPosition(IGridPosition gridPosition)
    {
        InitPosition(gridPosition.GridPos, gridPosition.GridSize);
    }

    private void InitPosition(float2 gridPosition, int2 gridSize)
    {
        transform.localPosition = new Vector3(gridPosition.x, gridPosition.y);

        if (gridSize.x > 1)
        {
            transform.rotation = Quaternion.Euler(0, 0, 90);
            SetLength(gridSize.x);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, 180);
            SetLength(gridSize.y);
        }
    }


    public void SetLength(int length)
    {
        for (int i = 1; i <= length - MinGateLength; i++)
        {
            GameObject newMiddlePart = Instantiate(middlePart.gameObject, forceField.transform);
            newMiddlePart.transform.SetSiblingIndex(i + 1);
            newMiddlePart.transform.localPosition = middlePart.localPosition + Vector3.down * i;
        }

        endPart.localPosition += Vector3.down * (length - MinGateLength);
        endPartBackground.localPosition = endPart.localPosition + Vector3.up;
        endPart.gameObject.SetActive(true);
        forceField.gameObject.SetActive(true);
    }

    public void SetOffset() => transform.localPosition += new Vector3(offset, offset);

    private void RemoveOffset() => transform.localPosition -= new Vector3(offset, offset);

    public Gate GetGateData(int2 gridPosOffset, Tilemap targetTilemap)
    {
        RemoveOffset();
        Gate result = new Gate() { GridPos = GetGridPos(targetTilemap) + gridPosOffset, GridSize = GetGridSize(), IsPowered = this.IsPowered };
        SetOffset();

        return result;
    }

    private int2 GetGridPos(Tilemap targetTilemap)
    {
        Vector3Int gridPosition;
        if (Mathf.RoundToInt(startPart.position.x) < Mathf.RoundToInt(endPart.position.x) || Mathf.RoundToInt(startPart.position.y) < Mathf.RoundToInt(endPart.position.y))
        {
            gridPosition = targetTilemap.WorldToCell(startPart.position);
        }
        else
        {
            gridPosition = targetTilemap.WorldToCell(endPart.position);
        }

        return new int2(gridPosition.x, gridPosition.y);
    }

    private int2 GetGridSize()
    {
        if (Mathf.RoundToInt(startPart.position.x) < Mathf.RoundToInt(endPart.position.x) || Mathf.RoundToInt(startPart.position.y) < Mathf.RoundToInt(endPart.position.y))
        {
            return new int2(Mathf.RoundToInt(endPart.position.x - startPart.position.x) + 1,
                Mathf.RoundToInt(endPart.position.y - startPart.position.y) + 1);
        }
        else
        {
            return new int2(Mathf.RoundToInt(startPart.position.x - endPart.position.x) + 1,
                Mathf.RoundToInt(startPart.position.y - endPart.position.y) + 1);
        }
    }
}