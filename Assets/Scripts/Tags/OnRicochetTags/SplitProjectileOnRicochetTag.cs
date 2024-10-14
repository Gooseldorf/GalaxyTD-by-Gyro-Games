using CardTD.Utilities;
using ECSTest.Components;
using ECSTest.Systems;
using I2.Loc;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class SplitProjectileOnRicochetTag : OnRicochetTag
{
    [SerializeField] private int projectilesAmount = 1;
    
    [SerializeField] private AttackerComponent attackerComponent;
    [SerializeField] private GunStatsComponent gunStatsComponent;
    
    public override void OnRicochet(ProjectileComponent projectileComponent, PositionComponent positionComponent, RefRW<CashComponent> cashComponent, EntityManager manager, EntityCommandBuffer ecb)
    {
        for (int i = 0; i < projectilesAmount; i++)
        {
            float angle = Random.Range(-30f, 30f);
            float2 direction = -positionComponent.Direction;
            
            PositionComponent projectilePosition = new()
            {
                Position = positionComponent.Position,
                Direction = direction.GetRotated(angle * (math.PI / 180))
            };
            
            var bullet = new ProjectileComponent(Entity.Null, attackerComponent, gunStatsComponent, 0.0f, false, false, projectilePosition.Position);
            bullet.Velocity = projectileComponent.Velocity;
            bullet.TowerId = projectileComponent.TowerId;
            bullet.RicochetCount = 0;

            TargetingSystemBase.CreateProjectile(ecb, projectilePosition,projectileComponent.TowerId, out Entity projectile);
            ecb.SetName(projectile,"SplittedRicochetProjectile");
            ecb.AddComponent(projectile, bullet);
        }
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/SplitOnRicochet")
                                                .Replace("{param}", (projectilesAmount + 1).ToString());
}