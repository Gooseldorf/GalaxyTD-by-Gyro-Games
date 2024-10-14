using CardTD.Utilities;
using ECSTest.Components;
using ECSTest.Systems;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public sealed class MultishotOnShootTag : OnShootTag
{
    [SerializeField, InfoBox("Percents"), Range(0, 100)] private float multishotChance = 25f;
    [SerializeField] private int projectilesAmount = 1;
    [SerializeField] private float angle = 45;

    private const float barrelWidth = .4f;
    private const float offsetRotationAngle = 40 * math.TORADIANS;

    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        float rand = Random.Range(0f, 100f);

        if (rand >= multishotChance) return;

        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);

        PositionComponent towerPositionComponent = manager.GetComponentData<PositionComponent>(tower);

        foreach (Entity projectileEntity in dynamicBuffer)
        {
            PositionComponent projectilePositionComponent = manager.GetComponentData<PositionComponent>(projectileEntity);

            if (manager.HasComponent<ProjectileComponent>(projectileEntity))
            {
                ProjectileComponent projectileComponent = manager.GetComponentData<ProjectileComponent>(projectileEntity);

                for (int i = 0; i < projectilesAmount; i++)
                {
                    PrepareGunProjectileComponents(towerPositionComponent, projectilePositionComponent, out PositionComponent projectilePosition);

                    TargetingSystemBase.CreateProjectile(ecb, projectilePosition,projectileComponent.TowerId, out Entity projectile);
                    ecb.SetName(projectile, "MultishotProjectile");
                    ecb.AddComponent(projectile, projectileComponent);
                }
            }
            else if (manager.HasComponent<RocketProjectile>(projectileEntity))
            {
                RocketProjectile rocketProjectileComponent = manager.GetComponentData<RocketProjectile>(projectileEntity);

                int dirRandomizeMultiplier = 0;
                float2 origin = towerPositionComponent.Position + towerPositionComponent.Direction * attackerComponent.StartOffset;

                for (int i = 0; i < projectilesAmount; i++)
                {
                    if (dirRandomizeMultiplier > 2) dirRandomizeMultiplier = 0;

                    PrepareRocketProjectileComponents(towerPositionComponent, dirRandomizeMultiplier, origin, ref rocketProjectileComponent, out PositionComponent positionComponent);

                    TargetingSystemBase.CreateProjectile(ecb, positionComponent,AllEnums.TowerId.Rocket, out Entity projectile);
                    ecb.SetName(projectile, "MultishotProjectile");
                    ecb.AddComponent(projectile, rocketProjectileComponent);
                    dirRandomizeMultiplier++;
                }
            }
            else if (manager.HasComponent<MortarProjectile>(projectileEntity))
            {
                MortarProjectile mortarProjectileComponent = manager.GetComponentData<MortarProjectile>(projectileEntity);

                for (int i = 0; i < projectilesAmount; i++)
                {
                    PositionComponent positionComponent = new(mortarProjectileComponent.Target, float2.zero);

                    TargetingSystemBase.CreateProjectile(ecb, positionComponent,AllEnums.TowerId.Mortar, out Entity projectile);
                    ecb.SetName(projectile, "MultishotProjectile");
                    ecb.AddComponent(projectile, mortarProjectileComponent);
                }
            }
        }
    }

    private void PrepareGunProjectileComponents(PositionComponent towerPositionComponent, PositionComponent projectilePositionComponent, out PositionComponent projectilePosition)
    {
        float angleInDegrees = Random.Range(-angle, angle);
        float angleInRadians = angleInDegrees * (Mathf.PI / 180);

        projectilePosition = new()
        {
            Position = towerPositionComponent.Position,
            Direction = projectilePositionComponent.Direction.GetRotated(angleInRadians)
        };
    }

    private void PrepareRocketProjectileComponents(PositionComponent towerPositionComponent, int dirRandomizeMultiplier, float2 origin, ref RocketProjectile rocketProjectileComponent, out PositionComponent positionComponent)
    {
        float2 direction = math.normalize(rocketProjectileComponent.Target - towerPositionComponent.Position);
        origin += Utilities.GetNormal(direction) * dirRandomizeMultiplier * barrelWidth;
        direction = Utilities.GetNormal(direction.GetRotated(offsetRotationAngle * dirRandomizeMultiplier));

        float2 offsetPoint = origin + direction * dirRandomizeMultiplier;

        float2 pos = rocketProjectileComponent.GetPosition();

        positionComponent = new()
        {
            Position = pos,
            Direction = math.normalize(origin - pos)
        };

        rocketProjectileComponent.OffsetPoint = offsetPoint;
    }


    public override string GetDescription()
    {
        string count = projectilesAmount switch
        {
            1 => LocalizationManager.GetTranslation("Tags/Double"),
            2 => LocalizationManager.GetTranslation("Tags/Triple"),
            3 => LocalizationManager.GetTranslation("Tags/Quad"),
            _ => "Multi"
        };

        return LocalizationManager.GetTranslation("Tags/Multishot")
                                                .Replace("{param1}", multishotChance + "<color=#1fb2de>%</color>")
                                                .Replace("{param2}", "<color=#1fb2de>" + count + "</color>");
    }
}