using I2.Loc;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public sealed class SwitchRicochetPenetration : Tag, IStaticTag
{
    [SerializeField, EnumToggleButtons] private SwitchType type;
    [OdinSerialize] public int OrderId { get; set; }
    
    public void ApplyStats(Tower tower)
    {
        switch (type)
        {
            case SwitchType.PenetrationToRicochet:
                ((GunStats)tower.AttackStats).RicochetStats.RicochetCount += ((GunStats)tower.AttackStats).RicochetStats.PenetrationCount;
                ((GunStats)tower.AttackStats).RicochetStats.PenetrationCount = 0;
                break;
            case SwitchType.RicochetToPenetration:
                ((GunStats)tower.AttackStats).RicochetStats.PenetrationCount += ((GunStats)tower.AttackStats).RicochetStats.RicochetCount;
                ((GunStats)tower.AttackStats).RicochetStats.RicochetCount = 0;
                break;
        }
    }

    public override string GetDescription()
    {
        
        switch (type) 
        { 
            case SwitchType.PenetrationToRicochet:
                return LocalizationManager.GetTranslation("Tags/PenetrationToRicochet");
            case SwitchType.RicochetToPenetration:
            default:
                return LocalizationManager.GetTranslation("Tags/RicochetToPenetration");
        }  
    }


    private enum SwitchType { PenetrationToRicochet, RicochetToPenetration }
}