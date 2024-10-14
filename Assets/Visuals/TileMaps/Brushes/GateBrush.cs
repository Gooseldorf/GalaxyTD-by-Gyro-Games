#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomGridBrush(false, true, false, "GateBrush")]
public class GateBrush : GridBrush
{
    [SerializeField] private GateVisual gateVisualPrefab;
    
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
    
    public TileBase [] SideTiles;
    public bool IsDrawing;
    
    public Vector3Int StartPosition;
    public AllEnums.Direction Direction;

    private GateVisual currentGate;
    
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        Tilemap objectsTilemap = LevelGrid.Objects;
        
        if(objectsTilemap == null) return;
        
        GameObject gateGroup = LevelGrid.Gates;
        
        if (GetObjectInCell(position, gateGroup.transform) != null)
        {
            return;
        }
        
        Undo.RegisterFullObjectHierarchyUndo(gateGroup, "Paint Gate");
        
        if (!IsDrawing && currentGate == null)
        {
            currentGate = Instantiate(gateVisualPrefab, gateGroup.transform, true);
            currentGate.transform.localPosition = position;
            currentGate.SetOffset();
            SetDirection(Direction);
            IsDrawing = true;
            StartPosition = position;
        }
        else
        {
            if (Direction == AllEnums.Direction.Left || Direction == AllEnums.Direction.Right)
            {
                int length = Math.Abs(position.x - StartPosition.x);
                if(length + 1 < currentGate.MinGateLength) return;
                
                currentGate.SetLength(length + 1);
            }
            else
            {
                int length = Math.Abs(position.y - StartPosition.y);
                if(length + 1 < currentGate.MinGateLength) return;
                
                currentGate.SetLength(length + 1);
            }
            IsDrawing = false;
            currentGate = null;
        }
    }
    
    private void SetDirection(AllEnums.Direction direction)
  {
      switch (direction)
      {
          case AllEnums.Direction.Left:
              currentGate.transform.rotation = Quaternion.Euler(0,0,-90);
              break;
          case AllEnums.Direction.Right:
              currentGate.transform.rotation = Quaternion.Euler(0,0,90);
              break;
          case AllEnums.Direction.Up:
              currentGate.transform.rotation = Quaternion.Euler(0,0,180);
              break;
          case AllEnums.Direction.Down:
              currentGate.transform.rotation = Quaternion.Euler(0,0,0);
              break;
      }
  }
    
    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        GameObject gateGroup = LevelGrid.Gates;
        
        Transform child = GetObjectInCell(position, gateGroup.transform);
        if (child != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(gateGroup, "Erase Gate");
            DestroyImmediate(child.gameObject);
        }
    }
    
    public bool IsHorizontalLine(Vector3Int end) => StartPosition.y == end.y;

    public bool IsVerticalLine(Vector3Int end) => StartPosition.x == end.x;
    
    private Transform GetObjectInCell(Vector3Int position, Transform parent)
    {
        BoundsInt bounds = new (position, Vector3Int.one);

        int childCount = parent.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);
           
            if (bounds.Contains(new Vector3Int((int)child.transform.position.x, (int)child.transform.position.y)))
            {
                return child;
            }
        }
        return null;
    }
}

[CustomEditor(typeof(GateBrush))]
public class GateBrushEditor: GridBrushEditor
{
    public override GameObject[] validTargets => !ValidateLevelGrid() ? null : new[] { gateBrush.LevelGrid.Objects.gameObject };

    private GateBrush gateBrush => target as GateBrush;

    public override void OnPaintSceneGUI(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
    {
        var evt = Event.current;

        if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Space && !gateBrush.IsDrawing)
        {
            AllEnums.Direction[] directions = (AllEnums.Direction[])Enum.GetValues(typeof(AllEnums.Direction));

            int currentIndex = Array.IndexOf(directions, gateBrush.Direction);
            int nextIndex = (currentIndex + 1) % directions.Length;

            gateBrush.Direction = directions[nextIndex];
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
        
        Tilemap objects = gateBrush.LevelGrid.Objects;
        if (objects == null) return;

        TileBase previewTile = null;

        switch (gateBrush.Direction)
        {
            case AllEnums.Direction.Left:
                if (gateBrush.IsDrawing && (!gateBrush.IsHorizontalLine(position) || gateBrush.StartPosition.x < position.x))
                {
                    ClearPreview();
                    return;
                }
                previewTile = gateBrush.IsDrawing ? gateBrush.SideTiles[1] : gateBrush.SideTiles[0];
                break;
            case AllEnums.Direction.Right:
                if (gateBrush.IsDrawing && (!gateBrush.IsHorizontalLine(position) || gateBrush.StartPosition.x > position.x))
                {
                    ClearPreview();
                    return;
                }
                previewTile = gateBrush.IsDrawing ? gateBrush.SideTiles[0] : gateBrush.SideTiles[1];
                break;
            case AllEnums.Direction.Up:
                if (gateBrush.IsDrawing && (!gateBrush.IsVerticalLine(position) || gateBrush.StartPosition.x > position.x))
                {
                    ClearPreview();
                    return;
                }
                previewTile = gateBrush.IsDrawing ? gateBrush.SideTiles[3] : gateBrush.SideTiles[2];
                break;
            case AllEnums.Direction.Down:
                if (gateBrush.IsDrawing && (!gateBrush.IsVerticalLine(position) || gateBrush.StartPosition.x < position.x))
                {
                    ClearPreview();
                    return;
                }
                previewTile = gateBrush.IsDrawing ? gateBrush.SideTiles[2] : gateBrush.SideTiles[3];
                break;
        }
        if(previewTile == null) return;
        
        objects.SetEditorPreviewTile(position, previewTile);
    }
    
    public override void ClearPreview()
    {
        if(!ValidateLevelGrid()) return;
        
        Tilemap objects = gateBrush.LevelGrid.Objects;
        if (objects == null) return;
        
        objects.ClearAllEditorPreviewTiles();
    }
    
    private bool ValidateLevelGrid()
    {
        return gateBrush.LevelGrid != null && gateBrush.LevelGrid.Objects != null;
    }
}
#endif