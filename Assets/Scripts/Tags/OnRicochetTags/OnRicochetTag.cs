using ECSTest.Components;
using Unity.Entities;

public abstract class OnRicochetTag : Tag
{
    public abstract void OnRicochet(ProjectileComponent projectileComponent, PositionComponent positionComponent, RefRW<CashComponent> cashComponent, EntityManager manager, EntityCommandBuffer ecb); 
}
