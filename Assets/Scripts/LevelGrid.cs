using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGrid : MonoBehaviour
{
    [field: SerializeField] public Tilemap Background { get; private set; }
    [field: SerializeField] public Tilemap BackgroundEffects { get; private set; }
    [field: SerializeField] public Tilemap Floor { get; private set; }
    [field: SerializeField] public Tilemap Walls { get; private set; }
    [field: SerializeField] public Tilemap Objects { get; private set; }
    
    [field: SerializeField] public GameObject SpawnZones { get; private set; }
    [field: SerializeField] public GameObject ExitPoints { get; private set; }
    [field: SerializeField] public GameObject DropZones { get; private set; }
    [field: SerializeField] public GameObject Portals { get; private set; }
    [field: SerializeField] public GameObject EnergyCores { get; private set; }
    [field: SerializeField] public GameObject Bridges { get; private set; }
    [field: SerializeField] public GameObject Gates { get; private set; }
    [field: SerializeField] public GameObject CritterSpawnPoints { get; private set; }
    [field: SerializeField] public GameObject Conveyors { get; private set; }
    [field: SerializeField] public GameObject PowerSwitches { get; private set; }

    
    public bool ShowObstacles = false;
    public Mission CurrentMission;
    
    public void Clear()
    {
        Background.ClearAllTiles();
        BackgroundEffects.ClearAllTiles();
        Floor.ClearAllTiles();
        Walls.ClearAllTiles();
        Objects.ClearAllTiles();
        
        while (SpawnZones.transform.childCount > 0)
            DestroyImmediate(SpawnZones.transform.GetChild(0).gameObject);
        while (ExitPoints.transform.childCount > 0)
            DestroyImmediate(ExitPoints.transform.GetChild(0).gameObject);
        while (DropZones.transform.childCount > 0)
            DestroyImmediate(DropZones.transform.GetChild(0).gameObject);
        while (Portals.transform.childCount > 0)
            DestroyImmediate(Portals.transform.GetChild(0).gameObject);
        while (EnergyCores.transform.childCount > 0)
            DestroyImmediate(EnergyCores.transform.GetChild(0).gameObject);
        while (Bridges.transform.childCount > 0)
            DestroyImmediate(Bridges.transform.GetChild(0).gameObject);
        while (Gates.transform.childCount > 0)
            DestroyImmediate(Gates.transform.GetChild(0).gameObject);
        while (CritterSpawnPoints.transform.childCount > 0)
            DestroyImmediate(CritterSpawnPoints.transform.GetChild(0).gameObject);
        while (Conveyors.transform.childCount > 0)
            DestroyImmediate(Conveyors.transform.GetChild(0).gameObject);
        while (PowerSwitches.transform.childCount > 0)
            DestroyImmediate(PowerSwitches.transform.GetChild(0).gameObject);

    }

    public void Compress()
    {
        Walls.CompressBounds();
        Background.CompressBounds();
        BackgroundEffects.CompressBounds();
        Floor.CompressBounds();
        Objects.CompressBounds();
    }

    public Vector2Int GetGridSize()
    {
        Tilemap[] tilemaps = { Background, BackgroundEffects, Objects, Walls, Floor };
        
        Vector2Int minTile = new (int.MaxValue, int.MaxValue);
        Vector2Int maxTile = new (int.MinValue, int.MinValue);

        foreach (Tilemap tilemap in tilemaps)
        {
            BoundsInt bounds = tilemap.cellBounds;
            Vector3Int minCell = bounds.min, maxCell = bounds.max;

            minTile.x = Mathf.Min(minTile.x, minCell.x);
            minTile.y = Mathf.Min(minTile.y, minCell.y);
            maxTile.x = Mathf.Max(maxTile.x, maxCell.x);
            maxTile.y = Mathf.Max(maxTile.y, maxCell.y);
        }

        Vector2Int gridSize = maxTile - minTile + Vector2Int.one;
        return gridSize;
    }
    
    public Vector3Int GetOriginOffset()
    {
        int negX = int.MaxValue;
        int negY = int.MaxValue;
        int posX = int.MaxValue;
        int posY = int.MaxValue;
    
        List<Tilemap> tilemaps = new (){ Background, BackgroundEffects, Walls, Floor };

        for (int i = 0; i < tilemaps.Count; i++)
        {
            if (tilemaps[i].GetUsedTilesCount() == 0)
            {
                tilemaps.Remove(tilemaps[i]);
            }
        }

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap.origin.x <= 0)
                negX = Math.Min(negX, tilemap.origin.x);
            else
                posX = Math.Min(posX, tilemap.origin.x);
            
            if(tilemap.origin.y <= 0)
                negY = Math.Min(negY, tilemap.origin.y);
            else
                posY = Math.Min(posY, tilemap.origin.y);
        }
        
        int finalX = negX != int.MaxValue ? Math.Abs(negX) : -posX;
        int finalY = negY != int.MaxValue ? Math.Abs(negY) : -posY;

        if (finalX == - int.MaxValue)
            finalX = 0;
        
        if (finalY == - int.MaxValue)
            finalY = 0;

        if (Floor.origin.x + finalX < 1 || Floor.origin.y + finalY < 1)
        {
            finalX += 1;
            finalY += 1;
        }
        
        return new Vector3Int(finalX, finalY, 0);
    }
    
    private void OnDrawGizmos()
    {
        if(!ShowObstacles || CurrentMission == null) return;
        
        foreach (IObstacle obst in CurrentMission.Obstacles)
        {
            if (obst is ISquareObstacle square)
            {
                Vector3 pos = new 
                (
                    (square.Points[0].x + square.Points[2].x) / 2,
                    (square.Points[0].y + square.Points[1].y) / 2,
                    0
                );
                Vector3 scale = new Vector3((square.Points[2].x - pos.x) * 2,
                    (square.Points[1].y - pos.y) * 2, 1);
                Gizmos.color = new Color(1,0,0,0.3f);
                Gizmos.DrawCube(pos, scale);
            }
        }
    }
}
