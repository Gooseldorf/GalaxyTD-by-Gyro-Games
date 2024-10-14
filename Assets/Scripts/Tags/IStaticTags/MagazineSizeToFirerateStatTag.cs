using I2.Loc;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public sealed class MagazineSizeToFirerateStatTag : Tag, IStaticTag
{
    [SerializeField, InfoBox("Percents, where 100% is 1. For reduction use minus")] private float magazineReduction;
    [SerializeField, InfoBox("Increase for bullets diff between old and new magazine size")] private float fireRatePerBullet;
    
    [OdinSerialize] public int OrderId { get; set; } 

    public void ApplyStats(Tower tower)
    {
        int currentMagazine = (int)tower.AttackStats.ReloadStats.RawMagazineSize;
        tower.AttackStats.ReloadStats.RawMagazineSize += tower.AttackStats.ReloadStats.RawMagazineSize * magazineReduction;

        int bulletsLost = currentMagazine - (int)tower.AttackStats.ReloadStats.RawMagazineSize;
        float firerateIncrease;
        
        firerateIncrease = 1 + bulletsLost * fireRatePerBullet;
        
        tower.AttackStats.ShootingStats.ShotDelay /= firerateIncrease;
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/MagazineSizeToFirerate")
                                                .Replace("{param}", (fireRatePerBullet * 100).ToString() + "<color=#1fb2de>%</color>");
}