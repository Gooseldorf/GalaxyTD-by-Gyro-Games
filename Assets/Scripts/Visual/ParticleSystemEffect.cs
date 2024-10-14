using CardTD.Utilities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

public class ParticleSystemEffect : ReferencedVisual
{
    [SerializeField] private bool instantStop = false;

    private IObjectPool<GameObject> tempPool;

    private void OnParticleSystemStopped()
    {
        tempPool.Release(gameObject);
    }

    public void Init(IObjectPool<GameObject> pool, float2 position, float2 direction)
    {
        tempPool = pool;
        if (direction.x == 0 && direction.y == 0)
            transform.position = position.ToFloat3();
        else
        {
            Quaternion rotation = Utilities.Direction2DToQuaternion(direction);
            transform.SetPositionAndRotation(position.ToFloat3(), rotation);
        }
        gameObject.SetActive(true);
        TryPlaySound();
    }
}