using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

[CustomGridBrush(false, true, false, "CritterSpawnPointBrush")]
public class CritterSpawnBrush : ObjectBrushBase
{
    [SerializeField] private CritterSpawnPointVisual critterSpawnPointVisualPrefab;
    
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Paint(gridLayout, brushTarget, position);

        GameObject critterGroup = LevelGrid.CritterSpawnPoints;

        BoundsInt bounds = new BoundsInt(position, TileSetSize);
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (GetObjectInCell(pos, critterGroup.transform) != null)
            {
                return;
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(critterGroup, "Paint DropZoneVisual");

        CritterSpawnPointVisual critterSpawnPointVisual = Instantiate(critterSpawnPointVisualPrefab, critterGroup.transform, true);
        critterSpawnPointVisual.transform.position = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(position) + new Vector3(0.5f, 0.5f));
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Erase(gridLayout, brushTarget, position);

        GameObject critterGroup = LevelGrid.CritterSpawnPoints;
        
        BoundsInt bounds = new (position, TileSetSize);

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Transform child = GetObjectInCell(pos, critterGroup.transform);
            if (child != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(critterGroup, "Erase CritterSpawnPoint");
                DestroyImmediate(child.gameObject);
                break;
            }
        }
    }
}

[CustomEditor(typeof(CritterSpawnBrush))]
public class CritterSpawnBrushEditor : ObjectBrushEditorBase { }
#endif
