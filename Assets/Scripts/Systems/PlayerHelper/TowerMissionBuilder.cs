using CardTD.Utilities;
using ECSTest.Components;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Systems.PlayerHelper
{
    public class TowerMissionBuilder : SerializedMonoBehaviour
    {
        [OdinSerialize] private List<MissionTowersBuilderData> missionTowersBuilderData = new();

        private void Awake()
        {
            Messenger.AddListener(GameEvents.Restart, BuildTowers);
        }

        private void OnDestroy()
        {
            Messenger.AddListener(GameEvents.Restart, BuildTowers);
        }

        private void Start()
        {
            BuildTowers();
        }

        // private void BuildTowerStarter()
        // {
        //     BuildTowers();
        //     // StartCoroutine(BuildTowerCoroutine());
        // }
        // private IEnumerator BuildTowerCoroutine()
        // {
        //     yield return new WaitForSeconds(1f);
        //     BuildTowers();
        // }

        private void BuildTowers()
        {
            MissionTowersBuilderData missionData = missionTowersBuilderData.Find((towerData) => towerData.MissionName == GameServices.Instance.CurrentMission.name);

            if (missionData == null)
                return;

            World world = World.DefaultGameObjectInjectionWorld;
            EntityManager manager = world.EntityManager;

            EntityQuery dropZones = manager.CreateEntityQuery(
                ComponentType.ReadOnly<DropZoneComponent>(),
                ComponentType.ReadOnly<Identifiable>()
            );

            NativeArray<Entity> dropZoneEntities = dropZones.ToEntityArray(Allocator.Temp);
            NativeArray<Identifiable> dropZoneIdes = dropZones.ToComponentDataArray<Identifiable>(Allocator.Temp);


            for (int i = 0; i < dropZoneIdes.Length; i++)
            {
                foreach (TowerBuilderData towerData in missionData.Towers)
                {
                    if (dropZoneIdes[i].Id == towerData.DropZoneIndex)
                    {
                        GameServices.Instance.BuildTower(towerData.Factory.GetAssembledTower(), dropZoneEntities[i], true);
                        break;
                    }
                }
            }


            dropZoneEntities.Dispose();
            dropZoneIdes.Dispose();
        }
    }
}

[Serializable]
public class MissionTowersBuilderData
{
    public string MissionName = "Mission_01";
    [OdinSerialize] public List<TowerBuilderData> Towers;
}

[Serializable]
public class TowerBuilderData
{
    public int DropZoneIndex;
    [OdinSerialize] public TowerFactory Factory;
}