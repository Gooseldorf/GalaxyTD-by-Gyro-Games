#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

[CustomGridBrush(false, true, false, "BridgeBrush")]
public class BridgeBrush : GridBrush
{
    [SerializeField] private BridgeVisual bridgeVisualPrefab;
    
    private LevelGrid levelGrid;

    public LevelGrid LevelGrid
    {
        get
        {
            if (levelGrid == null)
            {
                levelGrid = FindFirstObjectByType<LevelGrid>();
            }
            return levelGrid;
        }
    }
    
    public List<TileBase> LeftTileSet;
    public List<TileBase> RightTileSet;
    public List<TileBase> UpTileSet;
    public List<TileBase> DownTileSet;

    public Vector3Int verticalTileSetSize = new (3,4,1);
    public Vector3Int horizontalTileSetSize = new (4,3,1);

    public Vector3Int StartPosition;
    public AllEnums.Direction Direction;
    public bool IsDrawing = false;
    
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        Tilemap floorTilemap = LevelGrid.Floor;
        if(floorTilemap == null) return;

        Tilemap objectsTilemap = LevelGrid.Objects;
        if(objectsTilemap == null) return;
        
        GameObject bridgeGroup = LevelGrid.Bridges;
        
        BoundsInt bounds;
        
        switch (Direction)
        {
            case AllEnums.Direction.Left:
                bounds = new BoundsInt(position + Vector3Int.left * 2, verticalTileSetSize);
                if (IsDrawing)
                {
                    if( (!IsHorizontalLine(position) || StartPosition.x - 4 < position.x)) return;
                    PaintTiles(bounds, RightTileSet);
                    PaintBridgeObject(bridgeGroup.transform, AllEnums.Direction.Left, Math.Abs(position.x - StartPosition.x) - 1);
                    break;
                }
                StartPosition = position;
                PaintTiles(bounds, LeftTileSet);
                break;
            case AllEnums.Direction.Right:
                bounds = new BoundsInt(position + Vector3Int.left * 2, verticalTileSetSize);
                if (IsDrawing)
                {
                    if(!IsHorizontalLine(position) || StartPosition.x + 4 > position.x) return;
                    PaintTiles(bounds, LeftTileSet);
                    PaintBridgeObject(bridgeGroup.transform, AllEnums.Direction.Right, Math.Abs(position.x - StartPosition.x) - 1);
                    break;
                }
                StartPosition = position;
                PaintTiles(bounds, RightTileSet);
                break;
            case AllEnums.Direction.Up:
                bounds = new BoundsInt(position + Vector3Int.left * 3, horizontalTileSetSize);
                if (IsDrawing)
                {
                    if(!IsVerticalLine(position) || StartPosition.y + 4 > position.y) return;
                    PaintTiles(bounds, DownTileSet);
                    PaintBridgeObject(bridgeGroup.transform, AllEnums.Direction.Up, Math.Abs(position.y - StartPosition.y) - 1);
                    break;
                }
                StartPosition = position;
                PaintTiles(bounds, UpTileSet);
                break;
            case AllEnums.Direction.Down:
                bounds = new BoundsInt(position + Vector3Int.left * 3, horizontalTileSetSize);
                if (IsDrawing)
                {
                    if(!IsVerticalLine(position) || StartPosition.y - 4 < position.y) return;
                    PaintTiles(bounds, UpTileSet);
                    PaintBridgeObject(bridgeGroup.transform, AllEnums.Direction.Down,Math.Abs(position.y - StartPosition.y) - 1);
                    break;
                }
                StartPosition = position;
                PaintTiles(bounds, DownTileSet);
                break;
        }

        IsDrawing = !IsDrawing;
    }
    
    private void PaintTiles(BoundsInt bounds, List<TileBase> tileSet)
    {
        if (tileSet.Count == 0) return;
        
        Undo.RegisterCompleteObjectUndo(LevelGrid.Floor, "Paint");
        int i = 0;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {   
            LevelGrid.Floor.SetTile(pos, tileSet[i]);
            i++;
        }
    }

    private void PaintBridgeObject(Transform parent, AllEnums.Direction direction, int length)
    {
        Undo.RegisterFullObjectHierarchyUndo(parent.gameObject, "Paint Bridge");

        BridgeVisual bridgeVisual = Instantiate(bridgeVisualPrefab, parent);
        bridgeVisual.transform.localPosition = StartPosition;

        switch (direction)
        {
            case AllEnums.Direction.Left:
                bridgeVisual.transform.localPosition = StartPosition + new Vector3(-1.5f, 2f);
                break;
            case AllEnums.Direction.Right:
                bridgeVisual.transform.localPosition = StartPosition + new Vector3(0.5f, 2f);
                break;
            case AllEnums.Direction.Up:
                bridgeVisual.transform.localPosition = StartPosition + new Vector3(-1, 2.5f);
                break;
            case AllEnums.Direction.Down:
                bridgeVisual.transform.localPosition = StartPosition + new Vector3(-1, 0.5f);
                break;
        }
        SetDirection(bridgeVisual, direction);
        bridgeVisual.SetLength(length);
    }
    
    public void SetDirection(BridgeVisual visual, AllEnums.Direction direction)
    {
        switch (direction)
        {
            case AllEnums.Direction.Left:
                visual.transform.rotation = Quaternion.Euler(0,0,-90);
                break;
            case AllEnums.Direction.Right:
                visual.transform.rotation = Quaternion.Euler(0,0,90);
                break;
            case AllEnums.Direction.Up:
                visual. transform.rotation = Quaternion.Euler(0,0,180);
                break;
        }
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        GameObject bridgeGroup = LevelGrid.Bridges;

        BoundsInt bounds = new (position, Vector3Int.one);

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Transform child = GetObjectInCell(bridgeGroup.transform, bounds);
            if (child != null)
            {
                Transform parentTransform = bridgeGroup.transform;
                Undo.RegisterFullObjectHierarchyUndo(parentTransform.gameObject, "Erase Bridge");
                DestroyImmediate(child.gameObject);
                break;
            }
        }
    }
    
    private Transform GetObjectInCell(Transform parentGroup, BoundsInt bounds)
    {
        int bridgesCount = parentGroup.childCount;

        for (int i = 0; i < bridgesCount; i++)
        {
            Transform bridge = parentGroup.GetChild(i);

            int childCount = bridge.childCount;

            for (int j = 0; j < childCount; j++)
            {
                Transform child = bridge.GetChild(j);

                if (bounds.Contains(new Vector3Int((int)child.transform.position.x, (int)child.transform.position.y)))
                    return bridge;
            }
        }

        return null;
    }

    public bool IsHorizontalLine(Vector3Int end) => StartPosition.y == end.y;

    public bool IsVerticalLine(Vector3Int end) => StartPosition.x == end.x;
}

