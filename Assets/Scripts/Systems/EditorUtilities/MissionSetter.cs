#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEngine;

namespace Systems
{
    public class MissionSetter : OdinEditorWindow
    {
        [MenuItem("Tools/Mission Utilities/Mission setter")]
        private static void OpenWindow()
        {
            GetWindow<MissionSetter>().Show();
        }
        [SerializeField] private Mission mission;
        [SerializeField] private MissionList missionList;
        
        [FoldoutGroup("Waves/Indexes")]
        public int CahPerWaveIndex = 0;
        [FoldoutGroup("Waves/Indexes")]
        public int UnitsCountIndex = 1;
        [FoldoutGroup("Waves/Indexes")]
        public int CashPerUnitIndex = 2;
        [FoldoutGroup("Waves/Indexes")]
        public int CreepHpIndex = 3;
        [FoldoutGroup("Waves")] [SerializeField, TextArea] private string cashPerWaveUnitsCashPerUnit;
        [FoldoutGroup("Waves")]public List<MissionWaveData> WaveDatas = new();

        [FoldoutGroup("Rewards")] [SerializeField, TextArea] private string rewardsData;
        [FoldoutGroup("Rewards")]  [SerializeField] private List<Reward> parsedRewards;
        [FoldoutGroup("Rewards/Indexes")] public int Soft;
        [FoldoutGroup("Rewards/Indexes")] public int SoftReplay;
        [FoldoutGroup("Rewards/Indexes")] public int Hard;
        [FoldoutGroup("Rewards/Indexes")] public int Scrap;
        [FoldoutGroup("Rewards/Indexes")] public int ScrapReplay;
        
        [FoldoutGroup("Difficulty")] [SerializeField, TextArea] private string difficultyData;
        [FoldoutGroup("Difficulty")] [SerializeField] private List<float> parsedDifficulties = new ();
        
        [FoldoutGroup("Waves")][Button]
        private void ReadWaveData()
        {
            string[] data = cashPerWaveUnitsCashPerUnit.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            WaveDatas.Clear();

            int index = 0;
            foreach (string stringData in data)
            {
                string[] waveDatas = stringData.Split('\t');

                MissionWaveData waveData = new();
                waveData.WaveIndex = index;
                waveData.CashPerWave = (int)float.Parse(waveDatas[CahPerWaveIndex]);
                waveData.UnitsCount = int.Parse(waveDatas[UnitsCountIndex]);
                waveData.CashPerUnit = int.Parse(waveDatas[CashPerUnitIndex]);
                waveData.CreepHp = (int)float.Parse(waveDatas[CreepHpIndex]);
                WaveDatas.Add(waveData);
                index++;
            }
        }
        
        [FoldoutGroup("Waves")][Button]
        public void SetWaveDataToMission()
        {
            if (mission == null)
            {
                throw new Exception("mission is null");
            }

            if (WaveDatas.Count == 0)
            {
                throw new Exception("WaveDatas is empty");
            }

            List<int> cashPerStartWaves = new ();

            foreach (MissionWaveData waveData in WaveDatas)
            {
                cashPerStartWaves.Add(waveData.CashPerWave);
                
                int countSpawnGroup = 0;
                for (int i = 0; i < mission.SpawnData.Length; i++)
                {
                    foreach (var wave in mission.SpawnData[i].Waves)
                    {
                        if (wave.WaveNum == waveData.WaveIndex)// && wave.SpawnUnits.Count > 0
                        {
                            countSpawnGroup++;
                        }
                    }
                }

                if (countSpawnGroup == 0)
                {
                    Debug.LogError($"countSpawnGroup is 0 index {waveData.WaveIndex}");
                    continue;
                }
                #if UNITY_EDITOR
                    EditorUtility.SetDirty(mission);
                #endif
                
                int countUnits = waveData.UnitsCount / countSpawnGroup;

                List<int> units = new () {(waveData.UnitsCount % countSpawnGroup == 0) ? countUnits : countUnits + 1};

                if (countSpawnGroup > 1)
                    for (int i = 1; i < countSpawnGroup; i++)
                    {
                        units.Add(countUnits);
                    }
                
                int index = 0;
                for (int i = 0; i < mission.SpawnData.Length; i++)
                {
                    for (int waveIndex = 0; waveIndex < mission.SpawnData[i].Waves.Count; waveIndex++)
                    {
                        Wave wave = mission.SpawnData[i].Waves[waveIndex];
                        if (wave.WaveNum == waveData.WaveIndex)//wave.SpawnUnits.Count > 0 && 
                        {
                            //var key = wave.SpawnUnits.GetKeyByIndex(0);
                            // wave.SpawnUnits[key] = units[index];
                            wave.Count = units[index];
                            wave.CashReward = waveData.CashPerUnit;
                            wave.CreepHp = waveData.CreepHp;
                            index++;
                        }
                    }
                }

                mission.CashPerWaveStart = cashPerStartWaves;
            }
            
            AssetDatabase.SaveAssets();
        }
        [FoldoutGroup("Rewards")][Button]
        private void ReadRewardsData()
        {
            string[] data = rewardsData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            parsedRewards.Clear();
            
            for (int i = 0; i < data.Length; i++)
            {
                string[] rewardData = data[i].Split('\t');

                Reward reward = new();

                if (rewardData.Length > Soft)
                    reward.SoftCurrency = int.TryParse(rewardData[Soft], out int softCurrency) ? softCurrency : -1;
                else reward.SoftCurrency = -1;
                    
                //if (rewardData.Length > SoftReplay)
                //    reward.SoftReplay = int.TryParse(rewardData[SoftReplay], out int softReplay) ? softReplay : -1;
                //else reward.ScrapReplay = -1;
                
                if (rewardData.Length > Hard)
                    reward.HardCurrency = int.TryParse(rewardData[Hard], out int hardCurrency) ? hardCurrency : -1;
                else reward.HardCurrency = -1;
                
                if (rewardData.Length > Scrap)
                    reward.Scrap = int.TryParse(rewardData[Scrap], out int scrap) ? scrap : -1;
                else reward.Scrap = -1;
                
                //if (rewardData.Length > ScrapReplay)
                //    reward.ScrapReplay = int.TryParse(rewardData[ScrapReplay], out int scrapReplay) ? scrapReplay : -1;
                //else reward.ScrapReplay = -1;
                
                parsedRewards.Add(reward);
            }
        }
        
