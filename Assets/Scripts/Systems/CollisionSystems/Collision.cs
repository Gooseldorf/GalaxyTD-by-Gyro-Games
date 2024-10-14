using ECSTest.Components;
using Unity.Entities;
using Unity.Mathematics;

public struct Collision:IComponentData
{
    public Entity Target { get; private set; }
    public ProjectileComponent Projectile { get; private set; }
    public float2 Point { get; private set; }
    public float2 Normal { get; private set; }
    public float2 ImpactDirection => float2.zero;// this.Projectile.Direction.xy;

    public Collision(Entity target, ProjectileComponent projectile, float2 point, float2 normal)
    {
        Target = target;
        Projectile = projectile;
        Point = point;
        Normal = normal;
    }

    public void NormalizeNormal()
    {
        Normal = math.normalize(Normal);
    }
}