using CardTD.Utilities;
using ECSTest.Components;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TestingAgent.Editor
{
    public sealed partial class TestingAgent
    {
        private void BuildTowers(ref int cash)
        {
            using EntityQuery query = EntityManager.CreateEntityQuery(typeof(DropZoneComponent));
            
            if (query.IsEmpty)
            {
                query.Dispose();
                return;
            }
            
            using NativeArray<Entity> dropZoneEntities = query.ToEntityArray(Allocator.Temp);
            List<Entity> dropZones = new(dropZoneEntities);
            int maxTowersCount = dropZones.Count;
            int towersBuilt = 0;
            
            while (true)
            {
                int cashToBuildTower = cash;
                
                List<TowerFactory> availableTowers = towerFactories.Where(x => x.TowerPrototype.BuildCost <= cashToBuildTower).ToList();;
                
                if (availableTowers.Count == 0 || towersBuilt == maxTowersCount)
                    break;

                int randomDropZoneIndex = Random.Range(0, dropZones.Count);
                TowerFactory randomFactory = availableTowers.GetRandomValue();

                if (testDirectivesOnly)
                {
                    ((Slot)randomFactory.Directives[0]).WeaponPart = directiveProfile.Directives[currentTestingDirective];
                    randomFactory.RefreshTower();
                }
                
                Tower tower = randomFactory.GetAssembledTower();
                GameServices.Instance.BuildTower(tower, dropZones[randomDropZoneIndex]);
                
                builtTowers.Add(new TowerInfo
                {
                    Factory = randomFactory,
                    Tower = tower,
                    TowerId = tower.TowerId,
                    DropZoneIndex = EntityManager.GetComponentData<Identifiable>(dropZones[randomDropZoneIndex]).Id
                });
                
                dropZones.RemoveAt(randomDropZoneIndex);
                cash -= tower.BuildCost;
                towersBuilt++;
            }

            dropZones.Clear();
        }

        private void RebuildTowers()
        {
            if(builtTowers.Count == 0)
                return;
            
            using EntityQuery query = EntityManager.CreateEntityQuery(typeof(DropZoneComponent));
            using NativeArray<Entity> dropZoneEntities = query.ToEntityArray(Allocator.Temp);
            List<Entity> dropZones = new(dropZoneEntities);
            
            foreach (TowerInfo towerInfo in builtTowers)
            {
                Entity dropZone = dropZones.Find(x => EntityManager.GetComponentData<Identifiable>(x).Id == towerInfo.DropZoneIndex);

                if (testDirectivesOnly && rebuildWithNewDirective)
                {
                    ((Slot)towerInfo.Factory.Directives[0]).WeaponPart = directiveProfile.Directives[currentTestingDirective];
                    towerInfo.Factory.RefreshTower();
                    towerInfo.Tower = towerInfo.Factory.GetAssembledTower();
                }
                
                GameServices.Instance.BuildTower(towerInfo.Tower, dropZone);
            }

            rebuildWithNewDirective = false;
            dropZones.Clear();
        }

        private void UpgradeTowers(ref int cash)
        {
            if(cash <= 0)
                return;
            
            using EntityQuery query = EntityManager.CreateEntityQuery(typeof(AttackerComponent));
            
            if (query.IsEmpty)
            {
                query.Dispose();
                return;
            }
            
            NativeArray<Entity> towerEntities = query.ToEntityArray(Allocator.Temp);
            List<Entity> towers = new(towerEntities);

            while (towers.Count > 0 && cash > 0)
            {
                for (int i = 0; i < towers.Count; i++)
                {
                    AttackerComponent component = EntityManager.GetComponentData<AttackerComponent>(towers[i]);

                    if (GameServices.Instance.CanTowerUpgrade(component, out CompoundUpgrade upgrade))
                    {
                        int cost = upgrade.Cost;

                        if (cost <= cash)
                        {
                            
                            EntityHolderComponent dropZoneHolder = EntityManager.GetComponentData<EntityHolderComponent>(towers[i]);
                            int dropZoneId = EntityManager.GetComponentData<Identifiable>(dropZoneHolder.Entity).Id;

                            TowerInfo info = builtTowers.First(x => x.DropZoneIndex == dropZoneId);
                            
                            if (info.LevelUpCount + 1 < maxTowerLvl) 
                            {
                                GameServices.Instance.UpgradeTower(towers[i]);

                                info.LevelUpCount++;
                                cash -= cost;
                                continue;
                            }
                        }
                    }
                    
                    towers.RemoveAt(i);
                    i--;
                }
            }

            towers.Clear();
            
            if (towerEntities.IsCreated)
                towerEntities.Dispose();
        }

        private void ReUpgradeTowers()
        {
            using EntityQuery query = EntityManager.CreateEntityQuery(typeof(AttackerComponent));
            using NativeArray<Entity> towerEntities = query.ToEntityArray(Allocator.Temp);
            List<Entity> towers = new(towerEntities);

            foreach (TowerInfo towerInfo in builtTowers)
            {
                if(towerInfo.LevelUpCount == 0)
                    continue;
                
                Entity tower = towers.Find(x =>
                {
                    Entity dropZone = EntityManager.GetComponentData<EntityHolderComponent>(x).Entity;
                    int dropZoneId = EntityManager.GetComponentData<Identifiable>(dropZone).Id;
                    return dropZoneId == towerInfo.DropZoneIndex;
                });

                for (int i = 0; i < towerInfo.LevelUpCount; i++) 
                    GameServices.Instance.UpgradeTower(tower);
            }
            
            towers.Clear();
        }
    }
}