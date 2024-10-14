#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


[CustomGridBrush(false, true, false, "DropZoneBrush")]
public class DropZoneBrush : ObjectBrushBase
{
    [SerializeField] private DropZoneVisual dropZoneVisualPrefab;
    
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Paint(gridLayout, brushTarget, position);

        GameObject dropZoneGroup = LevelGrid.DropZones;

        BoundsInt bounds = new BoundsInt(position, TileSetSize);
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (GetObjectInCell(pos, dropZoneGroup.transform) != null)
            {
                return;
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(dropZoneGroup, "Paint DropZoneVisual");

        DropZoneVisual dropZoneVisual = Instantiate(dropZoneVisualPrefab, dropZoneGroup.transform, true);
        dropZoneVisual.transform.position = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(position) + Vector3.up);
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Erase(gridLayout, brushTarget, position);

        GameObject dropZonesGroup = LevelGrid.DropZones;
        
        BoundsInt bounds = new (position, TileSetSize);

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Transform child = GetObjectInCell(pos, dropZonesGroup.transform);
            if (child != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(dropZonesGroup, "Erase DropZone");
                DestroyImmediate(child.gameObject);
                break;
            }
        }
    }
}

[CustomEditor(typeof(DropZoneBrush))]
public class DropZoneBrushEditor : ObjectBrushEditorBase { }
#endif