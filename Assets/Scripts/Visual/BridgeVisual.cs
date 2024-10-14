using ECSTest.Structs;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BridgeVisual : EnvironmentVisual, IPowerableVisual
{
    private const int minBridgeLength = 3;
    [SerializeField] private Transform startPart;
    [SerializeField] private Transform middlePart;
    [SerializeField] private Transform endPart;

    [SerializeField] private List<Sprite> ActiveSprites;
    [SerializeField] private List<Sprite> InactiveSprites;
    [ShowInInspector] public int Id { get; set; }
    [field: SerializeField] public bool IsPowered { get; set; }

    private SpriteRenderer[] bridgeSections;

    [Button]
    public void TogglePower() => SetPowered(!IsPowered);

    public void SetPowered(bool isPowered)
    {
        IsPowered = isPowered;
        UpdateBridgeSections();
    }

    public void InitPosition(GridPositionStruct gridPosition, bool isPowered)
    {
        float2 position = gridPosition.GridPos;
        position.x += (gridPosition.GridSize.y > 2) ? 1f : .5f;
        position.y += (gridPosition.GridSize.x > 2) ? 1f : .5f;
        InitPosition(position, gridPosition.GridSize);
        SetPowered(isPowered);
    }

    private void UpdateBridgeSections()
    {
        if (bridgeSections == null)
            bridgeSections = GetComponentsInChildren<SpriteRenderer>();
        bridgeSections[0].sprite = IsPowered ? ActiveSprites[0] : InactiveSprites[0];

        for (int i = 1; i < bridgeSections.Length - 1; i++)
        {
            bridgeSections[i].sprite = IsPowered ? ActiveSprites[1] : InactiveSprites[1];
        }

        bridgeSections[^1].sprite = IsPowered ? ActiveSprites[^1] : InactiveSprites[^1];
    }

    public Bridge GetBridgeData(Tilemap targetTilemap, int2 gridPositionOffset)
    {
        GridPosition gridPosition = GetGridPos(targetTilemap);

        Bridge result = new Bridge() { GridPos = gridPosition.GridPos + gridPositionOffset, GridSize = gridPosition.GridSize, IsPowered = IsPowered };
        return result;
    }

    private GridPosition GetGridPos(Tilemap targetTilemap)
    {
        int roundStartPosX = Mathf.RoundToInt(startPart.position.x);
        int roundEndPosX = Mathf.RoundToInt(endPart.position.x);
        int roundStartPosY = Mathf.RoundToInt(startPart.position.y);
        int roundEndPosY = Mathf.RoundToInt(endPart.position.y);

        Vector3 startPosition;
        Vector3 endPosition;

        if (roundStartPosX < roundEndPosX || roundStartPosY < roundEndPosY)
        {
            startPosition = startPart.position;
            endPosition = endPart.position;
        }
        else
        {
            startPosition = endPart.position;
            endPosition = startPart.position;
        }

        Vector3 startOffset = (roundStartPosX != roundEndPosX) ? new Vector3(0, -1) : new Vector3(-1, 0);
        Vector3 endOffset = (roundStartPosX != roundEndPosX) ? new Vector3(1, 1) : new Vector3(1, 1);

        Vector3Int startGridPosition = targetTilemap.WorldToCell(startPosition + startOffset);
        Vector3Int endGridPosition = targetTilemap.WorldToCell(endPosition + endOffset);

        int2 gridSize = new int2(endGridPosition.x - startGridPosition.x, endGridPosition.y - startGridPosition.y);
        int2 gridPos = new int2(startGridPosition.x, startGridPosition.y);

        return new GridPosition(gridPos, gridSize);
    }

    public override void InitVisual(object data)
    {
        Bridge bridge = data as Bridge;

        InitPosition(bridge);
        Id = bridge.Id;
        SetOffset(bridge.GridSize.x > 2);
    }

    public override void InitPosition(IGridPosition gridPosition)
    {
        InitPosition(gridPosition.GridPos, gridPosition.GridSize);
    }

    private void InitPosition(float2 gridPosition, int2 gridSize)
    {
        transform.localPosition = new Vector3(gridPosition.x, gridPosition.y);

        if (gridSize.x > 2)
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

    private void SetOffset(bool isHorizontal)
    {
        if (isHorizontal)
        {
            transform.localPosition += new Vector3(0.5f, 1);
        }
        else
        {
            transform.localPosition += new Vector3(1, 0.5f);
        }
    }

    public void SetLength(int length)
    {
        if (length <= minBridgeLength) return;

        for (int i = 1; i <= length - minBridgeLength; i++)
        {
            GameObject newMiddlePart = Instantiate(middlePart.gameObject, transform);
            newMiddlePart.transform.SetSiblingIndex(i + 1);
            newMiddlePart.transform.localPosition = middlePart.transform.localPosition + Vector3.down * i;
        }

        endPart.transform.localPosition += Vector3.down * (length - minBridgeLength);
    }
}