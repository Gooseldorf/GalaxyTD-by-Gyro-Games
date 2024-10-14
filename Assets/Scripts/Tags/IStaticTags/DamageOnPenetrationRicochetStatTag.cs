using I2.Loc;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public sealed class DamageOnPenetrationRicochetStatTag : Tag, IStaticTag
{
    [SerializeField, EnumToggleButtons] private OnType type;
    [SerializeField] private float newValue;
    [OdinSerialize] public int OrderId { get; set; }

    public void ApplyStats(Tower tower)
    {
        switch (type)
        {
            case OnType.Penetration:
                ((GunStats)tower.AttackStats).RicochetStats.DamageMultPerPenetration = newValue;
                break;
            case OnType.Ricochet:
                ((GunStats)tower.AttackStats).RicochetStats.DamageMultPerRicochet = newValue;
                break;
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation($"Tags/DamageOn{type}")
                                                .Replace("{param}", (newValue > 1 ? "+" : "") + ((newValue - 1) * 100) + "<color=#1fb2de>%</color>");

    private enum OnType { Penetration, Ricochet }
}