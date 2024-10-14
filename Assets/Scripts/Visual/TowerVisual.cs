using ECSTest.Components;
using Sirenix.OdinInspector;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using DG.Tweening;
using CardTD.Utilities;
using UnityEngine.Pool;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;

public class TowerVisual : ReferencedVisual, IPowerableVisual
{
    [SerializeField] private ReloadBar reloadBar;
    [SerializeField, Required] protected Transform head;
    [SerializeField] protected Transform barrelShaking;

    protected Entity towerEntity;
    private IObjectPool<GameObject> pool;

    [SerializeField] private Vector3 defaultBarrelPos;
    [SerializeField] private SpriteRenderer rankIcon;
    [SerializeField] private SpriteRenderer turnOffIcon;

    public float ShakeStrength;
    public float shakeDuration = .2f; //=> Tower.AttackStats.ShootingStats.ShotDelay * .5f;
    private Sequence barrelSequence;

    [SerializeField, InfoBox("Sort this from lower to upper => for sortingOrder")]
    private List<SpriteRenderer> towerParts = new List<SpriteRenderer>();

    public IReadOnlyList<SpriteRenderer> TowerParts => towerParts;
    [SerializeField] private Material[] defaultMaterial;
    [SerializeField] private Material[] buildMaterial;
    [SerializeField] private Material[] offMaterial;
    private const float buildDuration = 0.5f;

    [SerializeField] private ParticleSystem lvlupNormalEffect;
    [SerializeField] private ParticleSystem lvlupRankEffect;
    private int towerLevel;
    private bool noCashForReloadTutorToShow = false;

    public int Id { get; set; }
    public bool IsPowered { get; set; }

    public void TogglePower() => SetPowered(!IsPowered);

    public void SetPowered(bool isPowered)
    {
        IsPowered = isPowered;
        //TODO: Toggle power effect
    }

    public virtual void Init(Entity tower, IObjectPool<GameObject> objectPool)
    {
        towerLevel = 0;

        rankIcon.gameObject.SetActive(false);
        turnOffIcon.gameObject.SetActive(false);

        barrelSequence = DOTween.Sequence();
        gameObject.SetActive(true);
        towerEntity = tower;
        pool = objectPool;

        reloadBar.UpdateAmmoCount(1);

        AttackerComponent attackerComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AttackerComponent>(towerEntity);
        bool isActive = attackerComponent.AttackPattern != AllEnums.AttackPattern.Off;
        turnOffIcon.gameObject.SetActive(!isActive);

        SetStartVisual();
        float hologramBlend = .001f;
        float fadeAmount = .01f;
        for (int i = 0; i < towerParts.Count; i++)
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(buildMaterial[i].DOFloat(hologramBlend, "_HologramBlend", buildDuration).SetEase(Ease.Linear));
            seq.Insert(0, buildMaterial[i].DOFloat(fadeAmount, "_FadeAmount", buildDuration).SetEase(Ease.Linear));
            int index = i;
            seq.OnComplete(() => SetEndVisual(index, isActive));
            seq.SetUpdate(true);
        }

