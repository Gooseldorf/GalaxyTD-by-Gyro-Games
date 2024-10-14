using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class FirerateFromReloadTag : OnReloadTag
{
    [SerializeField, BoxGroup("Time Bounds")] private float minPercentBound = 0.01f;
    [SerializeField, BoxGroup("Time Bounds")] private float maxPercentBound = 0.05f;
    [SerializeField, BoxGroup("Time Bounds")] private float minAmmoBound = 10f;
    [SerializeField, BoxGroup("Time Bounds")] private float maxAmmoBound = 100f;
    
    public override void OnReload(Entity tower, EntityManager manager)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        
        float percent = Utilities.GetLerpedValue(minAmmoBound, maxAmmoBound, minPercentBound, maxPercentBound, attackerComponent.AttackStats.ReloadStats.MagazineSize);
        
        attackerComponent.AttackStats.ShootingStats.ShotDelay /= (1 + percent);
        manager.SetComponentData(tower, attackerComponent);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/FirerateFromReload");
}