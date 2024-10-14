using I2.Loc;
using Sirenix.Serialization;

public sealed class NegativeDamageFalloffTag : Tag, IStaticTag
{
    [OdinSerialize] public int OrderId { get; set; }
    
    public void ApplyStats(Tower tower)
    {
        //this is formula for fallof
        //damage * Mathf.Pow(damageFalloff, distanceTraveled / damageDropDistance);
        //so if fallof < 1 - damage will reduce with distance
        //if fallof > 1 - damage will rise with distance
        //so if fallof was 0.3, negative fallof will be (1 / 0.3)
        if (tower.AttackStats is GunStats stats)
            stats.DamageFallof = 1 / stats.DamageFallof; 
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/NegativeDamageFalloff");
}