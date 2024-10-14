#if UNITY_EDITOR
using CardTD.Utilities;
using ECSTest.Systems;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static AllEnums;
using static Systems.MissionSpawnGenerator;
using Random = UnityEngine.Random;

namespace Systems
{
    public class MissionSpawnGenerator : OdinEditorWindow
    {
        [MenuItem("Tools/Mission Utilities/Wave generator")]
        private static void OpenWindow()
        {
            GetWindow<MissionSpawnGenerator>().Show();
        }

        private const string randomizationParams = "randomizationParams";
        private const string missionData = "Mission";
        private const string generator = "Generator";
        private const string helperFunctions = "helperFunctions";
        private const float waveExtraDifficulty = 1.2f;
        [FoldoutGroup(randomizationParams)] public int2 CountHeavyUnits = new int2(40, 80);
        [FoldoutGroup(randomizationParams)] public int2 CountLightUnits = new int2(80, 120);
        [FoldoutGroup(randomizationParams)] public int2 CountUnarmoredUnits = new int2(120, 180);

        [FoldoutGroup(randomizationParams)] public int2 CountFleshTypes = new int2(1, 3);
        [FoldoutGroup(randomizationParams)] public int2 CountArmorTypes = new int2(1, 3);
        [FoldoutGroup(randomizationParams)] public List<CreepStats> CreepDictionary = new();

        [FoldoutGroup(randomizationParams), EnumToggleButtons] public FleshType PermittedFleshTypes;
        [FoldoutGroup(randomizationParams), EnumToggleButtons] public ArmorType ArmorTypes;

        [SerializeField, FoldoutGroup(missionData)]
        public Mission Mission;

        [SerializeField, FoldoutGroup(missionData), HideInInspector]
        private List<PrepareWaveData> waves = new();
        [SerializeField, HorizontalGroup("Effectiveness", Title = "Effectiveness", MarginLeft = 0.2f, Gap = 0.2f)] public float TowersEff;
        [SerializeField, HorizontalGroup("Effectiveness", MarginRight = 0.2f)] public float ShootingEff;

        [HorizontalGroup("Distances", Title = "Distances", MarginLeft = 0.2f, Gap = 0.2f), LabelText("First")] public float DistanceToFirstTower = 60;
        [HorizontalGroup("Distances", MarginRight = 0.2f), LabelText("Last")] public float DistanceToLastTower = 60;

        public int FirstWaveCash = 50;
        [SerializeField, HideInInspector]
        private int countWave;
        [DisableIf("@IsManualCreepTypes||IsManualCreepCounts"), ShowInInspector]
        public int CountWave
        {
            get
            {
                if (IsManualCreepTypes)
                    countWave = CreepsUsed.Count;
                else if (IsManualCreepCounts)
                    countWave = CreepCounts.Count;
                return countWave;
            }
            set => countWave = value;
        }

        [HorizontalGroup("Creeps", Title = "Creeps"), VerticalGroup("Creeps/Left"), ValidateInput("SameLength", "this arrays should have the same length != 0")]
        public bool IsManualCreepTypes = false;
        [EnableIf("IsManualCreepTypes"), VerticalGroup("Creeps/Left"), ValidateInput("SameLength", "this arrays should have the same length != 0")]
        public List<CreepStats> CreepsUsed = new();

        [HorizontalGroup("Creeps"), VerticalGroup("Creeps/Right"), ValidateInput("SameLength", "this arrays should have the same length != 0")]
        public bool IsManualCreepCounts = false;
        [EnableIf("IsManualCreepCounts"), VerticalGroup("Creeps/Right"), ValidateInput("SameLength", "this arrays should have the same length != 0")]
        public List<int> CreepCounts = new();

        [ReadOnly] public List<int> WaveStartCashes = new();

        [TableList, FoldoutGroup(generator), LabelText("Waves"), SerializeField]
        private List<WaveSpawnData> waveDataList = new();

