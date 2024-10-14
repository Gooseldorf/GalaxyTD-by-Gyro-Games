using ECSTest.Components;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Systems.Attakers;
using Unity.Entities;


public class SimpleUpgrade : SerializedScriptableObject
{
    [OdinSerialize, ShowInInspector] private AttackStats bonus;

    public AttackStats Bonus => bonus;
    
    public void ApplyOneBonus(Entity towerEntity, ref EntityManager manager, ref AttackerComponent attackerComponent)
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