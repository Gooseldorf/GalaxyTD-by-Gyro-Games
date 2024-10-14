using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Line = TestingAgent.ClipboardHelper.StringLine<TestingAgent.TowerInfo>;

// ReSharper disable InconsistentNaming
namespace TestingAgent
{
    [Serializable]
    public class WinLoseInfo
    {
        public int Attempt;
        public float HpModifier;
        public WeaponPart Directive;
        public List<TowerInfo> Towers;

        public WinLoseInfo(int attempt, float hpModifier, List<TowerInfo> towers, WeaponPart directive)
        {
            Attempt = attempt;
            HpModifier = hpModifier;
            Towers = towers;
            Directive = directive;
        }
        
        [Button("Copy To Clipboard")/*, HorizontalGroup("g1")*/]
        private void ToClipBoardAsRows(bool withHeaders) => ClipboardHelper.CopyAsRows(GetDataToClopBoard(withHeaders));

        // [Button("Copy data to clipboard as columns"), HorizontalGroup("g1")]
        // private void ToClipBoardAsColumns(bool withHeaders) => ClipboardHelper.CopyAsColumns(GetDataToClopBoard(withHeaders));

        public virtual Line[] GetDataToClopBoard(bool withHeaders)
        {
            return new []
            {
                new Line(Towers, x => $"{x.TowerId}", withHeaders ? "Tower Type" : null),
                new Line(Towers, x => $"{(Directive == null ? "NONE" : Directive.name)}", withHeaders ? "Directive" : null),
                new Line(Towers, x => $"{x.LevelUpCount + 1}", withHeaders ? "Tower Lvl" : null),
                new Line(Towers, x => $"{x.Damage / x.OverallCost}", withHeaders ? "Damage/Cost" : null),
                new Line(Towers, x => $"{x.Shots}", withHeaders ? "Shots Fired" : null),
                new Line(Towers, x => $"{x.Kills}", withHeaders ? "Kills" : null),
                // new Line(Towers, x => $"{x.Reloads}", withHeaders ? "Reloads" : null),
                new Line(Towers, x => $"{x.Damage}", withHeaders ? "Damage" : null),
                new Line(Towers, x => $"{x.Overheat}", withHeaders ? "Overheat Damage" : null),
                // new Line(Towers, x => $"{x.Dps}", withHeaders ? "DPS" : null),
                // new Line(Towers, x => $"{x.OverallCost}", withHeaders ? "Overall Cost" : null),
                // new Line(Towers, x => $"{x.BuildCost}", withHeaders ? "Build Cost" : null),
                // new Line(Towers, x => $"{x.UpgradeCost}", withHeaders ? "Upgrade Cost" : null),
                // new Line(Towers, x => $"{x.ReloadCost}", withHeaders ? "Reload Cost" : null),
            };
        }
    }
}