        [TableList, FoldoutGroup(generator), LabelText("Segments"), SerializeField]
        private List<Segment> segments = new();

        [SerializeField, FoldoutGroup(missionData), HideInInspector]
        private List<CreepStats> creeps = new();


        [Button("1.Generate Source Data", buttonSize: ButtonSizes.Large), PropertySpace]
        public void GenerateSource()
        {
            waveDataList.Clear();

            GetUnitTypesForMission();
            GetUnitCountsForMission();
            float killPercent = 2;
            WaveStartCashes = new List<int>(CountWave);
            for (int i = 0; i < CountWave; i++)
            {
                WaveSpawnData wave;
                wave = new() { Creep = CreepsUsed[i] };

                float creepSpeed = wave.Creep.Speed;
                float waveStartTime = SpawnerSystem.FirstWaveOffset + i * (SpawnerSystem.WaveTimeLength + SpawnerSystem.PauseBetweenWaves);
                wave.StartTime = (int)(waveStartTime + DistanceToFirstTower / creepSpeed);
                wave.EndTime = (int)(waveStartTime + SpawnerSystem.WaveTimeLength + DistanceToLastTower / creepSpeed);

                wave.WaveNum = i;
                wave.Count = CreepCounts[i];
                //float killPercent = CalculateKillPercent(wave.ArmorType, wave.FleshType);
                killPercent = 2 * math.pow(1.3f, i);
                //killPercent *= 1.3f;
                killPercent /= creepSpeed;
                float totalGold = GetTotalWaveGold(i);
                //WaveStartCashes[wave.WaveNum] = (int)(totalGold / (1 + killPercent));
                //float cashForKill = WaveStartCashes[wave.WaveNum] / (1 - killPercent) * killPercent;
                float cashForKill = totalGold * killPercent / (1 + killPercent);

                wave.Reward = (int)math.clamp(cashForKill / wave.Count, 1, 99999);
                if (i == 0)
                    WaveStartCashes.Add(math.max(FirstWaveCash, (int)(totalGold - wave.CashForKill)));
                else if (i == 1)
                    WaveStartCashes.Add(math.max(FirstWaveCash / 2, (int)(totalGold - wave.CashForKill)));
                else
                    WaveStartCashes.Add(math.max(0, (int)(totalGold - wave.CashForKill)));

                WaveStartCashes[^1] = (WaveStartCashes[^1] / 5) * 5;
                wave.StartGold = WaveStartCashes[^1];
                waveDataList.Add(wave);
            }
            waveDataList.Sort((a, b) => a.EndTime.CompareTo(b.EndTime));
        }
        [Button("2.Generate Main Data", buttonSize: ButtonSizes.Large), PropertySpace]
        public void Generate()
        {
            segments.Clear();
            waveDataList.ForEach(x => x.Hp = 0);
            waveDataList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

            //Create all segments
            float currTime = waveDataList[0].StartTime;
            HashSet<WaveSpawnData> usedWaves = new();
            for (int i = 0; i < waveDataList.Count; i++)
            {
                Segment segment = FindNextSegment(i);
                FindOverlappingWaves(segment);
                segments.Add(segment);
            }

            for (int i = 0; i < segments.Count; i++)
            {

                segments[i].CurrentRewardGold = segments[i].CalculateRewardGold();

                if (i != 0)
                    segments[i].OldRewardGold += segments[i - 1].OldRewardGold + segments[i - 1].CurrentRewardGold;

                //Segments[i].StartGold1 = CalculateWaveStartGold(Segments[i].StartTime + 1) + CalculateRewardGold(Segments[i].StartTime);
                segments[i].EndGold1 = CalculateWaveStartGold(segments[i].EndTime - 5) + CalculateRewardGold(segments[i].EndTime);
            }

            RoundValues();
            CalculateSegmentsDPS();

            for (int i = 0; i < segments.Count; i++)
            {
                float remainingHp;
                float totalPercentage = 0;
                remainingHp = segments[i].TotalDamage;
                for (int j = 0; j < segments[i].OverlapingWaves.Count; j++)
                    totalPercentage += segments[i].OverlapPercent[j];

                for (int j = 0; j < segments[i].OverlapingWaves.Count; j++)
                    segments[i].OverlapingWaves[j].FullHp += remainingHp * segments[i].OverlapPercent[j] / totalPercentage;
            }

            for (int i = 0; i < waveDataList.Count; i++)
            {
                //Some rounding magic for beautiful numbers
                waveDataList[i].FullHp = math.round(waveDataList[i].FullHp);
                //Adjust for speed
                AdjustForSpeed(waveDataList[i]);
                if (i == waveDataList.Count - 1 || ((i + 1) % 5) == 0)
                    waveDataList[i].FullHp *= waveExtraDifficulty;
                waveDataList[i].Hp = (int)(waveDataList[i].FullHp / waveDataList[i].Count);

                if (i == waveDataList.Count - 1 || i == 0)
                    waveDataList[i].Reward = math.max((int)(waveDataList[i].Reward * 1.4f), waveDataList[i].Reward + 1);
                //int digits = (int)math.log10(waveDataList[i].Hp) + 1;
                //if (digits > 1)
                //{
                //    float temp = waveDataList[i].Hp / math.pow(10, digits - 2);
                //    temp = math.round(temp);
                //    waveDataList[i].Hp = (int)(temp * math.pow(10, digits - 2));
                //}
            }
        }

