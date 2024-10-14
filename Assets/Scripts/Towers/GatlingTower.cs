using Unity.Mathematics;

public class GatlingTower : GunTower, IAttacker
{
    private float barrelWidthOffset = .2f;

    float3 IAttacker.GetProjectilePosition(int i)
    {
        float3 offset = UnityEngine.Random.Range(-barrelWidthOffset, barrelWidthOffset) * math.cross(Direction, UnityEngine.Vector3.forward);
        return Position + offset;
    }
}