using ECSTest.Components;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using Systems.Attakers;
using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace.Towers
{
    public class GameUpgrade : SerializedScriptableObject
    {
        [SerializeField] private int numberOfBonusesToApply = 5;
        [OdinSerialize, ShowInInspector] private AttackStats[] minorBonuses;

        public int GetCost(int level)
        {
            return (level + 1) * 50;
        }

        //public List<AttackStats> GetBunuses()
        //{
        //    List<AttackStats> result = new();

        //    result.Add(minorBonuses[0]);
        //    for (int i = 0; i < numberOfBonusesToApply - 1; i++)
        //    {
        //        result.Add(minorBonuses[Random.Range(0, minorBonuses.Length)]);
        //    }
        //    return result;
        //}

        public void ApplyBonus(Entity towerEntity, EntityManager manager, AttackerComponent attackerComponent)
        {

            ApplyOneBonus(towerEntity, ref manager, ref attackerComponent, minorBonuses[0]);
            for (int i = 0; i < numberOfBonusesToApply - 1; i++)
                ApplyOneBonus(towerEntity, ref manager, ref attackerComponent, minorBonuses[Random.Range(0, minorBonuses.Length)]);

            manager.SetComponentData(towerEntity, attackerComponent);
        }

        private void ApplyOneBonus(Entity towerEntity, ref EntityManager manager, ref AttackerComponent attackerComponent, AttackStats bonus)
        {
            attackerComponent.AttackStats *= bonus.GetStats();
            switch (bonus)
            {
                case GunStats gunStats:
                    GunStatsComponent gs = manager.GetComponentData<GunStatsComponent>(towerEntity);
                    gunStats.MultiplyAttackStats(ref gs);
                    manager.SetComponentData(towerEntity, gs);
                    break;
                case RocketStats rocketStats:
                    RocketStatsComponent rs = manager.GetComponentData<RocketStatsComponent>(towerEntity);
                    rocketStats.MultiplyAttackStats(ref rs);
                    manager.SetComponentData(towerEntity, rs);
                    break;
                case MortarStats mortarStats:
                    MortarStatsComponent ms = manager.GetComponentData<MortarStatsComponent>(towerEntity);
                    mortarStats.MultiplyAttackStats(ref ms);
                    manager.SetComponentData(towerEntity, ms);
                    break;
            }
        }
    }
}