        noCashForReloadTutorToShow = TutorialManager.Instance.HasCurrentTutorial(TutorialKeys.NoCashForReload);
    }

    private void SetStartVisual()
    {
        // some visual magic values
        float hologramBlend = .5f;
        float fadeAmount = 1f;
        float grayscale = 0;
        for (int i = 0; i < towerParts.Count; i++)
        {
            towerParts[i].material = buildMaterial[i];
            buildMaterial[i].SetFloat("_HologramBlend", hologramBlend);
            buildMaterial[i].SetFloat("_FadeAmount", fadeAmount);
            buildMaterial[i].SetFloat("_GreyscaleBlend", grayscale);
        }
    }

    private void SetEndVisual(int index, bool isActive)
    {
        towerParts[index].material = isActive ? defaultMaterial[index] : offMaterial[index];
    }

    private void Awake()
    {
        Messenger<Entity>.AddListener(GameEvents.TowerReload, ShowReloadAnimation);
        Messenger<Entity>.AddListener(GameEvents.TowerUpdated, OnTowerUpdated);
        Messenger.AddListener(GameEvents.Restart, OnRestart);
    }

    private void OnDestroy()
    {
        Messenger<Entity>.RemoveListener(GameEvents.TowerReload, ShowReloadAnimation);
        Messenger<Entity>.RemoveListener(GameEvents.TowerUpdated, OnTowerUpdated);
        Messenger.RemoveListener(GameEvents.Restart, OnRestart);
    }

    private void OnRestart() => ReleaseToPool();

    public void Release()
    {
        for (int i = 0; i < towerParts.Count; i++)
        {
            towerParts[i].material = buildMaterial[i];

            Sequence seq = DOTween.Sequence();
            seq.Append(buildMaterial[i].DOFloat(.5f, "_HologramBlend", buildDuration).SetEase(Ease.Linear));
            seq.Insert(0, buildMaterial[i].DOFloat(1f, "_FadeAmount", buildDuration).SetEase(Ease.Linear));
            seq.OnComplete(() => ReleaseToPool());
            seq.SetUpdate(true);
        }
    }


    private void ReleaseToPool()
    {
        reloadBar.HideReload();
        reloadBar.ShowNoCash(false);
        pool?.Release(gameObject);
        pool = null;
    }

    public void RotateVisual(float2 direction)
    {
        head.localRotation = Utilities.Direction2DToQuaternion(direction);
    }

    [Button]
    public virtual void SaveBarrelPos() => defaultBarrelPos = barrelShaking.transform.localPosition;

    public virtual void Shoot()
    {
        if (barrelShaking.localPosition == defaultBarrelPos)
            ShakeBarrel();
    }

    private void ShakeBarrel()
    {
        if (barrelSequence.IsActive())
            return;

        barrelSequence = DOTween.Sequence();
        barrelSequence.Append(barrelShaking.DOLocalMoveY(defaultBarrelPos.y - ShakeStrength, 0));
        barrelSequence.Append(barrelShaking.DOLocalMove(defaultBarrelPos, shakeDuration / 2)
            .SetDelay(shakeDuration / 3));
        barrelSequence.Play().OnComplete(() => barrelSequence.Kill());
    }

    public void ShowReloadAnimation(Entity entity)
    {
        if (entity == towerEntity)
        {
            AttackerComponent attackerComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AttackerComponent>(towerEntity);
            reloadBar.ShowReload(attackerComponent.ReloadTimer);
        }
    }

    private void OnTowerUpdated(Entity tower)
    {
        if (tower == Entity.Null)
            return;

        if (tower != towerEntity)
            return;

        if (!World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<AttackerComponent>(tower))
            return;

        AttackerComponent attackerComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AttackerComponent>(tower);

        //update rank icon
        if (towerLevel != attackerComponent.Level)
        {
            towerLevel = attackerComponent.Level;

            if (towerLevel == 4 || towerLevel == 9 || towerLevel == 14)
                lvlupRankEffect.Play(true);
            else
                lvlupNormalEffect.Play(true);

            if (UI.UIHelper.Instance.GetRankSprite(towerLevel) is Sprite rankSprite && rankSprite != null)
            {
                if (rankIcon.sprite != rankSprite)
                    rankIcon.sprite = rankSprite;
                if (!rankIcon.gameObject.activeSelf)
                    rankIcon.gameObject.SetActive(true);
            }
            else
            {
                if (rankIcon.gameObject.activeSelf)
                    rankIcon.gameObject.SetActive(false);
            }

            reloadBar.HideReload();
        }

        //update ammo count
        reloadBar.UpdateAmmoCount(attackerComponent.Bullets / (float)attackerComponent.AttackStats.ReloadStats.MagazineSize);

        //update "off" state
        if (attackerComponent.AttackPattern == AllEnums.AttackPattern.Off && !turnOffIcon.gameObject.activeSelf)
        {
            for (int i = 0; i < towerParts.Count; i++)
                towerParts[i].material = offMaterial[i];

            turnOffIcon.gameObject.SetActive(true);
        }

        if (attackerComponent.AttackPattern != AllEnums.AttackPattern.Off && turnOffIcon.gameObject.activeSelf)
        {
            for (int i = 0; i < towerParts.Count; i++)
                towerParts[i].material = defaultMaterial[i];

            turnOffIcon.gameObject.SetActive(false);
        }

        //update no cash
        bool showNoCash = attackerComponent.Bullets <= 0 && attackerComponent.AttackPattern != AllEnums.AttackPattern.Off;
        reloadBar.ShowNoCash(showNoCash);
        if (noCashForReloadTutorToShow && showNoCash)
            Messenger<TowerVisual>.Broadcast(TutorialKeys.NoCashForReload, this, MessengerMode.DONT_REQUIRE_LISTENER);
    }

    private void OnDrawGizmosSelected()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return;

        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (!manager.Exists(towerEntity))
            return;

        Gizmos.color = Color.green;

        var squareObstacle = manager.GetComponentData<SquareObstacle>(towerEntity);
        var points = squareObstacle.GetPoints();
        for (int i = 0; i < points.Length; i++)
        {
            int nextPoint = (i + 1) % points.Length;
            Gizmos.DrawLine(new float3(points[i], 0), new float3(points[nextPoint], 0));
        }

        var attackerComponent = manager.GetComponentData<AttackerComponent>(towerEntity);
        var positionComponent = manager.GetComponentData<PositionComponent>(towerEntity);
        float3 position = new Vector3(positionComponent.Position.x, positionComponent.Position.y, 0);
        float3 direction = new float3(positionComponent.Direction.x, positionComponent.Direction.y, 0);

        // if (attackerComponent.Targets.IsCreated && !attackerComponent.Targets.IsEmpty)
        if (manager.Exists(attackerComponent.Target))
        {
            Gizmos.color = Color.yellow;

            // var targetPositionComponent = manager.GetComponentData<PositionComponent>(attackerComponent.Targets[0]);
            // var targetRoundObstacle = manager.GetComponentData<RoundObstacle>(attackerComponent.Targets[0]);

            var targetPositionComponent = manager.GetComponentData<PositionComponent>(attackerComponent.Target);
            var targetRoundObstacle = manager.GetComponentData<RoundObstacle>(attackerComponent.Target);

            float3 center = new(targetPositionComponent.Position.x, targetPositionComponent.Position.y, 0);
            float range = targetRoundObstacle.Range;
            float3 point;
            float3 point2;
            // Draw square
            point = center + new float3(-0.5f, -0.5f, 0) * range;
            point2 = center + new float3(-0.5f, 0.5f, 0) * range;
            Gizmos.DrawLine(point, point2);
            point = center + new float3(0.5f, 0.5f, 0) * range;
            Gizmos.DrawLine(point2, point);
            point2 = center + new float3(0.5f, -0.5f, 0) * range;
            Gizmos.DrawLine(point, point2);
            point = center + new float3(-0.5f, -0.5f, 0) * range;
            Gizmos.DrawLine(point, point2);

            if (Quaternion.Angle(
                    Quaternion.LookRotation(
                        new Vector3(positionComponent.Direction.x, positionComponent.Direction.y, 0)),
                    Quaternion.LookRotation(center - position)) <=
                attackerComponent.AttackStats.AimingStats.AttackAngle)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.yellow;


            //Draw diagonals
            point = center + new float3(-0.5f, -0.5f, 0) * range;
            point2 = center + new float3(0.5f, 0.5f, 0) * range;
            Gizmos.DrawLine(point, point2);
            point = center + new float3(-0.5f, 0.5f, 0) * range;
            point2 = center + new float3(0.5f, -0.5f, 0) * range;
            Gizmos.DrawLine(point, point2);
        }

        Debug.DrawRay(position,
            Quaternion.AngleAxis(attackerComponent.AttackStats.AimingStats.AttackAngle, Vector3.forward) * direction *
            20,
            Color.blue);
        Debug.DrawRay(position,
            Quaternion.AngleAxis(-attackerComponent.AttackStats.AimingStats.AttackAngle, Vector3.forward) * direction *
            20,
            Color.blue);
    }
}