        private void AdjustForSpeed(WaveSpawnData waveSpawnData)
        {
            switch (waveSpawnData.Creep.Speed)
            {
                case >= 4:
                    waveSpawnData.FullHp *= 0.7f;
                    break;
                case <= 1.6f:
                    waveSpawnData.FullHp *= 1.2f;
                    break;
                default:
                    break;
            }
        }

        [Button("3.Prepare Data", buttonSize: ButtonSizes.Large), PropertySpace]
        public void PrepareDataForMission()
        {
            creeps.Clear();
            waves.Clear();

            for (int i = 0; i < waveDataList.Count; i++)
            {
                WaveSpawnData wave = waveDataList.Find((data) => data.WaveNum == i);
                creeps.Add(wave.Creep);
                PrepareWaveData prepareWave = new() { Count = wave.Count, Hp = wave.Hp, Reward = wave.Reward };
                waves.Add(prepareWave);
            }
        }
        [Button("4.Write Data To Mission", buttonSize: ButtonSizes.Large), PropertySpace]
        public void SetDataToMission()
        {
            if (Mission == null)
                throw new Exception("mission link is null");

            EditorUtility.SetDirty(Mission);

            Mission.CashPerWaveStart = new(WaveStartCashes);
            Mission.CreepStatsPerWave = new(creeps);

            for (int j = 0; j < waves.Count; j++)
            {
                PrepareWaveData prepareWaveData = waves[j];

                int countSpawnGroup = 0;
                for (int i = 0; i < Mission.SpawnData.Length; i++)
                {
                    foreach (Wave wave in Mission.SpawnData[i].Waves)
                    {
                        if (wave.WaveNum == j)
                            countSpawnGroup++;
                    }
                }

                if (countSpawnGroup == 0)
                {
                    Debug.LogError($"Mission {Mission.MissionIndex + 1} countSpawnGroup is 0 index {j}");
                    continue;
                }

                int countUnits = prepareWaveData.Count / countSpawnGroup;
                List<int> units = new() { (prepareWaveData.Count % countSpawnGroup == 0) ? countUnits : countUnits + 1 };

                if (countSpawnGroup > 1)
                    for (int i = 1; i < countSpawnGroup; i++)
                        units.Add(countUnits);

                int index = 0;
                for (int i = 0; i < Mission.SpawnData.Length; i++)
                {
                    for (int waveIndex = 0; waveIndex < Mission.SpawnData[i].Waves.Count; waveIndex++)
                    {
                        Wave wave = Mission.SpawnData[i].Waves[waveIndex];
                        if (wave.WaveNum == j)
                        {
                            wave.Count = units[index];
                            wave.CashReward = prepareWaveData.Reward;
                            wave.CreepHp = prepareWaveData.Hp;
                            if (waveIndex < Mission.WavesCount - 1)
                                wave.ExtraTime = 10;
                            else
                                wave.ExtraTime = 30;

                            index++;
                        }
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }



        [Button, FoldoutGroup(helperFunctions)]
        public void UnitTypesFromMission()
        {
            CreepsUsed = new();
            IsManualCreepTypes = true;
            for (int i = 0; i < Mission.WavesCount; i++)
                CreepsUsed.Add(Mission.CreepStatsPerWave[i]);
        }
        [Button, FoldoutGroup(helperFunctions)]
        public void UnitCountsFromMission()
        {
            CreepCounts = new();
            IsManualCreepCounts = true;
            for (int i = 0; i < Mission.WavesCount; i++)
            {
                int count = 0;
                foreach (var spawnData in Mission.SpawnData)
                {
                    Wave wave = spawnData.Waves.Find((wave) => wave.WaveNum == i);
                    if (wave != null)
                    {
                        count += wave.Count;
                    }
                }
                CreepCounts.Add(count);
            }

        }
        [Button, FoldoutGroup(helperFunctions)]
        public void DirtyCreateEmptyWaves()
        {
            for (int k = 0; k < Mission.SpawnData.Length; k++)
            {
                Mission.SpawnData[k].Waves.Clear();
                for (int i = 0; i < CountWave; i++)
                {
                    Wave tempWave = new Wave() { WaveNum = i };
                    Mission.SpawnData[k].Waves.Add(tempWave);
                }
            }
        }

        #region sphagetti code

        private bool SameLength()
        {
            return !IsManualCreepCounts || !IsManualCreepTypes || (CreepCounts != null && CreepsUsed != null && CreepsUsed.Count == CreepCounts.Count && CreepsUsed.Count != 0);
        }

        private void GetUnitCountsForMission()
        {
            if (IsManualCreepCounts)
                return;

            CreepCounts = new();
            for (int i = 0; i < CreepsUsed.Count; i++)
                CreepCounts.Add(GetCreepCount(CreepsUsed[i]));

        }

        private void GetUnitTypesForMission()
        {
            if (IsManualCreepTypes)
                return;

            if (PermittedFleshTypes == 0 || ArmorTypes == 0)
                throw (new Exception("PermittedFleshTypes or ArmorTypes is empty"));


            List<FleshType> originalFleshTypes = Utilities.GetFlagValues(PermittedFleshTypes);
            originalFleshTypes.Shuffle();
            List<ArmorType> originalArmorTypes = Utilities.GetFlagValues(ArmorTypes);
            originalArmorTypes.Shuffle();

            int countDopUnits = Random.Range(0, 4);

            int removeCountTypes = Random.Range(CountFleshTypes.x - 1, math.min(CountFleshTypes.y, originalFleshTypes.Count));
            List<FleshType> fleshTypes = GetMissionDataList(originalFleshTypes, removeCountTypes, countDopUnits);
            int removeCountArmor = Random.Range(CountArmorTypes.x - 1, math.min(CountArmorTypes.y, originalArmorTypes.Count));
            bool scheduleToMoveArmorTypes = (removeCountTypes == removeCountArmor && removeCountArmor != originalArmorTypes.Count - 1);
            List<ArmorType> armorTypes = GetMissionDataList(originalArmorTypes, removeCountArmor, countDopUnits, scheduleToMoveArmorTypes);

            CreepsUsed = new();
            for (int i = 0; i < fleshTypes.Count; i++)
                CreepsUsed.Add(GetCreep(fleshTypes[i], armorTypes[i]));
        }

        private static float CalculateKillPercent(ArmorType armorType, FleshType fleshType)
        {
            float killPercent;

            switch (armorType)
            {
                case ArmorType.Unarmored:
                    killPercent = 0.1f;
                    break;
                case ArmorType.Light:
                    killPercent = 0.65f;
                    break;
                case ArmorType.Heavy:
                    killPercent = 0.9f;
                    break;
                default:
                    killPercent = 0.65f;
                    break;
            }

            switch (fleshType)
            {
                case FleshType.Bio:
                    killPercent *= 0.9f;
                    break;
                case FleshType.Mech:
                    killPercent *= 1f;
                    break;
                case FleshType.Energy:
                    killPercent *= 0.8f;
                    break;
                default:
                    killPercent *= 1f;
                    break;
            }

            return killPercent;
        }

        private float GetTotalWaveGold(int length)
        {
            List<float> floats = new();
            for (int i = 0; i <= length; i++)
            {
                if (i == 0)
                    floats.Add(FirstWaveCash);
                else if (i <= 1)
                    floats.Add(FirstWaveCash + (int)(0.5f * floats[^1]));
                else
                    floats.Add(FirstWaveCash + (int)(0.5f * floats[^1] + 0.8f * floats[^2]));
            }

            return floats[^1];
        }
        private void RoundValues()
        {
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].OldRewardGold = math.round(segments[i].OldRewardGold);
                segments[i].EndGold1 = math.round(segments[i].EndGold1);
            }
        }
        private void FindOverlappingWaves(Segment segment)
        {
            segment.OverlapingWaves = waveDataList.FindAll((data) => data.StartTime < segment.EndTime && data.EndTime > segment.StartTime);
            segment.OverlapPercent = new List<float>();
            for (int i = 0; i < segment.OverlapingWaves.Count; ++i)
            {
                float minTime = math.max(segment.StartTime, segment.OverlapingWaves[i].StartTime);
                float maxTime = math.min(segment.EndTime, segment.OverlapingWaves[i].EndTime);
                float percent = (maxTime - minTime) / (segment.OverlapingWaves[i].EndTime - segment.OverlapingWaves[i].StartTime);
                segment.OverlapPercent.Add(percent);
            }
        }
        private Segment FindNextSegment(int index)
        {
            Segment result = new();
            result.StartTime = waveDataList[index].StartTime;
            result.EndTime = waveDataList[index].EndTime;
            WaveSpawnData foundData = waveDataList.Find((x) => x.StartTime < result.EndTime && x.StartTime > result.StartTime);
            while (foundData != null)
            {
                result.EndTime = foundData.StartTime;
                foundData = waveDataList.Find((x) => x.StartTime < result.EndTime && x.StartTime > result.StartTime);
            }
            return result;
        }
        private float CalculateWaveStartGold(float time)
        {
            float currTime = SpawnerSystem.FirstWaveOffset;
            int waveNum = 0;
            float result = 0;
            while (time > currTime && waveNum < CountWave)
            {
                result += WaveStartCashes[waveNum];
                currTime += SpawnerSystem.WaveTimeLength + SpawnerSystem.PauseBetweenWaves;
                waveNum++;
            }
            return result;
        }
        private float CalculateRewardGold(float time)
        {
            float result = 0;
            foreach (var data in waveDataList)
            {
                if (data.EndTime <= time)
                {
                    //Add Full Gold for killing Wave
                    result += data.CashForKill;
                }
                else if (data.StartTime < time)
                {
                    //Add Gold for killing part of Wave
                    float killTime = time - data.StartTime;
                    float percent = killTime / (data.EndTime - data.StartTime);
                    result += data.CashForKill * percent;
                }
            }
            return result;
        }
        private float GetEffectiveness(float timeFrame)
        {
            return TowersEff / 60 * timeFrame;
        }
        private int GetCreepCount(CreepStats creep)
        {
            return creep.ArmorType switch
            {
                ArmorType.Heavy => Random.Range(CountHeavyUnits.x, CountHeavyUnits.y),
                ArmorType.Light => Random.Range(CountLightUnits.x, CountLightUnits.y),
                _ => Random.Range(CountUnarmoredUnits.x, CountUnarmoredUnits.y)
            };
        }


        private CreepStats GetCreep(FleshType fleshType, ArmorType armorType)
        {
            if (CreepDictionary == null || CreepDictionary.Count == 0)
            {
                CreepDictionary = new();
                string[] creepStatsDirectories = Directory.GetDirectories("Assets/LevelsScriptableObjects", "CreepStats", SearchOption.AllDirectories);
                foreach (string directory in creepStatsDirectories)
                {
                    string[] assets = AssetDatabase.FindAssets("t:CreepStats", new[] { directory });
                    foreach (string asset in assets)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(asset);
                        CreepStats creepsStats = AssetDatabase.LoadAssetAtPath<CreepStats>(path);
                        if (creepsStats != null)
                            CreepDictionary.Add(creepsStats);

                    }
                }
                if (CreepDictionary.Count == 0)
                    throw new Exception("CreepStats is empty");
            }
            return CreepDictionary.Find((creep) => creep.ArmorType == armorType && fleshType == creep.FleshType);
        }
        private void CalculateSegmentsDPS()
        {
            for (int i = 0; i < segments.Count; i++)
            {
                float upkeep;
                if (i == 0)
                {
                    //First segment
                    segments[i].DPSOfOldTowers = 0;
                    segments[i].CashToBuild = (int)segments[i].EndGold1;

                }
                else
                {
                    segments[i].DPSOfOldTowers = segments[i - 1].DPSOfOldTowers + segments[i - 1].DPSOfNewTowers;

                    //Upkeep is zero if we have no enemies to shoot
                    if (segments[i].OverlapingWaves.Count == 0)
                        upkeep = 0;
                    else
                        upkeep = segments[i].DPSOfOldTowers / ShootingEff * segments[i].TimeFrame;

                    //If we have negative gold we would have to add cash to Reward to get more money to build towers
                    //Gold That we will have  - gold that we had - upkeep
                    segments[i].CashToBuild = (int)(segments[i].EndGold1 - segments[i - 1].EndGold1 - upkeep);
                }

                segments[i].DPSOfNewTowers = (int)((segments[i].CashToBuild * GetEffectiveness(segments[i].TimeFrame)) / segments[i].TimeFrame);
            }
        }
        private List<T> GetMissionDataList<T>(List<T> originalList, int countRemove, int countDopUnit = 0, bool scheduleToMove = false)
        {
            List<T> result = new();

            List<T> uniq = new(originalList);

            for (int i = 0; i < countRemove; i++)
                uniq.RemoveAt(uniq.Count - 1);

            int wavePeDopUnit = CountWave / (countDopUnit + 1);

            int uniqIndex = 0;
            int countUnits = (CountWave / uniq.Count) + ((uniq.Count > 1) ? 0 : Random.Range(-1, 2));
            int getCountTypes = 1;

            if (scheduleToMove)
            {
                countUnits /= 2;
                uniqIndex = uniq.Count - 1;
            }

            for (int i = 0; i < CountWave; i++)
            {
                if (countUnits <= 0)
                {
                    if (scheduleToMove)
                    {
                        countUnits = (CountWave / uniq.Count) + Random.Range(-1, 2);
                        uniqIndex = 0;
                        scheduleToMove = false;
                    }
                    else
                    {
                        countUnits = (uniq.Count - getCountTypes <= 0) ? CountWave - i : ((CountWave - i) / (uniq.Count - getCountTypes)) + Random.Range(-1, 2);
                        getCountTypes++;

                        if (uniqIndex < uniq.Count - 1)
                            uniqIndex++;
                    }
                }

                result.Add(uniq[uniqIndex]);

                if (countDopUnit > 0 && (i % (wavePeDopUnit) == wavePeDopUnit - 1) && (i != CountWave - 1))
                {
                    List<T> tempUniq = new(originalList);

                    for (int j = i - 1; j < i + 1; j++)
                    {
                        if (tempUniq.Contains(result[j]))
                            tempUniq.Remove(result[j]);
                    }

                    var old = result[^1];
                    result[^1] = (tempUniq.Count > 0) ? tempUniq[Random.Range(0, tempUniq.Count)] : originalList[Random.Range(0, originalList.Count)];

                    Debug.Log($"old: {old} new: {result[^1]}");
                }

                countUnits--;
            }


            return result;
        }

