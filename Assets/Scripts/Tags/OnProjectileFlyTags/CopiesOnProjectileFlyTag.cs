using CardTD.Utilities;
using ECSTest.Components;
using ECSTest.Systems;
using I2.Loc;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class CopiesOnProjectileFlyTag : OnProjectileFlyTag
{
    [SerializeField] private CopiesOnFlyType emitType = CopiesOnFlyType.Side;
    [SerializeField] private float emitFrequency = 5f;

    private readonly int projectileAmount = 2;

    private float2 projectileDirection;
    private float angle;

    public override void OnProjectileFly(Entity projectile, EntityManager manager, EntityCommandBuffer ecb)
    {
        if(!manager.Exists(projectile))
            return;
        bool isSecond = false;
        ProjectileComponent projectileComponent = manager.GetComponentData<ProjectileComponent>(projectile);
        ProjectileFlyComponent flyComponent = manager.GetComponentData<ProjectileFlyComponent>(projectile);

        if (projectileComponent.DistanceTraveled >= flyComponent.LastEmitDistance + emitFrequency)
        {
            PositionComponent positionComponent = manager.GetComponentData<PositionComponent>(projectile);
            flyComponent.LastEmitDistance = projectileComponent.DistanceTraveled;

            for (int i = 0; i < projectileAmount; i++)
            {
                SetProjectileDirection(positionComponent, isSecond);

                PositionComponent projectilePosition = new() { Position = positionComponent.Position, Direction = projectileDirection };
                TargetingSystemBase.CreateProjectile(ecb, projectilePosition,projectileComponent.TowerId, out Entity projectileCopy);

                projectileComponent.DistanceTraveled = 0;
                ecb.SetName(projectileCopy, "ExtraShotProjectile");
                ecb.AddComponent(projectileCopy, projectileComponent);

                isSecond = true; //don't like this bool, but idk how to make it better
            }

            manager.SetComponentData(projectile, flyComponent);
        }
    }

    private void SetProjectileDirection(PositionComponent positionComponent, bool isSecond)
    {
        switch (emitType)
        {
            case CopiesOnFlyType.Side:
                angle = isSecond ? -90 : 90;
                projectileDirection = positionComponent.Direction.GetRotated(angle * (Mathf.PI / 180));
                break;
            case CopiesOnFlyType.BackDiagonal:
                angle = isSecond ? -135 : 135;
                projectileDirection = positionComponent.Direction.GetRotated(angle * (Mathf.PI / 180));
                break;
            case CopiesOnFlyType.Random:
                angle = isSecond ? -Random.Range(5f, 175f) : Random.Range(5f, 175f);
                projectileDirection = positionComponent.Direction.GetRotated(angle * (Mathf.PI / 180));
                break;
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation($"Tags/Copies{emitType}");

    public enum CopiesOnFlyType { Side, BackDiagonal, Random }
}
