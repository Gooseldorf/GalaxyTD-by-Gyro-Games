#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;


[CustomGridBrush(false, true, false, "SpawnZoneBrush")]
public class SpawnZoneBrush : ObjectBrushBase
{
    [SerializeField] private SpawnGroupVisual spawnGroupVisualPrefab;
    [SerializeField] private SpawnZonePartialVisual spawnZonePartialVisualPrefab;
    private SpawnGroupVisual currentSpawnGroupVisual;

    public int SpawnGroupIndex;
    
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Paint(gridLayout, brushTarget, position);
        
        GameObject spawnZoneGroup = LevelGrid.SpawnZones;
        GameObject exitPointGroup = LevelGrid.ExitPoints;

        currentSpawnGroupVisual = GetSpawnGroupVisual(spawnZoneGroup.transform);
        
        BoundsInt bounds = new (position, TileSetSize);
        
        bool isCombinedZone = false;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            int spawnGroupCount = spawnZoneGroup.transform.childCount;
            for (int i = 0; i < spawnGroupCount; i++)
            {
                Transform spawnGroup = spawnZoneGroup.transform.GetChild(i);
              
                if(GetObjectInCell(pos, spawnGroup) != null)
                {
                   return;
                }
            }
            
            isCombinedZone = CheckCombinedZone(pos, exitPointGroup.transform);
        }

        Undo.RegisterFullObjectHierarchyUndo(spawnZoneGroup, "Paint SpawnZonePartial");

        SpawnZonePartialVisual spawnZonePartialVisual = Instantiate(spawnZonePartialVisualPrefab, currentSpawnGroupVisual.transform, true);
        spawnZonePartialVisual.transform.position = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(position) + Vector3.up);
        if(isCombinedZone) spawnZonePartialVisual.SetCombinedZoneSprite();
    }

    private bool CheckCombinedZone(Vector3Int pos, Transform exitPointGroup)
    {
        Transform exitPoint = GetObjectInCell(pos, exitPointGroup);
            
        if (exitPoint != null)
        {
            ExitPointVisual epv = exitPoint.GetComponent<ExitPointVisual>();
            epv.SetCombinedZoneSprite();
            return true;
        }

        return false;
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        base.Erase(gridLayout, brushTarget, position);

        SpawnGroupVisual spawnGroupVisual = GetSpawnGroupVisual(LevelGrid.SpawnZones.transform);
        
        BoundsInt bounds = new (position, TileSetSize);

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Transform child = GetObjectInCell(pos, spawnGroupVisual.transform);
            if (child != null)
            {
                Transform parentTransform = spawnGroupVisual.transform;
                Undo.RegisterFullObjectHierarchyUndo(parentTransform.gameObject, "Erase SpawnZonePartial");
                DestroyImmediate(child.gameObject);
                break;
            }
        }
    }
    
    private SpawnGroupVisual GetSpawnGroupVisual(Transform spawnZoneGroup)
    {
        var spawnGroupVisuals = spawnZoneGroup.GetComponentsInChildren<SpawnGroupVisual>();
        var result = spawnGroupVisuals.FirstOrDefault(x => x.Id == SpawnGroupIndex);
        
        if (result != null) return result;
        
        result = Instantiate(spawnGroupVisualPrefab, spawnZoneGroup);
        result.Id = SpawnGroupIndex;

        return result;
    }
}

[CustomEditor(typeof(SpawnZoneBrush))]
public class SpawnAreaBrushEditor : ObjectBrushEditorBase { }
#endif