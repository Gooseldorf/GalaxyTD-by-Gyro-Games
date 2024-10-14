using Unity.Entities;

public abstract class OnProjectileFlyTag : Tag
{
    public abstract void OnProjectileFly(Entity projectile, EntityManager manager, EntityCommandBuffer ecb); 
}