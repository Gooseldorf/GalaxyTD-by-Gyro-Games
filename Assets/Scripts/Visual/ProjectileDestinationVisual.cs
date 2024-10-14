using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectileDestinationVisual : ParticleSystemEffect
{
    private const float rangeMultiplier = .9f;
    public ParticleSystem ImpactDestinationPreEffect;

    public void Init(IObjectPool<GameObject> pool, float2 targetPosition, float eta, float range)
    {
        ImpactDestinationPreEffect.transform.localScale = Vector3.one * range * rangeMultiplier;
        ImpactDestinationPreEffect.startLifetime = eta;

        base.Init(pool,targetPosition,0);
    }
}