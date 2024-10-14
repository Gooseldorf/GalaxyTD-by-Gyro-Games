#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;


[CustomGridBrush(false, true, false, "PortalBrush")]
public class PortalBrush : ObjectBrushBase
{
    [SerializeField] private PortalVisual portalVisualPrefab;
    private PortalVisual currentPortal;
    
    public List<TileBase> EnterTileSet;
    public List<TileBase> ExitTileSet;

    public bool IsDrawExit = false;
    
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Paint(gridLayout, brushTarget, position);

        GameObject portalGroup = LevelGrid.Portals;

        BoundsInt bounds = new (position, TileSetSize);
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (GetObjectInCell(pos, portalGroup.transform) != null)
            {
                return;
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(portalGroup, "Paint Portal");

        if (!IsDrawExit && currentPortal == null)
        {
            currentPortal = Instantiate(portalVisualPrefab, portalGroup.transform);
            currentPortal.InPortal.transform.position = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(position) + Vector3.up);
            IsDrawExit = true;
        }
        else
        {
            currentPortal.OutPortal.transform.position = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(position) + Vector3.up);
            currentPortal.OutPortal.gameObject.SetActive(true);
            IsDrawExit = false;
            currentPortal = null;
        }
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Erase(gridLayout, brushTarget, position);

        GameObject portalGroup = LevelGrid.Portals;
        
        BoundsInt bounds = new (position, TileSetSize);

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Transform child = GetObjectInCell(pos, portalGroup.transform);
            if (child != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(portalGroup, "Erase PortalVisual");
                DestroyImmediate(child.gameObject);
                break;
            }
        }
    }
}

[CustomEditor(typeof(PortalBrush))]
public class PortalBrushEditor : ObjectBrushEditorBase
{
    public override void PaintPreview(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        PortalBrush portalBrush = objectBrush as PortalBrush;
        
        portalBrush.TileSet = portalBrush.IsDrawExit ? portalBrush.ExitTileSet : portalBrush.EnterTileSet;
        
        base.PaintPreview(grid, brushTarget, position);
    }
}
#endif