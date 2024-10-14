using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class RemovePenetrationRicochetForDamageTag : Tag, IStaticTag
{
    [SerializeField, EnumToggleButtons] private RemoveType removeType;
    [SerializeField, InfoBox("Damage increase percent for every removed")] private float increasePercent = 0.25f;

    [OdinSerialize] public int OrderId { get; set; }

    public void ApplyStats(Tower tower)
    {
        if (tower.AttackStats is not GunStats stats) return;

        switch (removeType)
        {
            case RemoveType.Penetration:
                int penetrationCount = stats.RicochetStats.PenetrationCount;
                stats.RicochetStats.PenetrationCount = 0;
                stats.DamagePerBullet += stats.DamagePerBullet * increasePercent * penetrationCount;
                break;
            case RemoveType.Ricochet:
                int ricochetCount = stats.RicochetStats.RicochetCount;
                stats.RicochetStats.RicochetCount = 0;
                stats.DamagePerBullet += stats.DamagePerBullet * increasePercent * ricochetCount;
                break;
        }
    }

    public void ApplyStats(Entity towerEntity, EntityManager manager)
    {
        if (manager.HasComponent<GunStatsComponent>(towerEntity))
        {
            var gunStats = manager.GetComponentData<GunStatsComponent>(towerEntity);
            var attacker = manager.GetComponentData<AttackerComponent>(towerEntity);
            
            switch (removeType)
            {
                case RemoveType.Penetration:
                    int penetrationCount = gunStats.PenetrationCount;
                    gunStats.PenetrationCount = 0;
                    attacker.AttackStats.DamagePerBullet += attacker.AttackStats.DamagePerBullet * increasePercent * penetrationCount;
                    break;
                case RemoveType.Ricochet:
                    int ricochetCount = gunStats.RicochetCount;
                    gunStats.RicochetCount = 0;
                    attacker.AttackStats.DamagePerBullet += attacker.AttackStats.DamagePerBullet * increasePercent * ricochetCount;
                    break;
            }
            
            manager.SetComponentData(towerEntity,attacker);
            manager.SetComponentData(towerEntity,gunStats);
        }

        
    }

    public override string GetDescription()
    {
        switch (removeType)
        {
            case RemoveType.Penetration:
                return LocalizationManager.GetTranslation("Tags/RemovePenetrationForDamage")
                    .Replace("{param}", (increasePercent > 0 ? "+" : "") + Mathf.RoundToInt(increasePercent * 100) + "<color=#1fb2de>%</color>");
            case RemoveType.Ricochet:
            default:
                return LocalizationManager.GetTranslation("Tags/RemoveRicochetForDamage")
                    .Replace("{param}", (increasePercent > 0 ? "+" : "") + Mathf.RoundToInt(increasePercent * 100) + "<color=#1fb2de>%</color>");
        }
    }
    private enum RemoveType { Penetration, Ricochet }
}