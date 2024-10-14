#if UNITY_EDITOR
using CardTD.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TestingAgent.Editor.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace TestingAgent.Editor
{
    public sealed partial class TestingAgent
    {
        private void Subscribe()
        {
            Messenger.AddListener(GameEvents.Restart, OnRestartHandler);
            Messenger<int>.AddListener(GameEvents.Win, OnLevelWin);
            Messenger.AddListener(GameEvents.Lost, OnLevelLost);
        }

        private void Unsubscribe()
        {
            Messenger.RemoveListener(GameEvents.Restart, OnRestartHandler);
            Messenger<int>.RemoveListener(GameEvents.Win, OnLevelWin);
            Messenger.RemoveListener(GameEvents.Lost, OnLevelLost);
        }

        private void OnLevelWin(int _) => ChangeModifier(true);

        private void OnLevelLost() => ChangeModifier(false);
        
        /// <summary>Wait until ECS done internal jobs before we restart</summary>
        private IEnumerator AwaitRestart()
        {
            yield return new WaitForSecondsRealtime(1f);
            GameServices.Instance.Restart();

            GetCashForReload();

            ReduceCreepCount();
            ModifyCreepHp();
        }
        
        private void ChangeModifier(bool isWin)
        {
            if (isWin)
            {
                Dictionary<int,int> dictionary = DataManager.Instance.GameData.GetFieldValue<Dictionary<int, int>>("selectedRewards");
                dictionary[mission.MissionIndex] = 0;
            }
            
            Debug.Log($"is win: {isWin}");
            isBusy = true;
            winCount += isWin  ? 1 : 0;
            loseCount += isWin ? 0 : 1;
            
            List<WinLoseInfo> list = isWin switch
            {
                true  => currentResult.Current.Wins,
                false => currentResult.Current.Lose
            };

            List<TowerInfo> towers = new(builtTowers.Count);
            foreach (TowerInfo info in builtTowers)
                towers.Add(info.PartialClone());

            WeaponPart directive = testDirectivesOnly 
                ? directiveProfile.Directives[currentTestingDirective] 
                : builtTowers[0].Tower.Directives?.FirstOrDefault()?.WeaponPart;
            
            WinLoseInfo item = new(attempts, currentHpModifier, towers, directive);
            
            list.Add(item);
            
            CollectTowerStatistics(list[^1].Towers);
            UnityEditor.EditorUtility.SetDirty(currentResult);

            if (condition == TestCondition.LoseWinAttempts)
            {
                if (lastAttemptWinResult.HasValue && !lastAttemptWinResult.Value && isWin && loseWinAttemptsTemp > 0)
                    loseWinAttemptsTemp--;

                currentHpModifier *= isWin switch
                {
                    true  => multiplierPerWin,
                    false => multiplierPerLose
                };
                
                if (loseWinAttemptsTemp == 0)
                {
                    loseWinAttemptsTemp = loseWinAttempts;
                    
                    if (TryCompleteAndIterateTest(true))
                        return;
                }
            }
            
            lastAttemptWinResult = isWin;
            
            if(condition == TestCondition.ModifierSliceLessThan)
            {
                hpModifierStepSlice = isWin ? hpModifierStepSlice : hpModifierStepSlice / 2;
                currentHpModifier += isWin switch
                {
                    true  => hpModifierStepSlice,
                    false => -hpModifierStepSlice // 7.5 += -0.5 / 2 = 7.25
                };

                if (TryCompleteAndIterateTest(hpModifierStepSlice <= minAvailableSlice))
                    return;
            }

            if (condition == TestCondition.CreepHpLessThanOrEqual)
            {
                float creepHp = waves.Entries[currentTestIteration].OverrideCreepHp
                    ? waves.Entries[currentTestIteration].CreepHp
                    : waves.Stats.MaxHP;

                float before = creepHp * currentHpModifier;
                currentHpModifier = isWin switch
                {
                    true  => currentHpModifier * 2,
                    false => currentHpModifier - (currentHpModifier / 2 / 2)
                };
                float after = creepHp * currentHpModifier;
                
                if(TryCompleteAndIterateTest(math.abs(before - after) <= minAvailableSlice))
                    return;
            }
            
            StartCoroutine(AwaitRestart());
        }
    }
}
#endif