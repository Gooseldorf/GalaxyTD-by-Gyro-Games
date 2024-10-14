using Unity.Mathematics;

public class TwinTower : GunTower, IAttacker
{
    private float barrelWidthOffset = .2f;

    float3 IAttacker.GetProjectilePosition(int i)
    {
        float3 offset = barrelWidthOffset * math.cross(Direction, UnityEngine.Vector3.forward);
        return i == 0 ? Position + offset: Position - offset;
    }
}