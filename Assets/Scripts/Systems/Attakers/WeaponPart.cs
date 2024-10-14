using I2.Loc;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static AllEnums;

[Serializable]
public class WeaponPart : ScriptableObject, IWeaponPart, ILocalized
{
    public List<Tag> Bonuses = new();

    [field: SerializeField]
    public PartType PartType { get; private set; }
    public string SerializedID => name;

    [field: SerializeField, EnumToggleButtons]
    public TowerId TowerId { get; private set; }
    public int SoftCost;
    public int HardCost;
    public int ScrapCost;
    [InfoBox("Percent value, where 100% is 1"), BoxGroup("CostIncrease")] public float TowerCostIncrease = 0.15f;
    [InfoBox("Percent value, where 100% is 1"), BoxGroup("CostIncrease")] public float BulletCostIncrease = 0.15f;

    [field: SerializeField,
    PreviewField(Alignment = ObjectFieldAlignment.Center, Height = 200),
    Title("Sprite", "@Sprite.name"),
    HideLabel]
    public Sprite Sprite { get; private set; }

    [Button]
    public string GetTitle()
    {
        return LocalizationManager.GetTranslation($"WeaponParts/{name}_title");
    }

    [Button]
    public virtual string GetDescription()
    {
        string result = "";
        string desc;
        for (int i = 0; i < Bonuses.Count; i++)
        {
            if(PartType==AllEnums.PartType.Ammo && Bonuses[i] is DamageModifiersStatTag)
                continue;
            
            desc = Bonuses[i].GetDescription();
            if (desc.Length > 0)
            {
                if (result != "") result += "\n";
                result += "<color=#1fb2de>></color> " + Bonuses[i].GetDescription();
            }
        }

        if (result == "")
        {
            string buildCost = TowerCostIncrease == 0 ? "" : "<color=#1fb2de>></color> " + LocalizationManager.GetTranslation("Tags/OnlyBuildCostModifyDirective")
                .Replace("{param1}", (TowerCostIncrease < 0 ? "" : "+") + (TowerCostIncrease * 100));
            string bulletCost = BulletCostIncrease == 0 ? "" : "<color=#1fb2de>></color> " + LocalizationManager.GetTranslation("Tags/OnlyBulletCostModifyDirective")
                .Replace("{param2}", (BulletCostIncrease < 0 ? "" : "+") + (BulletCostIncrease * 100));
            string separator = buildCost == String.Empty || bulletCost == String.Empty ? "" : "\n";
            return buildCost + separator + bulletCost;
        }
        
        if (result == "")
        {
            var sb = new StringBuilder();

            if(TowerCostIncrease != 0)
            {
                string translation = LocalizationManager.GetTranslation("Tags/OnlyBuildCostModifyDirective");
                string buildCost = $"{(TowerCostIncrease < 0 ? "" : "+")}{(TowerCostIncrease * 100)}";
                sb.AppendLine($"<color=#1fb2de>></color> {translation.Replace("{param1}", buildCost)}");
            }
   
            if(BulletCostIncrease != 0)
            {
                string translation = LocalizationManager.GetTranslation("Tags/OnlyBulletCostModifyDirective");
                string bulletCost = $"{(BulletCostIncrease < 0 ? "" : "+")}{(BulletCostIncrease * 100)}";
                if(sb.Length > 0)
                    sb.AppendLine();
                sb.Append($"<color=#1fb2de>></color> {translation.Replace("{param2}", bulletCost)}");
            }
   
            return sb.ToString();
        }
            /*return "<color=#1fb2de>></color> " + LocalizationManager.GetTranslation("Tags/OnlyCostModifyDirective")
                        .Replace("{param1}", (TowerCostIncrease < 0 ? "" : "+") + (TowerCostIncrease * 100).ToString())
                        .Replace("{param2}", (BulletCostIncrease < 0 ? "" : "+") + (BulletCostIncrease * 100).ToString());*/

        return result;
    }
}