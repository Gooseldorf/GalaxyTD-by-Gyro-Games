#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomGridBrush(false, true, false, "ConveyorBrush")]
public class ConveyorBrush : ObjectBrushBase
{
    [SerializeField] private ConveyorBeltVisual conveyorVisualPrefab;
    public AllEnums.Direction Direction;
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Paint(gridLayout, brushTarget, position);

        GameObject conveyorGroup = LevelGrid.Conveyors;

        BoundsInt bounds = new BoundsInt(position, TileSetSize);
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (GetObjectInCell(pos, conveyorGroup.transform) != null)
            {
                return;
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(conveyorGroup, "Paint Conveyor");

        ConveyorBeltVisual conveyorVisual = Instantiate(conveyorVisualPrefab, conveyorGroup.transform, true);
        conveyorVisual.transform.position = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(position) + Vector3.up);
        switch (Direction)
        {
            case AllEnums.Direction.Left:
                conveyorVisual.transform.eulerAngles = new Vector3(0, 0, 90);
                break;
            case AllEnums.Direction.Down:
                conveyorVisual.transform.eulerAngles = new Vector3(0, 0, 180);
                break;
            case AllEnums.Direction.Right:
                conveyorVisual.transform.eulerAngles = new Vector3(0, 0, 270);
                break;
        }
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Erase(gridLayout, brushTarget, position);

        GameObject conveyorGroup = LevelGrid.Conveyors;
        
        BoundsInt bounds = new (position, TileSetSize);

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Transform child = GetObjectInCell(pos, conveyorGroup.transform);
            if (child != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(conveyorGroup, "Erase Conveyor");
                DestroyImmediate(child.gameObject);
                break;
            }
        }
    }
}

[CustomEditor(typeof(ConveyorBrush))]
public class ConveyorBrushEditor : ObjectBrushEditorBase
{
    private ConveyorBrush conveyorBrush => target as ConveyorBrush;
    
    public override void OnPaintSceneGUI(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
    {
        var evt = Event.current;

        if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Space)
        {
            AllEnums.Direction[] directions = (AllEnums.Direction[])Enum.GetValues(typeof(AllEnums.Direction));

            int currentIndex = Array.IndexOf(directions, conveyorBrush.Direction);
            int nextIndex = (currentIndex + 1) % directions.Length;

            conveyorBrush.Direction = directions[nextIndex];
        }

        base.OnPaintSceneGUI(gridLayout, brushTarget, position, tool, executing);
        ClearPreview();
        PaintPreview(gridLayout, brushTarget, position.position);
    }

    public override void PaintPreview(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        Tilemap objects = objectBrush.LevelGrid.Objects;
        if (objects == null) return;
        
        var bounds = new BoundsInt(position, objectBrush.TileSetSize);
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Vector3Int relativePos = pos - position;
            int index;
            switch (conveyorBrush.Direction)
            {
                case AllEnums.Direction.Up:
                    index = relativePos.y * objectBrush.TileSetSize.x + relativePos.x;
                    break;
                case AllEnums.Direction.Right:
                    index = relativePos.x * objectBrush.TileSetSize.y + relativePos.y;
                    break;
                case AllEnums.Direction.Down:
                    index = (objectBrush.TileSetSize.y - 1 - relativePos.y) * objectBrush.TileSetSize.x + (objectBrush.TileSetSize.x - 1 - relativePos.x);
                    break;
                case AllEnums.Direction.Left:
                    index = (objectBrush.TileSetSize.x - 1 - relativePos.x) * objectBrush.TileSetSize.y + (objectBrush.TileSetSize.y - 1 - relativePos.y);
                    break;
                default:
                    index = relativePos.y * objectBrush.TileSetSize.x + relativePos.x;
                    break;
            }

            if (index < 0 || index >= objectBrush.TileSet.Count)
                continue;

            objects.SetEditorPreviewTile(pos, objectBrush.TileSet[index]);

            float angle;
            Vector2 scale;
            switch (conveyorBrush.Direction)
            {
                case AllEnums.Direction.Up:
                    angle = 0;
                    scale = Vector2.one;
                    break;
                case AllEnums.Direction.Right:
                    angle = 270;
                    scale = new Vector2(-1,1);
                    break;
                case AllEnums.Direction.Down:
                    angle = 0;
                    scale = -Vector2.one;
                    break;
                case AllEnums.Direction.Left:
                    angle = -90;
                    scale = new Vector2(1,-1);
                    break;
                default:
                    angle = 0;
                    scale = Vector2.one;
                    break;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.left, Quaternion.Euler(0f, 0f, angle), new Vector3(scale.x, scale.y,1));
            objects.SetEditorPreviewTransformMatrix(pos, matrix);
        }
    }
}
#endif