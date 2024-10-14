#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


[CustomGridBrush(false, true, false, "ExitPointBrush")]
public class ExitZoneBrush : ObjectBrushBase
{
    [SerializeField] private ExitPointVisual exitPointVisualPrefab;
    
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Paint(gridLayout, brushTarget, position);

        GameObject exitPointGroup = LevelGrid.ExitPoints;
        GameObject spawnZoneGroup = LevelGrid.SpawnZones;

        BoundsInt bounds = new (position, TileSetSize);
        
        bool isCombinedZone = false;
        
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (GetObjectInCell(pos, exitPointGroup.transform) != null)
            {
                return;
            }

            int spawnGroupCount = spawnZoneGroup.transform.childCount;
            for (int i = 0; i < spawnGroupCount; i++)
            {
                Transform spawnGroup = spawnZoneGroup.transform.GetChild(i);
                
                isCombinedZone = CheckCombinedZone(pos, spawnGroup);
            }
        }
        
        Undo.RegisterFullObjectHierarchyUndo(exitPointGroup, "Paint ExitZoneVisual");

        ExitPointVisual exitPointVisual = Instantiate(exitPointVisualPrefab, exitPointGroup.transform, true);
        exitPointVisual.transform.position = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(position) + Vector3.up);
        if(isCombinedZone) exitPointVisual.SetCombinedZoneSprite();
    }
    
    private bool CheckCombinedZone(Vector3Int pos, Transform spawnZoneGroup)
    {
        Transform spawnZone = GetObjectInCell(pos, spawnZoneGroup);
            
        if (spawnZone != null)
        {
            SpawnZonePartialVisual szp = spawnZone.GetComponent<SpawnZonePartialVisual>();
            szp.SetCombinedZoneSprite();
            return true;
        }

        return false;
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Erase(gridLayout, brushTarget, position);

        GameObject exitPointGroup = LevelGrid.ExitPoints;
        
        BoundsInt bounds = new (position, TileSetSize);

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Transform child = GetObjectInCell(pos, exitPointGroup.transform);
            if (child != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(exitPointGroup, "Erase ExitZone");
                DestroyImmediate(child.gameObject);
                break;
            }
        }
    }
}

[CustomEditor(typeof(ExitZoneBrush))]
public class ExitZoneBrushEditor: ObjectBrushEditorBase{}
#endif