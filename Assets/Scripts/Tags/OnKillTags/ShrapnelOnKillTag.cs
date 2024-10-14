using CardTD.Utilities;
using ECSTest.Components;
using ECSTest.Systems;
using I2.Loc;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class ShrapnelOnKillTag : OnKillTag
{
    [SerializeField] private int shrapnelAmount = 1;
    [SerializeField] private float overkillPercent = 1;
    [SerializeField] private AttackerComponent attackerComponent;
    [SerializeField] private GunStatsComponent gunStatsComponent;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        PositionComponent creepPositionComponent = handler.Manager.GetComponentData<PositionComponent>(handler.Creep);
        float overkillDamage = -creepComponent.Hp;
        float damage = overkillDamage * overkillPercent;
        attackerComponent.AttackStats.DamagePerBullet = damage;
        
        for (int i = 0; i < shrapnelAmount; i++)
        {
            PositionComponent projectilePosition = new()
            {
                Position = creepPositionComponent.Position,
                Direction = new float2(1,0).GetRotated(Random.Range(0.0f, 2 * math.PI))
            };
            
            TargetingSystemBase.CreateProjectile(handler.EntityCommandBuffer, projectilePosition,attackerComponent.TowerType, out Entity projectile);
            handler.EntityCommandBuffer.SetName(projectile,"Shrapnel");
            handler.EntityCommandBuffer.AddComponent(projectile, new ProjectileComponent(Entity.Null, attackerComponent, gunStatsComponent, 0.0f, false, false,projectilePosition.Position));
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/ShrapnelOnKill")
                                                .Replace("{param}", shrapnelAmount.ToString());
}