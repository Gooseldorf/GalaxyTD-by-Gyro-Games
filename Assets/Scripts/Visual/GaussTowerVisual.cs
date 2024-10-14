using DG.Tweening;
using ECSTest.Components;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Pool;

public class GaussTowerVisual : TowerVisual
{
    [SerializeField]
    private Sprite[] barrelSprites;
    [SerializeField]
    private SpriteRenderer barrel;

    private Sequence barrelSeq;

    //[SerializeField, Required] private Animator barrelAnimator;

    //private readonly string barrelSpeedAnimationKey = "speed";
    //private readonly string shootTriggerAnimationKey = "shoot";

    //private float speedMultiplier = 800f;

    //public override void Init(Entity towerEntity, IObjectPool<GameObject> pool, int layoutIndex)
    //{
    //    base.Init(towerEntity, pool, layoutIndex);
    //    barrelAnimator.SetFloat(barrelSpeedAnimationKey, 1);
    //}

    public override void Shoot()
    {
        base.Shoot();

        AttackerComponent attackerComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AttackerComponent>(towerEntity);
        float reloadTime = attackerComponent.AttackStats.ReloadStats.ReloadTime;

        if (barrelSeq != null)
            barrelSeq.Kill();

        SetBarrelSprite(barrelSprites[0]);

        barrelSeq = DOTween.Sequence();
        for (int i = 1; i < barrelSprites.Length; i++)
        {
            int index = i;
            barrelSeq.InsertCallback((i + 1) * (reloadTime / barrelSprites.Length), () => SetBarrelSprite(barrelSprites[index]));
        }
        barrelSeq.OnComplete(() => barrelSeq = null);

        //barrelAnimator.SetFloat(barrelSpeedAnimationKey, 20 / speedMultiplier); //Tower.AttackStats.ReloadStats.ReloadTime
        //barrelAnimator.SetTrigger(shootTriggerAnimationKey);
    }

    private void SetBarrelSprite(Sprite sprite) => barrel.sprite = sprite;

}