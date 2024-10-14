using I2.Loc;
using Sirenix.Serialization;

public sealed class NegativeKnockbackTag : Tag, IStaticTag
{
    [OdinSerialize] public int OrderId { get; set; }
    
    public void ApplyStats(Tower tower) => tower.AttackStats.KnockBackPerBullet = -tower.AttackStats.KnockBackPerBullet;
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/NegativeKnockback");
}