        public void FillCashes()
        {
            WaveStartCashes = new();
            for (int i = 0; i < CountWave; i++)
            {
                //WaveStartCashes.Add(Random.Range(i == 0 ? FirstWaveCash : WaveStartCashes[i - 1], FirstWaveCash + i * FirstWaveCash));
                if (i == 0)
                    WaveStartCashes.Add(FirstWaveCash);
                else if (i <= 1)
                    WaveStartCashes.Add(FirstWaveCash + (int)(0.5f * WaveStartCashes[^1]));
                else
                    WaveStartCashes.Add(FirstWaveCash + (int)(0.5f * WaveStartCashes[^1] + 0.8f * WaveStartCashes[^2]));

                //WaveStartCashes.Add(FirstWaveCash + /*i * FirstWaveCash*/ +(i > 0 ? ((int)(0.6f * WaveStartCashes[^1])) : 0));
                //WaveStartCashes.Add((int) (FirstWaveCash * math.pow(1.3f, i)));
            }
        }
        #endregion

    }

    [Serializable]
    public class PrepareWaveData
    {
        [HorizontalGroup("1")] public int Count;
        [HorizontalGroup("1")] public int Hp;
        [HorizontalGroup("1")] public int Reward;
    }
    public class WaveSpawnData
    {
        [TableColumnWidth(60, false)]
        public float StartTime;
        [TableColumnWidth(60, false)]
        public float EndTime;
        [GUIColor(214f / 255, 224f / 225, 17f / 255, 1f)]
        [TableColumnWidth(80, false)]
        public float StartGold;
        [GUIColor(214f / 255, 224f / 225, 17f / 255, 1f)]
        [TableColumnWidth(80, false), ShowInInspector]
        public int CashForKill => Reward * Count;

