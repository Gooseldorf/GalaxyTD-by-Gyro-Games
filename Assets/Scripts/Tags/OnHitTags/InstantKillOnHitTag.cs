using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class InstantKillOnHitTag : OnHitTag
{
    [SerializeField] private float damagePercentFromHpThreshold = 0.05f;
    [SerializeField, BoxGroup("Bounds")] private int minAmmoBound = 5;
    [SerializeField, BoxGroup("Bounds")] private int maxAmmoBound = 100;
    [SerializeField, BoxGroup("Bounds")] private float minPercentBound = 0.5f;
    [SerializeField, BoxGroup("Bounds")] private float maxPercentBound = 1.5f;

    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        float damagePercentFromHp = damage / creepComponent.MaxHp;
        if (damagePercentFromHp < damagePercentFromHpThreshold) return;

        // if shotgun imba => * attackerComponent.AttackStats.ShootingStats.ProjectilesPerShot
        float instantKillChance = 100 * Utilities.GetLerpedValue(minAmmoBound, maxAmmoBound, maxPercentBound, minPercentBound, attackerComponent.AttackStats.ReloadStats.MagazineSize);
        float rand = Random.Range(0f, 100f);

        if (rand <= instantKillChance)
        {
            if(!manager.Exists(onHitTagData.CreepEntity))
                return;
            
            PositionComponent positionComponent = manager.GetComponentData<PositionComponent>(onHitTagData.CreepEntity);
            DamageSystem.DoInstantKill(ref creepComponent, positionComponent.Position, ecb);
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/InstantKill")
                                                .Replace("{param}", ((int)(damagePercentFromHpThreshold * 100)) + "<color=#1fb2de>%</color>");
}