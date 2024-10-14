using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
namespace TestingAgent
{
    [Serializable]
    public sealed class TowerInfo
    {
        [BoxGroup("General"), GUIColor(.3f, 1f, .9f)] 
        public AllEnums.TowerId TowerId; 
        
        public Tower Tower;
        public TowerFactory Factory;
        
        [HideInInspector] public int DropZoneIndex;

        [Title("Upgrades")]
        [FoldoutGroup("Statistics"), Indent, LabelText("Count", SdfIconType.FullscreenExit)] 
        public int LevelUpCount;
        
        [Title("Shot stats")]
        [FoldoutGroup("Statistics"), Indent, LabelText(SdfIconType.FullscreenExit)] 
        public int Shots;
        [FoldoutGroup("Statistics"), Indent, LabelText(SdfIconType.EmojiDizzy)] 
        public int Kills;
        [FoldoutGroup("Statistics"), Indent, LabelText(SdfIconType.ArrowRepeat)] 
        public int Reloads;
        
        [Title("Damage Info")]
        [FoldoutGroup("Statistics"), Indent, LabelText(SdfIconType.LightningChargeFill)] 
        public float Damage;
        [FoldoutGroup("Statistics"), Indent, LabelText(SdfIconType.Radioactive)] 
        public float Overheat;
        [FoldoutGroup("Statistics"), Indent, LabelText(SdfIconType.Stopwatch)] 
        public float Dps;
        
        [Title("Money")]
        [FoldoutGroup("Statistics"), Indent, LabelText(SdfIconType.CurrencyDollar)] 
        public int OverallCost;
        [FoldoutGroup("Statistics"), Indent(2), LabelText(SdfIconType.GearFill)] 
        public int BuildCost;
        [FoldoutGroup("Statistics"), Indent(2), LabelText(SdfIconType.FileArrowUp)] 
        public int UpgradeCost;
        [FoldoutGroup("Statistics"), Indent(2), LabelText(SdfIconType.ArrowRepeat)] 
        public int ReloadCost;        
        
        [FormerlySerializedAs("CostPerDamage"), FoldoutGroup("Statistics"), Indent, LabelText(SdfIconType.GraphUpArrow)] 
        public float CostOverDamage;      
        [FormerlySerializedAs("CostPerDamage"), FoldoutGroup("Statistics"), Indent, LabelText(SdfIconType.GraphUpArrow)] 
        public float CostPerShot;
        
        public TowerInfo PartialClone()
        {
            return new TowerInfo
            {
                TowerId = TowerId, 
                Tower = Tower, 
                DropZoneIndex = DropZoneIndex,
                LevelUpCount = LevelUpCount,
            };
        }
    }
}