using UnityEngine;

public class CritterSpawnPointVisual : EnvironmentVisual
{
    public CritterStats CritterStats;
    //private Vector3 offset = new (0.5f, 0.5f);

    public override void InitVisual(object data)
    {
        base.InitVisual(data);
        CritterStats = ((CritterSpawnPoint)data).CritterStats;
        InitPosition(data as CritterSpawnPoint);
        //transform.position += offset;
    }

    public CritterSpawnPoint GetData()
    {
        return new CritterSpawnPoint()
        {
            GridPos = GridPosition, 
            CritterStats = this.CritterStats,
            GridSize = this.GridSize,
        };
    }
}
