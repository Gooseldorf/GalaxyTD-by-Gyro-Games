using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectileVisual : ReferencedVisual
{
    [SerializeField] protected GameObject model;
    [SerializeField] protected ParticleSystem trail;

    public void Hide(IObjectPool<GameObject> poolToRelease)
    {
        if (model != null && model.activeInHierarchy)
        {
            StartCoroutine(HideCoroutine(poolToRelease));
        }
    }

    protected virtual IEnumerator HideCoroutine(IObjectPool<GameObject> poolToRelease)
    {
        if (model != null)
            model.SetActive(false);
        if (trail != null)
            yield return new WaitForSeconds(trail.main.startLifetime.constant);
        if (trail != null)
        {
            trail.Stop();
            trail.gameObject.SetActive(false);
        }

        poolToRelease.Release(gameObject);
    }

    public void OnEnable()
    {
        if (model != null)
            model.SetActive(true);
        if (trail != null)
        {
            trail.gameObject.SetActive(true);
            trail.Play();
        }
    }
}