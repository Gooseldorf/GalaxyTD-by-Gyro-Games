#if UNITY_EDITOR
using ECSTest.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using static AllEnums;

namespace TestingAgent.Editor
{
    public sealed partial class TestingAgent
    {
        private TestResult CreateDefaultAsset(string missionName)
        {
            TestResult result = testDirectivesOnly
                ? ScriptableObject.CreateInstance<DirectivesTestResult>()
                : ScriptableObject.CreateInstance<TestResult>();

            // Main asset.
            result.Mission = mission;
            result.TowerFactoryData = gameData;
            result.IsCompleted = false;
            result.BuiltTowers = new List<TowerInfo>(builtTowers);
            result.Stats = waves.Stats;
            result.Cash = resultCash;

            if (testDirectivesOnly && result is DirectivesTestResult directivesTestResult)
            {
                directivesTestResult.TestedDirectives = new List<WeaponPart>(directiveProfile.Directives);
                directivesTestResult.directivesSequence = new List<string>(directiveProfile.DirSequence);
            }
            
            string allowedTowersNames = "(";
            if (!randomTowers)
            {
                foreach (TowerId towerID in Enum.GetValues(typeof(TowerId)))
                    if (allowedTowers.HasFlag(towerID))
                        allowedTowersNames += $"_{towerID}";
            }
            else
                allowedTowersNames = "(_randomTowers";

            allowedTowersNames += "_)";

            string assetPath = testDirectivesOnly
                ? $"{TEST_RESULTS_PATH}/DirectivesTest_{missionName}{allowedTowersNames}_{GetResultNumber(missionName, allowedTowersNames)}.asset"
                : $"{TEST_RESULTS_PATH}/Test_{missionName}{allowedTowersNames}_{GetResultNumber(missionName, allowedTowersNames)}.asset";
            
            AssetDatabase.CreateAsset(result, assetPath);
            result = AssetDatabase.LoadAssetAtPath<TestResult>(assetPath);
            AddEntry(result, waves.Entries[0].Count); // First sub-asset.
            return result;
        }

        private int GetResultNumber(string missionName, string allowedTowersNames)
        {

            List<string> files = testDirectivesOnly
                ? Directory.GetFiles(TEST_RESULTS_PATH).Where(x => x.Contains($"DirectivesTest_{missionName}{allowedTowersNames}") && x.EndsWith(".asset")).ToList()
                : Directory.GetFiles(TEST_RESULTS_PATH).Where(x => x.Contains($"Test_{missionName}{allowedTowersNames}") && x.EndsWith(".asset")).ToList();

            if (files.Count == 0)
                return 0;
            
            files = files.Select(x =>
            {
                string result = testDirectivesOnly
                    ? x.Replace($@"{TEST_RESULTS_PATH}\DirectivesTest_{missionName}{allowedTowersNames}_", string.Empty)
                    : x.Replace($@"{TEST_RESULTS_PATH}\Test_{missionName}{allowedTowersNames}_", string.Empty);
                
                return result.Replace(".asset", string.Empty);
            }).ToList();
            
            int maxValue = files.Select(x=>int.TryParse(x,out int result)?result:0).ToList().Max();
            return maxValue + 1;
        }

        private void AddEntry(TestResult result, int creepCount)
        {
            TestResultEntry testEntry = CreateSubTestAsset(creepCount);
            result.Entries.Add(testEntry);
            AssetDatabase.AddObjectToAsset(testEntry, result);
            AssetDatabase.SaveAssets();
        }

        private TestResultEntry CreateSubTestAsset(int creepCount)
        {
            TestResultEntry testEntry = testDirectivesOnly
                ? ScriptableObject.CreateInstance<DirectiveTestResultEntry>()
                : ScriptableObject.CreateInstance<TestResultEntry>();
            
            testEntry.name = testDirectivesOnly
                ? $"DirectiveTest_{directiveProfile.Directives[currentTestingDirective].name}_{waves.Stats.name}_{creepCount}"
                : $"SubTest_{waves.Stats.name}_{creepCount}";

            if (testDirectivesOnly)
                ((DirectiveTestResultEntry)testEntry).Directive = directiveProfile.Directives[currentTestingDirective];
            
            testEntry.HpModifier = currentHpModifier;
            testEntry.CreepCount = creepCount;

            // foreach (TowerInfo info in builtTowers)
            //     testEntry.Towers.Add(info.PartialClone());
            
            return testEntry;
        }
        
        private void CollectTowerStatistics(IList<TowerInfo> towers)
        {
            using EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AttackerStatisticComponent>()
                .Build(EntityManager);
            
            if(query.IsEmpty)
                return;
            
            using NativeArray<AttackerStatisticComponent> statisticComponents = query.ToComponentDataArray<AttackerStatisticComponent>(Allocator.Temp);
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            foreach (TowerInfo towerInfo in towers)
            {
                int index = 0;
                for (int i = 0; i < entities.Length; i++, index++)
                {
                    EntityHolderComponent holderComponent = EntityManager.GetComponentData<EntityHolderComponent>(entities[i]);
                    Identifiable dropZoneId = EntityManager.GetComponentData<Identifiable>(holderComponent.Entity);
                    
                    if(dropZoneId.Id == towerInfo.DropZoneIndex)
                        break;
                }

                AttackerStatisticComponent statisticComponent = statisticComponents[index];
                AttackerComponent attackerComponent = EntityManager.GetComponentData<AttackerComponent>(entities[index]);

                towerInfo.Damage = statisticComponent.CreepDamage;
                towerInfo.Overheat = statisticComponent.OverheatDamage;
                towerInfo.Kills = statisticComponent.Kills;
                towerInfo.Shots = statisticComponent.Shoots;

                towerInfo.BuildCost = statisticComponent.CashBuildSpent;
                towerInfo.UpgradeCost = statisticComponent.CashUpgradeSpent;
                towerInfo.ReloadCost = statisticComponent.CashReloadSpent;
                towerInfo.OverallCost = towerInfo.BuildCost + towerInfo.UpgradeCost + towerInfo.ReloadCost;

                towerInfo.CostOverDamage = towerInfo.Damage / towerInfo.ReloadCost;
                towerInfo.CostPerShot = attackerComponent.AttackStats.ReloadStats.BulletCost;
            }
        }       
        
        private void SaveAssetsOnHaltTesting()
        {
            currentResult.Current.HpModifier = currentHpModifier;
            currentResult.ElapsedTime = testWatch.Elapsed.ToString("hh\\:mm\\:ss");
            EditorUtility.SetDirty(currentResult);

            foreach (TestResultEntry entry in currentResult.Entries)
                EditorUtility.SetDirty(entry);
            
            AssetDatabase.SaveAssets();
        }
    }
}
#endif