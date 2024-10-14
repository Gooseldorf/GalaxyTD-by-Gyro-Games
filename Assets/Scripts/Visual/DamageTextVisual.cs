using Unity.Mathematics;
using UnityEngine;

public sealed class DamageTextVisual : ReferencedVisual
{
    public IPosition UnitToFollow;
    public float3 StartPosition;

    private const float percent = 0.5f;

    private void Update()
    {
        if (UnitToFollow != null)
        {
            transform.position += (Vector3)(UnitToFollow.Position - StartPosition) * percent;
            StartPosition = UnitToFollow.Position;
        }
    }
}
