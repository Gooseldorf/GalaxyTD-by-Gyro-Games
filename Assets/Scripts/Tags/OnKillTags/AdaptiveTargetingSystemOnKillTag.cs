using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public sealed class AdaptiveTargetingSystemOnKillTag : OnKillTag
{
    [SerializeField, EnumToggleButtons] private AdaptiveTargetingSystemType type;
    
    [SerializeField, ShowIf("type", AdaptiveTargetingSystemType.Armor)] private AllEnums.ArmorType armorType;
    [InfoBox("Positive number - increase, negative decrease. 100% is 1")]
    [SerializeField, ShowIf("type", AdaptiveTargetingSystemType.Armor)] private float increasePercentArmor = .05f;
    [SerializeField, ShowIf("type", AdaptiveTargetingSystemType.Flesh)] private AllEnums.FleshType fleshType;
    [InfoBox("Positive number - increase, negative decrease. 100% is 1")]
    [SerializeField, ShowIf("type", AdaptiveTargetingSystemType.Flesh)] private float increasePercentFlesh = .05f;
    [InfoBox("Positive number - increase, negative decrease. 100% is 1")]
    [SerializeField, ShowIf("type", AdaptiveTargetingSystemType.Creep)] private float increasePercentCreepArmorFlesh = .05f;

    [SerializeField] private float increasePercentThreshold = 2.5f;
    
    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        if(!handler.Exist())
            return;
        
        DamageModifiers damageModifiersComponent = handler.Manager.GetComponentData<DamageModifiers>(handler.Tower);
        
        switch (type)
        {
            case AdaptiveTargetingSystemType.Armor:
                CheckArmorType(armorType, increasePercentArmor, ref damageModifiersComponent);
                break;
            case AdaptiveTargetingSystemType.Flesh:
                CheckFleshType(fleshType, increasePercentFlesh, ref damageModifiersComponent);
                break;
            case AdaptiveTargetingSystemType.Creep:
                CheckArmorType(handler.SharedData.ArmorType, increasePercentCreepArmorFlesh, ref damageModifiersComponent);
                CheckFleshType(handler.SharedData.FleshType, increasePercentCreepArmorFlesh, ref damageModifiersComponent);
                break;
        }
        
        handler.Manager.SetComponentData(handler.Tower, damageModifiersComponent);
    }

    private void CheckArmorType(AllEnums.ArmorType armorType, float increasePercent, ref DamageModifiers damageModifiersComponent)
    {
        switch (armorType)
        {
            case AllEnums.ArmorType.Unarmored:
                damageModifiersComponent.DamageToUnarmored = math.min(damageModifiersComponent.DamageToUnarmored + increasePercent, increasePercentThreshold);
                break;
            case AllEnums.ArmorType.Light:
                damageModifiersComponent.DamageToLight = math.min(damageModifiersComponent.DamageToLight + increasePercent, increasePercentThreshold);;
                break;
            case AllEnums.ArmorType.Heavy:
                damageModifiersComponent.DamageToHeavy = math.min(damageModifiersComponent.DamageToHeavy + increasePercent, increasePercentThreshold);;
                break;
        }
    }

    private void CheckFleshType(AllEnums.FleshType fleshType, float increasePercent, ref DamageModifiers damageModifiersComponent)
    {
        switch (fleshType)
        {
            case AllEnums.FleshType.Bio:
                damageModifiersComponent.DamageToBio = math.min(damageModifiersComponent.DamageToBio + increasePercent, increasePercentThreshold);;
                break;
            case AllEnums.FleshType.Mech:
                damageModifiersComponent.DamageToMechanical = math.min(damageModifiersComponent.DamageToMechanical + increasePercent, increasePercentThreshold);;
                break;
            case AllEnums.FleshType.Energy:
                damageModifiersComponent.DamageToEnergy = math.min(damageModifiersComponent.DamageToEnergy + increasePercent, increasePercentThreshold);;
                break;
        }
    }

    public override string GetDescription()
    {
        string result;
        switch (type)
        {
            case AdaptiveTargetingSystemType.Armor:
                result = LocalizationManager.GetTranslation("Tags/AdaptiveTargetingType")
                                                .Replace("{param}", LocalizationManager.GetTranslation(armorType.ToString()));
                break;
            case AdaptiveTargetingSystemType.Flesh:
                result = LocalizationManager.GetTranslation("Tags/AdaptiveTargetingType")
                                                .Replace("{param}", LocalizationManager.GetTranslation(fleshType.ToString()));
                break;
            default:
            case AdaptiveTargetingSystemType.Creep:
                result = LocalizationManager.GetTranslation("Tags/AdaptiveTargetingCreep");
                break;

        }
        return result;
    }
private enum AdaptiveTargetingSystemType { Armor, Flesh, Creep }
}
