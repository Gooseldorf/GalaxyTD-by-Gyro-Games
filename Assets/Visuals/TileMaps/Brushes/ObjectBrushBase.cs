#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomGridBrush(true, true, false, "ObjectBrushBase")]
public class ObjectBrushBase : GridBrush
{
   public readonly Vector3Int TileSetSize = new (2, 2, 1);

   public bool DrawTiles = false;
   public List<TileBase> TileSet;

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

   public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
   {
       if(!DrawTiles) return;
       
       Tilemap objects = LevelGrid.Objects;
       if(objects == null) return;
       
       var bounds = new BoundsInt(position + Vector3Int.left, TileSetSize);
        
       int i = 0;
       foreach (var pos in bounds.allPositionsWithin)
       {
           if(objects.GetTile(pos) != null) return;
           objects.SetTile(pos, TileSet[i]);
           i++;
       }
   }

   public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
   {
       if(!DrawTiles) return;
       
       Tilemap objects = LevelGrid.Objects;
        
       if(objects == null) return;
       
       var bounds = new BoundsInt(position + Vector3Int.left, TileSetSize);
        
       foreach (var pos in bounds.allPositionsWithin)
       {
           if(TileSet.Contains(objects.GetTile(pos)))
               objects.SetTile(pos, null);
       }
   }

   private protected Transform GetObjectInCell(Vector3Int position, Transform parent)
   {
       BoundsInt bounds = new (position + Vector3Int.left, TileSetSize);

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

[CustomEditor(typeof(ObjectBrushBase))]
public class ObjectBrushEditorBase : GridBrushEditor
{
    private protected ObjectBrushBase objectBrush => target as ObjectBrushBase;
    
    public override GameObject[] validTargets => !ValidateLevelGrid() ? null : new[] { objectBrush.LevelGrid.Objects.gameObject };

    public override void PaintPreview(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        if (!ValidateLevelGrid())
        {
            return;
        }
        
        Tilemap objects = objectBrush.LevelGrid.Objects;
        if (objects == null) return;
        
        if (objectBrush.TileSet.Count == 1)
        {
            objects.SetEditorPreviewTile(position, objectBrush.TileSet[0]);
            return;
        }
        
        var bounds = new BoundsInt(position + Vector3Int.left, objectBrush.TileSetSize);
        int i = 0;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            objects.SetEditorPreviewTile(pos, objectBrush.TileSet[i]);
            i++;
        }
    }
    
    public override void ClearPreview()
    {
        if (!ValidateLevelGrid())
        {
            return;
        }
        
        objectBrush.LevelGrid.Objects.ClearAllEditorPreviewTiles();
    }
    
    private bool ValidateLevelGrid()
    {
        return objectBrush.LevelGrid != null && objectBrush.LevelGrid.Objects != null;
    }
}
#endif