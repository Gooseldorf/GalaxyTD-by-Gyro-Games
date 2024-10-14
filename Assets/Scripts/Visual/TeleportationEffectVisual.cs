using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace Visual
{
    public class TeleportationEffectVisual: ParticleSystemEffect
    {
        public void Play(IObjectPool<GameObject> pool, float3 position, Color color)
        {
            var main = GetComponent<ParticleSystem>().main;
            main.startColor = color;

            base.Init(pool, position.xy, 0);
        }
    }
}