[CustomEditor(typeof(BridgeBrush))]
public class BridgeBrushEditor: GridBrushEditor
{
    public override GameObject[] validTargets => !ValidateLevelGrid() ? null : new[] { bridgeBrush.LevelGrid.Objects.gameObject };
    
    private BridgeBrush bridgeBrush => target as BridgeBrush;

    public override void OnPaintSceneGUI(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
    {
        var evt = Event.current;

        if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Space && !bridgeBrush.IsDrawing)
        {
            AllEnums.Direction[] directions = (AllEnums.Direction[])Enum.GetValues(typeof(AllEnums.Direction));

            int currentIndex = Array.IndexOf(directions, bridgeBrush.Direction);
            int nextIndex = (currentIndex + 1) % directions.Length;

            bridgeBrush.Direction = directions[nextIndex];
        }

        base.OnPaintSceneGUI(gridLayout, brushTarget, position, tool, executing);
        ClearPreview();
        PaintPreview(gridLayout, brushTarget, position.position);
    }

    public override void PaintPreview(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        if (!ValidateLevelGrid())
        {
            ClearPreview();
            return;
        }
        
        Tilemap floor = bridgeBrush.LevelGrid.Floor;
        if (floor == null) return;

        BoundsInt bounds = default;
        List<TileBase> tileSet = default;
        
        switch (bridgeBrush.Direction)
        {
            case AllEnums.Direction.Left:
                if (bridgeBrush.IsDrawing && (!bridgeBrush.IsHorizontalLine(position) || bridgeBrush.StartPosition.x - 4 < position.x))
                {
                    ClearPreview();
                    return;
                }
                bounds = new BoundsInt(position + Vector3Int.left * 2, bridgeBrush.verticalTileSetSize);
                tileSet = bridgeBrush.IsDrawing? bridgeBrush.RightTileSet : bridgeBrush.LeftTileSet;
                break;
            
            case AllEnums.Direction.Right:
                if (bridgeBrush.IsDrawing && (!bridgeBrush.IsHorizontalLine(position) || bridgeBrush.StartPosition.x + 4 > position.x))
                {
                    ClearPreview();
                    return;
                }
                bounds = new BoundsInt(position + Vector3Int.left * 2, bridgeBrush.verticalTileSetSize);
                tileSet = bridgeBrush.IsDrawing? bridgeBrush.LeftTileSet : bridgeBrush.RightTileSet;
                break;
            
            case AllEnums.Direction.Up: 
                if(bridgeBrush.IsDrawing && (!bridgeBrush.IsVerticalLine(position) || bridgeBrush.StartPosition.y + 4 > position.y))
                {
                    ClearPreview();
                    return;
                }
                bounds = new BoundsInt(position + Vector3Int.left * 3, bridgeBrush.horizontalTileSetSize);
                tileSet = bridgeBrush.IsDrawing? bridgeBrush.DownTileSet : bridgeBrush.UpTileSet;
                break;
            
            case AllEnums.Direction.Down: 
                if(bridgeBrush.IsDrawing && (!bridgeBrush.IsVerticalLine(position) || bridgeBrush.StartPosition.y - 4 < position.y))
                {
                    ClearPreview();
                    return;
                }
                bounds = new BoundsInt(position + Vector3Int.left * 3, bridgeBrush.horizontalTileSetSize);
                tileSet = bridgeBrush.IsDrawing? bridgeBrush.UpTileSet : bridgeBrush.DownTileSet;
                break;
        }
        
        PaintPreviewInternal(floor, bounds, tileSet);
    }

    private void PaintPreviewInternal(Tilemap floor, BoundsInt bounds, List<TileBase> tileSet)
    {
        if (tileSet.Count == 0) return;
    
        int i = 0;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {   
            if(tileSet.Contains(floor.GetTile(pos))) return;
            floor.SetEditorPreviewTile(pos, tileSet[i]);
            i++;
        }
    }
    public override void ClearPreview()
    {
        if(!ValidateLevelGrid()) return;
        
        Tilemap floor = bridgeBrush.LevelGrid.Floor;
        if (floor == null) return;
        
        floor.ClearAllEditorPreviewTiles();
    }
    
    private bool ValidateLevelGrid()
    {
        return bridgeBrush.LevelGrid != null && bridgeBrush.LevelGrid.Objects != null;
    }
}
#endif