        [GUIColor(255f / 255, 30f / 225, 30f / 255, 1f), TableColumnWidth(60, false)]
        public int Hp;
        [GUIColor(214f / 255, 224f / 225, 17f / 255, 1f), TableColumnWidth(40, false)]
        public int Reward;
        [TableColumnWidth(40, false)]
        public int Count;

        [GUIColor(255f / 255, 30f / 225, 30f / 255, 1f)]
        public float FullHp;
        public int WaveNum;
        public CreepStats Creep;
    }

    [Serializable]
    public class Segment
    {
        [TableColumnWidth(60, false)]
        public float StartTime;
        [TableColumnWidth(60, false)]
        public float EndTime;
        public float TimeFrame => EndTime - StartTime;

        [GUIColor(214f / 255, 224f / 225, 17f / 255, 1f)]
        [TableColumnWidth(80, false)]
        public float CashToBuild;

        public float CurrentRewardGold;
        public float OldRewardGold;

        public float EndGold1;


        [ShowInInspector]
        public float TotalDamage => (DPSOfNewTowers * 0.5f + DPSOfOldTowers) * TimeFrame;

        public float DPS => DPSOfNewTowers + DPSOfOldTowers;

        public float CalculateRewardGold()
        {
            float result = 0;
            for (int i = 0; i < OverlapingWaves.Count; i++)
            {
                result += OverlapingWaves[i].CashForKill * OverlapPercent[i];
            }
            return result;
        }
        [HideInInspector]
        public List<WaveSpawnData> OverlapingWaves;
        [HideInInspector]
        public List<float> OverlapPercent;

        public float DPSOfOldTowers;
        public float DPSOfNewTowers;
        public float TotalHp1;
    }
}

#endif