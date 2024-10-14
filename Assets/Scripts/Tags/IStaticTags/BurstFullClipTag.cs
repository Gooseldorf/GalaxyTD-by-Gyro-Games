using I2.Loc;
using Sirenix.Serialization;

public sealed class BurstFullClipTag : Tag, IStaticTag
{
    [OdinSerialize]
    public int OrderId { get; set; }

    public void ApplyStats(Tower tower)
    {
        tower.AttackStats.ShootingStats.ShotsPerBurst = tower.AttackStats.ReloadStats.MagazineSize;
        tower.AttackStats.ShootingStats.AvailableAttackPatterns = AllEnums.AttackPattern.Off | AllEnums.AttackPattern.Burst;
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/BurstFullClip");
}
