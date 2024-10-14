using I2.Loc;
using Sirenix.Serialization;

public sealed class NoDamageLossOnPenetrationTag : Tag, IStaticTag
{
    [OdinSerialize] public int OrderId { get; set; }
    
    public void ApplyStats(Tower tower)
    {
        if (tower.AttackStats is GunStats stats)
            stats.RicochetStats.DamageMultPerPenetration = 1;
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/NoDamageLossOnPenetration");
}