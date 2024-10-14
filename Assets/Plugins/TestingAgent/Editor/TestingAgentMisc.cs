#if UNITY_EDITOR
using ECSTest.Systems;
using Sirenix.OdinInspector;
using System.Collections;
using System.IO;
using UI;
using Unity.Core;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace TestingAgent.Editor
{
    public sealed partial class TestingAgent
    {
        private void OnValidate()
        {
            if (minCash > maxCash)
                minCash = maxCash;
            
            if (maxCash < minCash)
                maxCash = minCash;
        }
        
        [Button]
        private void SkipTime()
        {
            StartCoroutine(SkipCoroutine());
        }

        IEnumerator SkipCoroutine()
        {
            yield return null;
            TimeData timeData = World.DefaultGameObjectInjectionWorld.Time;
            World.DefaultGameObjectInjectionWorld.PushTime(new TimeData(timeData.ElapsedTime + pushTimeValue, pushTimeValue));
        }

        private void Update()
        {
            //SkipTime();
        }

        private void DisableVisualizationSystems()
        {
            // if(enableVisual)
            //     return;
            
            FindFirstObjectByType<GameUIManager>().gameObject.SetActive(false);
            FindFirstObjectByType<RangeVisualizator>().gameObject.SetActive(false);

            if(enableVisual)
                return;
            
            WorldUnmanaged world = World.DefaultGameObjectInjectionWorld.Unmanaged;
            
            ref SystemState spawnZone = ref world.GetExistingSystemState<SpawnZoneVisualizator>();
            ref SystemState critters = ref world.GetExistingSystemState<CritterVisualizatorSystem>();
            ref SystemState towerVisualizator = ref world.GetExistingSystemState<TowerVisualizatorSystemBase>();
            ref SystemState towerVisualisation = ref world.GetExistingSystemState<TowerEffectVisualisationSystem>();
            ref SystemState projectileVisualizator = ref world.GetExistingSystemState<ProjectileVisualizatorSystemBase>();
            ref SystemState animationSystem = ref world.GetExistingSystemState<AnimationSystem>();
            ref SystemState visualizator = ref world.GetExistingSystemState<VisualizatorSystemBase>(); 
            ref SystemState uiEvents = ref world.GetExistingSystemState<UIEventSystem>();
            ref SystemState brg = ref world.GetExistingSystemState<BRGSystem>();

            brg.Enabled = false;
            critters.Enabled = false;
            spawnZone.Enabled = false;
            towerVisualizator.Enabled = false;
            towerVisualisation.Enabled = false;
            projectileVisualizator.Enabled = false;
            animationSystem.Enabled = false;
            visualizator.Enabled = false;
            uiEvents.Enabled = false;

            // state0.Enabled = state1.Enabled = state2.Enabled = state4.Enabled = 
            //     state5.Enabled = state6.Enabled = false;
        }

        [Button]
        private void ClearResultFolder()
        {
            if (Directory.Exists(TEST_RESULTS_PATH))
                FileUtil.DeleteFileOrDirectory(TEST_RESULTS_PATH);
            
            Directory.CreateDirectory(TEST_RESULTS_PATH);
            AssetDatabase.Refresh();
        }
    }
}
#endif