using System;
using Unity.Mathematics;

public class Projectile : IPosition, IDestroyable
{
    public float3 Position { get; set; }
    public float3 Direction { get; set; }
    public IAttacker Attacker { get; set; }
    public float Velocity => Attacker.AttackStats.ProjectileSpeed;
    public float Damage { get; set; }
    public float DistanceTraveled { get; set; }
    public Action OnDestroy { get; }
    public bool IsNeedToDestroy { get; set; }
    public float LastUpdate { get; set; }

    //private float startOffset = 1.1f;
    public Projectile(IAttacker attacker, float lastUpdate)
    {
        Direction = attacker.Direction;
        //Position = attacker.Position + (attacker.AttackStats.BarrelLengthOffset * Direction);//TODO relocate
        
        
        //if (attacker.AttackStats is GunStats gunStats)
        //{
        //    //attackPoint width
        //    float barrelWidth = Random.Range(-gunStats.AccuracyStats.BarrelWidthOffset, gunStats.AccuracyStats.BarrelWidthOffset);
        //    Position += (barrelWidth * math.cross(Direction, UnityEngine.Vector3.forward));

        //    //Direction Deviation
        //    float deviation = Random.Range(-gunStats.AccuracyStats.Deviation, gunStats.AccuracyStats.Deviation);
        //    Direction = Quaternion.Euler(Direction) * Quaternion.Euler(0, 0, deviation) * Direction;
        //}

        Attacker = attacker;
        Damage = attacker.AttackStats.DamagePerBullet;
        DistanceTraveled = 0;
        LastUpdate = lastUpdate;
        
    }

    public Projectile(Projectile projectile)
    {
        Position = projectile.Position;
        Direction = projectile.Direction;
        Attacker = projectile.Attacker;
        Damage = projectile.Damage;
        DistanceTraveled = projectile.DistanceTraveled;
        IsNeedToDestroy = projectile.IsNeedToDestroy;
    }
}