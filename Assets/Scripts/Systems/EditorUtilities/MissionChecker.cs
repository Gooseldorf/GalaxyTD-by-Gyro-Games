#if UNITY_EDITOR

using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Systems
{
    public class MissionChecker : MonoBehaviour
    {
        public List<Mission> FindMissions = new();


        [Button]
        private void FindMissionWithNoDirectives()
        {
            //FindMissions.Clear();
            //string[] guids = AssetDatabase.FindAssets("t:" + nameof(Mission)); //FindAssets uses tags check documentation for more info
            //Mission[] missions = new Mission[guids.Length];
            //for (int i = 0; i < guids.Length; i++) //probably could get optimized
            //{
            //    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            //    missions[i] = AssetDatabase.LoadAssetAtPath<Mission>(path);
            //}

            //foreach (Mission mission in missions)
            //{
            //    if (mission.Reward==null || mission.Reward.Items==null || mission.Reward.Items.Length == 0)
            //    {
            //        FindMissions.Add(mission);
            //        continue;
            //    }

            //    foreach (WeaponPart directive in mission.Reward.Items)
            //    {
            //        if (directive == null)
            //        {
            //            FindMissions.Add(mission);
            //            break;
            //        }
            //    }
            //}
            Debug.LogError("This function removed by Ivan, to find mission without Directives go to UnlockManager");
        }


        [Button, FoldoutGroup("DropZones")]
        private void CheckMissionDropZones()
        {
            FindMissions.Clear();
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(Mission)); //FindAssets uses tags check documentation for more info
            Mission[] missions = new Mission[guids.Length];
            for (int i = 0; i < guids.Length; i++) //probably could get optimized
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                missions[i] = AssetDatabase.LoadAssetAtPath<Mission>(path);
            }

            foreach (var mission in missions)
            {
                List<int> idsDropZones = new();

                foreach (DropZone dropZone in mission.DropZones)
                {
                    if (dropZone.IsPowered)
                        idsDropZones.Add(dropZone.Id);
                }


                foreach (var energyCore in mission.EnergyCores)
                {
                    if (FindMissions.Contains(mission))
                        break;

                    foreach (int powerableId in energyCore.Powerables)
                    {
                        if (idsDropZones.Contains(powerableId))
                        {
                            FindMissions.Add(mission);
                            break;
                        }
                    }
                }
            }
        }

        [Button, FoldoutGroup("DropZones")]
        private void RemovePoweredDropZoneFromEnergyCoresPowerables()
        {
            for (int i = 0; i < FindMissions.Count; i++)
            {
                Mission mission = FindMissions[i];

                List<int> idsDropZones = new();

                foreach (DropZone dropZone in mission.DropZones)
                {
                    if (dropZone.IsPowered)
                        idsDropZones.Add(dropZone.Id);
                }


                EditorUtility.SetDirty(mission);

                for (int index = 0; index < mission.EnergyCores.Length; index++)
                {
                    EnergyCore energyCore = mission.EnergyCores[index];

                    List<int> newIds = new();
                    foreach (int powerableId in energyCore.Powerables)
                    {
                        if (!idsDropZones.Contains(powerableId))
                        {
                            newIds.Add(powerableId);
                        }
                    }

                    mission.EnergyCores[index].Powerables = newIds;
                }
            }

            AssetDatabase.SaveAssets();
        }
    }
}

#endif