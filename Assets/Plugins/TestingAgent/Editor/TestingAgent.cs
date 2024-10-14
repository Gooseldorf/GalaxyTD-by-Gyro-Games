#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using TestingAgent.Editor.Utils;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace TestingAgent.Editor
{
    [HideMonoScript]
    [DefaultExecutionOrder(-100)]
    public sealed partial class TestingAgent : SerializedMonoBehaviour
    {
        private const int cashRewardPerUnit = 0;
        
        private void Awake()
        {
            DisableVisualizationSystems();
            hpModifierStepSlice = hpModifierStep;
            
            GameInitialization initialization = FindFirstObjectByType<GameInitialization>();
            gameData = initialization.GetFieldValue<GameData>("gameData");
            DataManager.Instance.GameData.SetFieldValue("saveToDisk", false);
            mission = initialization.GetFieldValue<Mission>("mission"); // Get active and change Mission.
            maxTestIteration = waves.Entries.Count;
            loseWinAttemptsTemp = loseWinAttempts;
            Mission missionClone = MissionFactory.Clone(mission);
            
            // Sort to begin from greater creep count. E.g. [90, 150, 80, 200] >> [200, 150, 90, 80]
            waves.Entries.Sort((x, y) => y.Count.CompareTo(x.Count));
            
            // Change clone of original mission.
            availableCash = resultCash = Random.Range(minCash, maxCash);
            List<int> startCash = new List<int>(){availableCash};

            missionClone.CashPerWaveStart = startCash; 
            
            missionClone.HpModifier = currentHpModifier = defaultHpModifier;
            missionClone.CreepStatsPerWave = new List<CreepStats>() {waves.Stats };
            missionClone.SpawnData = new[]
            {
                new SpawnGroup
                {
                    Waves = new List<Wave>(new[]
                    {
                        new Wave 
                        { 
                            WaveNum = 0, 
                            //SpawnUnits = new Dictionary<CreepStats, int> { { waves.Stats, waves.Entries[0].Count } } 
                            Count =  waves.Entries[0].Count,
                            // Creep = waves.Stats,
                            CreepHp = (int)waves.Stats.MaxHP,
                            CashReward = cashRewardPerUnit,
                        }
                    }),
                    CombinedZones = missionClone.SpawnData[0].CombinedZones,
                    SpawnPositions = missionClone.SpawnData[0].SpawnPositions,
                }
            };
            initialization.SetFieldValue("mission", missionClone);
            
            Subscribe();
        }

        private void Start()
        {
            towerFactories =  gameData.TowerFactories
                .Where(x => randomTowers || allowedTowers.HasFlag(x.TowerId))
                .Select(x => ((TowerFactory)x).Clone())
                .ToList();
            
            ReplayManager.Instance.RecordReplay = false;
            
            if(!enableVisual)
            {
                // Disable map visual here because MapCreator work with level grid in awake.
                FindFirstObjectByType<LevelGrid>().gameObject.SetActive(false);
            }
            
            // Set default delta time.
            FixedStepSimulationSystemGroup mainGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            mainGroup.World.MaximumDeltaTime = float.PositiveInfinity;
            
            testWatch.Start();
            iterationWatch.Start();
            
            BuildTowers(ref availableCash);
            UpgradeTowers(ref availableCash);
            currentResult = CreateDefaultAsset(mission.name);
            GameServices.Instance.SetTimeScale(timeScale);
            GetCashForReload();
            initialized = true;
            SkipTime();
        }

        private void GetCashForReload() => InGameCheats.AddCash(extraCashForReload);

        private void OnDestroy()
        {
            Unsubscribe();
            SaveAssetsOnHaltTesting();
        }
        
        private bool TryCompleteAndIterateTest(bool endCondition)
        {
            if (!endCondition)
                return false;
            
            CompleteCurrentTest();

            currentHpModifier = defaultHpModifier;
            hpModifierStepSlice = hpModifierStep;
                
            if(currentTestIteration == waves.Entries.Count)
            {
                if (testDirectivesOnly && currentTestingDirective != directiveProfile.Directives.Count - 1)
                {
                    Debug.Log($"[TEST DIRECTIVE COMPLETE! Attempts: {attempts} | HpModifier: {currentHpModifier}]");
                    currentResult.Current.IsCompleted = true;
                    currentTestIteration = 0;
                    do
                    {
                        currentTestingDirective++;
                        AllEnums.TowerId towerId = directiveProfile.Directives[currentTestingDirective].TowerId;

                        if(towerFactories.TrueForAll(x => towerId.HasFlag(x.TowerId)))
                            break;
                        
                    } 
                    while (currentTestingDirective != directiveProfile.Directives.Count - 1);

                    if (currentTestingDirective == directiveProfile.Directives.Count - 1)
                    {
                        testWatch.Stop();
                        iterationWatch.Stop();
                    
                        currentResult.IsCompleted = true;
                        currentResult.ElapsedTime = testWatch.Elapsed.ToString("hh\\:mm\\:ss");
                    
                        currentResult.Current.IsCompleted = true;
                        currentResult.Current.ElapsedTime = iterationWatch.Elapsed.ToString("hh\\:mm\\:ss");;
                
                        Debug.Log($"[TEST FULLY COMPLETE! Attempts: {attempts} | HpModifier: {currentHpModifier}]");
                        EditorApplication.isPlaying = false;
                        return true;
                    }
                    
                    rebuildWithNewDirective = true;
                    AddEntry(currentResult, waves.Entries[currentTestIteration].Count);
                    iterationWatch.Restart();
                }
                else
                {
                    testWatch.Stop();
                    iterationWatch.Stop();
                    
                    currentResult.IsCompleted = true;
                    currentResult.ElapsedTime = testWatch.Elapsed.ToString("hh\\:mm\\:ss");
                    
                    currentResult.Current.IsCompleted = true;
                    currentResult.Current.ElapsedTime = iterationWatch.Elapsed.ToString("hh\\:mm\\:ss");
                
                    Debug.Log($"[TEST FULLY COMPLETE! Attempts: {attempts} | HpModifier: {currentHpModifier}]");
                    EditorApplication.isPlaying = false;

                    return true;
                }
            }
            
            return false;
        }
        
        private void CompleteCurrentTest()
        {
            if (currentTestIteration == maxTestIteration - 1)
            {
                currentTestIteration++;
                return;
            }
            
            TestResultEntry currentEntry = currentResult.Current;
            
            currentEntry.HpModifier = currentHpModifier;
            currentEntry.IsCompleted = true;
            currentEntry.ElapsedTime = iterationWatch.Elapsed.ToString("hh\\:mm\\:ss"); 
            currentTestIteration++;
            attempts = 0;
            iterationWatch.Restart();
            
            Debug.Log($"[SUBTEST COMPLETE ({currentEntry.CreepCount})] Attempts: {attempts} (W: {currentEntry.Wins.Count} | L: {currentEntry.Lose.Count}) | HpModifier: {currentHpModifier}]");
            
            if (currentTestIteration <= maxTestIteration)
                AddEntry(currentResult, waves.Entries[currentTestIteration].Count);
            
            EditorUtility.SetDirty(currentResult);
        }
        
        private void OnRestartHandler()
        {
            if(builtTowers.Count == 0)
                return;
            
            Debug.Log("Restart");
            
            GameServices.Instance.SetTimeScale(timeScale);
            RebuildTowers();
            ReUpgradeTowers();
            attempts++;
            isBusy = false;
            SkipTime();
        }
    }
}

#endif