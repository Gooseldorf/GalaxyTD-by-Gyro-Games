using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Line = TestingAgent.ClipboardHelper.StringLine<TestingAgent.TowerInfo>;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
namespace TestingAgent
{
    public class TestResultEntry : ScriptableObject, IEnumerable<WinLoseInfo>
    {
        public bool WithHeaders = true;

        [GUIColor("GetLabelColor")]
        public bool IsCompleted;
        public int CreepCount;
        public float HpModifier;
        [ReadOnly] public string ElapsedTime;
        public List<WinLoseInfo> Wins = new();
        public List<WinLoseInfo> Lose = new();
        
        [HorizontalGroup("Horizontal1")]
        [Button]
        private void CopyAllWins() => CopyData(Wins);
        
        [HorizontalGroup("Horizontal1")]
        [Button]
        private void CopyAllLoses() => CopyData(Lose);

        [HorizontalGroup("Horizontal2")]
        [Button]
        private void CopyLastWin()
        {
            WinLoseInfo info = Wins.LastOrDefault();
            
            if(info == null)
                return;
            
            string data = ClipboardHelper.FormatDataAsRows(info.GetDataToClopBoard(WithHeaders));
            ClipboardHelper.Copy(data);
        }
        
        [HorizontalGroup("Horizontal2")]
        [Button]
        private void CopyLastLose()
        {
            WinLoseInfo info = Lose.LastOrDefault();
            
            if(info == null)
                return;
            
            string data = ClipboardHelper.FormatDataAsRows(info.GetDataToClopBoard(WithHeaders));
            ClipboardHelper.Copy(data);
        }

        private void CopyData(List<WinLoseInfo> list)
        {
            StringBuilder builder = new();
            foreach (WinLoseInfo info in list)
            {
                string result = ClipboardHelper.FormatDataAsRows(info.GetDataToClopBoard(WithHeaders));
                builder.Append($"{result}\r\r");
            }
            
            ClipboardHelper.Copy(builder.ToString());
        }

        public IEnumerator<WinLoseInfo> GetEnumerator()
        {
            foreach (WinLoseInfo info in Wins) yield return info;
            foreach (WinLoseInfo info in Lose) yield return info;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private Color GetLabelColor(bool value) => value ? Color.green : Color.red;

    }
}