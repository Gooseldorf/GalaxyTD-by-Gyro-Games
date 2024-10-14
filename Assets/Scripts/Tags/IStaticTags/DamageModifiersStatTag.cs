using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UI;
using UnityEngine;
using static AllEnums;

public sealed class DamageModifiersStatTag : Tag, IStaticTag
{
    [SerializeField] private bool modifiersTypeToggle = true;
    
    [SerializeField, ShowIf("modifiersTypeToggle")] private ArmorType armorType;
    [SerializeField, ShowIf("modifiersTypeToggle"), InfoBox("Percents, where 100% is 1")] private float bonusArmorDamagePercent = 1f;
    
    [SerializeField, HideIf("modifiersTypeToggle")] private FleshType fleshType;
    [SerializeField, HideIf("modifiersTypeToggle"), InfoBox("Percents, where 100% is 1")] private float bonusFleshDamagePercent = 1f;
    [OdinSerialize] public int OrderId { get; set; } 
    
    public void ApplyStats(Tower tower)
    {
        DamageModifiers modifiers = tower.DamageModifiers;
        Apply(ref modifiers);
        tower.DamageModifiers = modifiers;
    }

    private void Apply(ref DamageModifiers modifiers)
    {
        if(modifiersTypeToggle)
        {
            switch (armorType)
            {
                case ArmorType.Unarmored:
                    modifiers.DamageToUnarmored += bonusArmorDamagePercent;
                    break;
                case ArmorType.Light:
                    modifiers.DamageToLight += bonusArmorDamagePercent;
                    break;
                case ArmorType.Heavy:
                    modifiers.DamageToHeavy += bonusArmorDamagePercent;
                    break;
            }
        }
        else
        {
            switch (fleshType)
            {
                case FleshType.Bio:
                    modifiers.DamageToBio += bonusFleshDamagePercent;
                    break;
                case FleshType.Mech:
                    modifiers.DamageToMechanical += bonusFleshDamagePercent;
                    break;
                case FleshType.Energy:
                    modifiers.DamageToEnergy += bonusFleshDamagePercent;
                    break;
            }
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/DamageModifier")
        .Replace("{param}", (int)(modifiersTypeToggle ? bonusArmorDamagePercent * 100 : bonusFleshDamagePercent * 100) + "<color=#1fb2de>%</color>")
                                                .Replace("{param1}", LocalizationManager.GetTranslation(modifiersTypeToggle ? armorType.ToString() : fleshType.ToString()));

    public Sprite GetTypeIcon => modifiersTypeToggle ? UIHelper.Instance.GetWaveIcon($"{armorType.ToString()}Icon") : UIHelper.Instance.GetWaveIcon($"{fleshType.ToString()}FleshIcon");
    public float GetBonusForUI => modifiersTypeToggle ? bonusArmorDamagePercent : bonusFleshDamagePercent;

}