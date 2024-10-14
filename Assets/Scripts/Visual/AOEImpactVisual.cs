using Unity.Mathematics;
using UnityEngine.Pool;
using UnityEngine;

public class AOEImpactVisual : ParticleSystemEffect
{
    public float ScaleMultiplier = .5f;

    public void Init(IObjectPool<GameObject> pool, float2 position, float aoeScale)
    {
        transform.localScale = Vector3.one * aoeScale * ScaleMultiplier;
        base.Init(pool, position, 0);
    }
}