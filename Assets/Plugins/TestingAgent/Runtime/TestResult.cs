using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
namespace TestingAgent
{
    public class TestResult : ScriptableObject
    {
        [GUIColor("GetLabelColor")]
        [ReadOnly] public bool IsCompleted;
        [ReadOnly] public Mission Mission;
        [ReadOnly] public GameData TowerFactoryData;
        [ReadOnly] public CreepStats Stats;
        [ReadOnly] public int Cash;
        [ReadOnly] public string ElapsedTime;
        
        [ReadOnly, HideInInspector] public List<TowerInfo> BuiltTowers;
        [ReadOnly, HideInInspector] public List<TestResultEntry> Entries = new();
        public bool WithHeaders = true;
        
        public TestResultEntry Current => Entries[^1];
        
        private Color GetLabelColor(bool value) => value ? Color.green : Color.red;

        [HorizontalGroup("Horizontal1")]
        [Button]
        private void CopyAllWins()
        {
            StringBuilder builder = new();
            foreach (TestResultEntry entry in Entries)
            {
                foreach (WinLoseInfo info in entry.Wins)
                {
                    string result = ClipboardHelper.FormatDataAsRows(info.GetDataToClopBoard(WithHeaders));
                    builder.Append($"{result}\r\r");
                }
                
                builder.Append("\r\r");
            }
            
            ClipboardHelper.Copy(builder.ToString());
        }

        [HorizontalGroup("Horizontal1")]
        [Button]
        private void CopyAllLoses()
        {
            StringBuilder builder = new();
            foreach (TestResultEntry entry in Entries)
            {
                foreach (WinLoseInfo info in entry.Lose)
                {
                    string result = ClipboardHelper.FormatDataAsRows(info.GetDataToClopBoard(WithHeaders));
                    builder.Append($"{result}\r\r");
                }
                
                builder.Append("\r\r");
            }
            
            ClipboardHelper.Copy(builder.ToString());
        }

        [HorizontalGroup("Horizontal2")]
        [Button]
        private void CopyLastWin()
        {
            StringBuilder builder = new();
            foreach (TestResultEntry entry in Entries)
            {
                WinLoseInfo info = entry.Wins.LastOrDefault();
                
                if(info == null)
                    continue;

                string result = ClipboardHelper.FormatDataAsRows(info.GetDataToClopBoard(WithHeaders));
                builder.Append($"{result}\r\r");
            }
            
            ClipboardHelper.Copy(builder.ToString());
        }
        
        [HorizontalGroup("Horizontal2")]
        [Button]
        private void CopyLastLose()
        {
            StringBuilder builder = new();
            foreach (TestResultEntry entry in Entries)
            {
                WinLoseInfo info = entry.Lose.LastOrDefault();
                
                if(info == null)
                    continue;
                
                string result = ClipboardHelper.FormatDataAsRows(info.GetDataToClopBoard(WithHeaders));
                builder.Append($"{result}\r\r");
            }
            
            ClipboardHelper.Copy(builder.ToString());
        }
    }
}