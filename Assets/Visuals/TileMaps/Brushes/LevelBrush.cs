#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomGridBrush(false, true, false, "LevelBrush")]
public class LevelBrush : RandomBrush
{
    public TileBase WallTile;
    
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

    internal struct SizeEnumerator : IEnumerator<Vector3Int>
    {
        private readonly Vector3Int min, max, delta;
        private Vector3Int current;

        public SizeEnumerator(Vector3Int min, Vector3Int max, Vector3Int delta)
        {
            this.min = current = min;
            this.max = max;
            this.delta = delta;
            Reset();
        }

        public LevelBrush.SizeEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (current.z >= max.z)
                return false;

            current.x += delta.x;
            if (current.x >= max.x)
            {
                current.x = min.x;
                current.y += delta.y;
                if (current.y >= max.y)
                {
                    current.y = min.y;
                    current.z += delta.z;
                    if (current.z >= max.z)
                        return false;
                }
            }
            return true;
        }

        public void Reset()
        {
            current = min;
            current.x -= delta.x;
        }

        public Vector3Int Current { get { return current; } }

        object IEnumerator.Current { get { return Current; } }

        void IDisposable.Dispose() {}
    }
    
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        Tilemap floor = LevelGrid.Floor;
        Tilemap walls = LevelGrid.Walls;

        if (floor == null || walls == null || floor.GetTile(position) != null) return;
        
        Undo.RegisterCompleteObjectUndo(floor, "Paint");
        Undo.RegisterCompleteObjectUndo(walls, "Paint");
           
        if (randomTileChangeDataSets != null && randomTileChangeDataSets.Length > 0)
        {
            if (brushTarget == null)
                return;
            
            var min = position - pivot;
            
            foreach (var startLocation in new SizeEnumerator(min, min + size, randomTileSetSize))
            {
                var randomTileChangeDataSet = randomTileChangeDataSets[(int)(randomTileChangeDataSets.Length * UnityEngine.Random.value)];
                var randomBounds = new BoundsInt(startLocation, randomTileSetSize);
                int i = 0;
                foreach (var pos in randomBounds.allPositionsWithin)
                {
                    if(floor.GetTile(pos) != null) return;
                    if(walls.GetTile(pos) != null) walls.SetTile(pos, null);
                    randomTileChangeDataSet.randomTileChangeData[i++].position = pos;
                }
                
                floor.SetTiles(randomTileChangeDataSet.randomTileChangeData, false);

                Vector3Int wallsMin = new Vector3Int(startLocation.x - 1, startLocation.y - 1, 0);
                Vector3Int wallsSize = new Vector3Int(randomTileSetSize.x * 2, randomTileSetSize.y * 2, 1);
                
                var bounds = new BoundsInt(wallsMin, wallsSize);
                
                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (floor.GetTile(pos) == null && !randomBounds.Contains(pos))
                    {
                        walls.SetTile(pos, WallTile);
                    }
                }
            }
        }
        else
        {
            base.Paint(gridLayout, brushTarget, position);
        }
    }

    public override void BoxErase(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
    {
        Tilemap floor = LevelGrid.Floor;
        Tilemap walls = LevelGrid.Walls;

        if (floor == null || walls == null) return;
        
        Undo.RegisterCompleteObjectUndo(floor, "Paint");
        Undo.RegisterCompleteObjectUndo(walls, "Paint");
        
        foreach (var pos in position.allPositionsWithin)
        {
            floor.SetTile(pos, null);
            walls.SetTile(pos, null);
        }
    }
}

[CustomEditor(typeof(LevelBrush))]
public class LevelBrushEditor : RandomBrushEditor
{
    private LevelBrush levelBrush => target as LevelBrush;
    
    public override GameObject[] validTargets => !ValidateLevelGrid() ? null : new[] { levelBrush.LevelGrid.Objects.gameObject };

    public override void PaintPreview(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        if (!ValidateLevelGrid())
        {
            ClearPreview();
            return;
        }
        base.PaintPreview(grid, brushTarget, position);
    }
    
    public override void ClearPreview()
    {
        if(!ValidateLevelGrid()) return;
        
        levelBrush.LevelGrid.Objects.ClearAllEditorPreviewTiles();
    }
    
    private bool ValidateLevelGrid()
    {
        return levelBrush.LevelGrid != null && levelBrush.LevelGrid.Walls != null && levelBrush.LevelGrid.Floor != null;
    }
}
#endif