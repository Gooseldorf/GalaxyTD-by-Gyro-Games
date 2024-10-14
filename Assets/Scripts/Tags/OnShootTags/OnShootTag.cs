using ECSTest.Components;
using Unity.Entities;


public abstract class OnShootTag : Tag
{
    public abstract void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer);
}
