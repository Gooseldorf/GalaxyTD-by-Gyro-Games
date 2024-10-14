#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


[CustomGridBrush(false, true, false, "EnergyCoreBrush")]
public class EnergyCoreBrush : ObjectBrushBase
{
    [SerializeField] private EnergyCoreVisual energyCoreVisualPrefab;
    
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Paint(gridLayout, brushTarget, position);

        GameObject energyCoreGroup = LevelGrid.EnergyCores;

        BoundsInt bounds = new (position, TileSetSize);
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (GetObjectInCell(pos, energyCoreGroup.transform) != null)
            {
                return;
            }
        }
        Undo.RegisterFullObjectHierarchyUndo(energyCoreGroup, "Paint DropZoneVisual");

        EnergyCoreVisual energyCoreVisual = Instantiate(energyCoreVisualPrefab, energyCoreGroup.transform, true);
        energyCoreVisual.transform.position = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(position) + Vector3.up);
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Erase(gridLayout, brushTarget, position);

        GameObject energyCoreGroup = LevelGrid.EnergyCores;
        
        BoundsInt bounds = new (position, TileSetSize);

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Transform child = GetObjectInCell(pos, energyCoreGroup.transform);
            if (child != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(energyCoreGroup, "Erase EnergyCore");
                DestroyImmediate(child.gameObject);
                break;
            }
        }
    }
}


[CustomEditor(typeof(EnergyCoreBrush))]
public class EnergyCoreEditorBase : ObjectBrushEditorBase{}
#endif