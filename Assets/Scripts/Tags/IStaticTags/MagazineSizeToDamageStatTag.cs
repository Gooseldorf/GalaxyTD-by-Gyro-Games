using I2.Loc;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public sealed class MagazineSizeToDamageStatTag : Tag, IStaticTag
{
    [SerializeField, InfoBox("Percents, where 100% is 1. For reduction use minus")] private float magazineReduction;
    [SerializeField, InfoBox("Increase for bullets fired")] private float bulletDamagePerBullet;
    
    [OdinSerialize] public int OrderId { get; set; }
    
    public void ApplyStats(Tower tower)
    {
        int currentMagazine = (int)tower.AttackStats.ReloadStats.RawMagazineSize;
        tower.AttackStats.ReloadStats.RawMagazineSize += tower.AttackStats.ReloadStats.RawMagazineSize * magazineReduction;

        int bulletsLost = currentMagazine - (int)tower.AttackStats.ReloadStats.RawMagazineSize;
        float bulletDamageIncrease = 1 + bulletsLost * bulletDamagePerBullet;
        tower.AttackStats.DamagePerBullet *= bulletDamageIncrease;
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/MagazineSizeToDamage")
                                                .Replace("{param}", (bulletDamagePerBullet * 100).ToString() + "<color=#1fb2de>%</color>");
}