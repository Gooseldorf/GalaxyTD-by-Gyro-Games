using CardTD.Utilities;
using ECSTest.Components;
using ECSTest.Systems;
using I2.Loc;
using Unity.Entities;
using UnityEngine;

public sealed class SplitProjectileOnPenetrationTag : OnHitTag
{
    [SerializeField] private int projectilesAmount = 1;

    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        if (onHitTagData.GunCollisionEvent.ProjectileComponent.PenetrationCount >= 1)
        {
            ProjectileComponent projectileComponent = onHitTagData.GunCollisionEvent.ProjectileComponent;

            projectileComponent.PenetrationCount--;
            projectileComponent.DistanceTraveled = 0;
            
            for (int i = 0; i < projectilesAmount; i++)
            {
                PositionComponent projectilePosition = new()
                {
                    Position = onHitTagData.GunCollisionEvent.Point,
                    Direction = onHitTagData.GunCollisionEvent.CollisionDirection.GetRotated((i%2==0 ? -1:1)*(2.09f)+onHitTagData.TagIndex*.1f-(i * 0.05f))//2.09 - p *2/3 random(0,2*pi)
                };
                projectileComponent.StartPosition = onHitTagData.GunCollisionEvent.Point;

                TargetingSystemBase.CreateProjectile(ecb, projectilePosition,projectileComponent.TowerId, out Entity projectile);
                ecb.SetName(projectile, $"SplittedPenetrationProjectile");
                ecb.AddComponent(projectile, projectileComponent);
            }
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/SplitOnPenetration")
                                                .Replace("{param}", (projectilesAmount + 1).ToString());
}