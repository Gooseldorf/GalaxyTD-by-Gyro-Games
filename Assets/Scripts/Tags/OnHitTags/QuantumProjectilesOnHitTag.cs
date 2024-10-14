using ECSTest.Components;
using I2.Loc;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class QuantumProjectilesOnHitTag : OnHitTag
{
    [SerializeField] private List<OnHitTag> randomOnHitTagEffects = new List<OnHitTag>(4);
    [SerializeField] private List<float> extraDamagePercent = new List<float>(3);

    [SerializeField] private int bulletsToRestoreAmount = 1;

    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        int rand = Random.Range(0, 9);// last = nothing
        onHitTagData.GunCollisionEvent.IsEnhanced = true;

        switch (rand)
        {
            case 0:
            case 1:
            case 2://damage modify
                damage += damage * extraDamagePercent[rand];
                break;
            case 3://restore bullets
                AttackerComponent component = manager.GetComponentData<AttackerComponent>(tower);
                component.Bullets = math.min(component.Bullets + bulletsToRestoreAmount, component.AttackStats.ReloadStats.MagazineSize); ;
                manager.SetComponentData(tower, component);
                break;
            case 4://stun
            case 5://slow
            case 6://radiation
            case 7://aoe
                randomOnHitTagEffects[rand - 4].OnHit(onHitTagData, ref creepComponent, ref damage, tower, manager, ecb);
                break;
            default:
                break;
        }

        DamageSystem.ShowTagEffect(ecb, onHitTagData.GunCollisionEvent.Point, 0, AllEnums.TagEffectType.Quantum, "QuantumEffect");
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/QuantumProjectiles");
}