        [FoldoutGroup("Rewards")][Button]
        public void SetParsedRewards()
        {
            if (missionList.Missions.Count > parsedRewards.Count)
            {
                Debug.LogError($"Mission rewards will be set until {parsedRewards.Count} mission. Data entry count is {parsedRewards.Count}, missions count is {missionList.Missions.Count}");
            }
            for (int i = 0; i < parsedRewards.Count; i++)
            {
                if (i >= missionList.Missions.Count)
                {
                    Debug.LogError($"Mission rewards has been set until {i} mission.");
                    break;
                }
                if(parsedRewards[i].SoftCurrency != -1)
                    missionList.Missions[i].Reward.SoftCurrency = parsedRewards[i].SoftCurrency;
                //if(parsedRewards[i].SoftReplay != -1)
                //    missionList.Missions[i].Reward.SoftReplay = parsedRewards[i].SoftReplay;                
                if(parsedRewards[i].HardCurrency != -1)
                    missionList.Missions[i].Reward.HardCurrency = parsedRewards[i].HardCurrency;                
                if(parsedRewards[i].Scrap != -1)
                    missionList.Missions[i].Reward.Scrap = parsedRewards[i].Scrap;                
                //if(parsedRewards[i].ScrapReplay != -1)
                //    missionList.Missions[i].Reward.ScrapReplay = parsedRewards[i].ScrapReplay;

                EditorUtility.SetDirty(missionList.Missions[i]);
            }
            AssetDatabase.SaveAssets();

        }

        [FoldoutGroup("Difficulty")] [Button]
        private void ReadDifficultyData()
        {
            string formatedData =  Regex.Replace(difficultyData, @"(\d+)%", m => (int.Parse(m.Groups[1].Value) / 100.0).ToString());
            string[] data = formatedData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            parsedDifficulties.Clear();

            foreach (string value in data)
            {
                parsedDifficulties.Add(1 + float.Parse(value));
            }
        }

        [FoldoutGroup("Difficulty")] [Button]
        public void SetDifficulties()
        {
            if (missionList.Missions.Count > parsedDifficulties.Count)
            {
                Debug.LogError($"Mission difficulties will be set until {missionList.Missions.Count} mission. Data entry count is {parsedRewards.Count}, missions count is {missionList.Missions.Count}");
            }

            for (int i = 0; i < parsedDifficulties.Count; i++)
            {
                if (i >= missionList.Missions.Count)
                {
                    Debug.LogError($"Mission rewards has been set until {i} mission.");
                    break;
                }

                missionList.Missions[i].HpModifier = parsedDifficulties[i];
                

                EditorUtility.SetDirty(missionList.Missions[i]);
            }
            AssetDatabase.SaveAssets();
        }


        [Serializable]
        public class MissionWaveData
        {
            public int WaveIndex;
            public int UnitsCount;
            public int CashPerWave;
            public int CashPerUnit;
            public int CreepHp;
        }
    }
}
#endif

