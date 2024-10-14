using CardTD.Utilities;
using ECSTest.Components;
using ECSTest.Systems;
using I2.Loc;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public sealed class ExtraShotOnShootTag : OnShootTag
{
    [SerializeField] private ExtraShotType extraShotType = ExtraShotType.Back;
    [SerializeField] private int projectilesAmount = 1;
    [SerializeField] private int deviation = 10;
    
    private float2 projectileDirection;
    private float angle;
    
    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        PositionComponent positionComponent = manager.GetComponentData<PositionComponent>(tower);

        foreach (Entity projectileEntity in dynamicBuffer)
        {
            ProjectileComponent projectileComponent = manager.GetComponentData<ProjectileComponent>(projectileEntity);
            if (!projectileComponent.IsLastBullet) continue;
            
            PositionComponent projectilePositionComponent = manager.GetComponentData<PositionComponent>(projectileEntity);
            SetProjectileDirection(projectilePositionComponent);

            EntityQuery skipperQuery = manager.CreateEntityQuery(new ComponentType[] { typeof(RandomComponent) });
            skipperQuery.TryGetSingleton(out RandomComponent randomComponent);
            Random random = randomComponent.GetRandom(JobsUtility.ThreadIndex);

            for (int i = 0; i < projectilesAmount; i++)
            {
                PositionComponent projectilePosition = TargetingSystemBase.GetStartPosition(ref random, positionComponent.Position, projectileDirection, deviation);

                Utilities.GetGaussian(ref random, 0, 1 / 30f, out float flyTime, out _);
                projectileComponent.StartDistance += flyTime * projectileComponent.Velocity;

                TargetingSystemBase.CreateProjectile(ecb, projectilePosition,projectileComponent.TowerId, out Entity projectile);
                ecb.SetName(projectile, "ExtraShotProjectile");
                ecb.AddComponent(projectile, projectileComponent);

                projectileDirection = projectilePositionComponent.Direction.GetRotated(-angle * (Mathf.PI / 180));
            }

            randomComponent.SetRandom(random, JobsUtility.ThreadIndex);
        }
    }

    private void SetProjectileDirection(PositionComponent projectilePositionComponent)
    {
        switch (extraShotType)
        {
            case ExtraShotType.Back:
                projectileDirection = -projectilePositionComponent.Direction;
                break;
            case ExtraShotType.Side:
                angle = 90;
                projectileDirection =  projectilePositionComponent.Direction.GetRotated(angle * (Mathf.PI / 180));
                break;
            case ExtraShotType.Diagonal:
                angle = 45; 
                projectileDirection =  projectilePositionComponent.Direction.GetRotated(angle * (Mathf.PI / 180));
                break;
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation($"Tags/ExtraShot{extraShotType}")
                                                .Replace("{param}", projectilesAmount.ToString());
    public enum ExtraShotType { Back, Side, Diagonal }
}
