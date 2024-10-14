using System;

namespace ECSTest.Structs
{
    [Serializable]
    public struct AttackStatsStruct
    {
        public ReloadStatsStruct ReloadStats;
        public AimingStatsStruct AimingStats;
        public ShootingStatsStruct ShootingStats;
        
        public float ProjectileSpeed;
        public float DamagePerBullet;
        public float KnockBackPerBullet;
        

        #region Operator overloads

        private const float tolerance = 0.0001f;
        
        public static bool operator ==(AttackStatsStruct a, AttackStatsStruct b)
        {
            return Math.Abs(a.ProjectileSpeed - b.ProjectileSpeed) < tolerance
                   && Math.Abs(a.DamagePerBullet - b.DamagePerBullet) < tolerance
                   && Math.Abs(a.KnockBackPerBullet - b.KnockBackPerBullet) < tolerance
                   && a.ReloadStats.Equals(b.ReloadStats)
                   && a.AimingStats.Equals(b.AimingStats)
                   && a.ShootingStats.Equals(b.ShootingStats);
        }

        public static bool operator !=(AttackStatsStruct a, AttackStatsStruct b)
        {
            return !(a == b);
        }
        
        public static AttackStatsStruct operator +(AttackStatsStruct a, AttackStatsStruct b)
        {
            return new AttackStatsStruct
            {
                ProjectileSpeed = a.ProjectileSpeed + b.ProjectileSpeed,
                DamagePerBullet = a.DamagePerBullet + b.DamagePerBullet,
                KnockBackPerBullet = a.KnockBackPerBullet + b.KnockBackPerBullet,
                ReloadStats = a.ReloadStats + b.ReloadStats,
                AimingStats = a.AimingStats + b.AimingStats,
                ShootingStats = a.ShootingStats + b.ShootingStats
            };
        }

        public static AttackStatsStruct operator -(AttackStatsStruct a, AttackStatsStruct b)
        {
            return new AttackStatsStruct
            {
                ProjectileSpeed = a.ProjectileSpeed - b.ProjectileSpeed,
                DamagePerBullet = a.DamagePerBullet - b.DamagePerBullet,
                KnockBackPerBullet = a.KnockBackPerBullet - b.KnockBackPerBullet,
                ReloadStats = a.ReloadStats - b.ReloadStats,
                AimingStats = a.AimingStats - b.AimingStats,
                ShootingStats = a.ShootingStats - b.ShootingStats
            };
        }

        public static AttackStatsStruct operator *(AttackStatsStruct a, AttackStatsStruct b)
        {
            return new AttackStatsStruct
            {
                ProjectileSpeed = a.ProjectileSpeed + a.ProjectileSpeed * b.ProjectileSpeed,
                DamagePerBullet = a.DamagePerBullet + a.DamagePerBullet * b.DamagePerBullet,
                KnockBackPerBullet = a.KnockBackPerBullet + a.KnockBackPerBullet * b.KnockBackPerBullet,
                ReloadStats = a.ReloadStats * b.ReloadStats,
                AimingStats = a.AimingStats * b.AimingStats,
                ShootingStats = a.ShootingStats * b.ShootingStats
            };
        }
        
        public static AttackStatsStruct operator /(AttackStatsStruct a, AttackStatsStruct b)
        {
            return new AttackStatsStruct
            {
                ProjectileSpeed = a.ProjectileSpeed - a.ProjectileSpeed * b.ProjectileSpeed,
                DamagePerBullet = a.DamagePerBullet - a.DamagePerBullet * b.DamagePerBullet,
                KnockBackPerBullet = a.KnockBackPerBullet - a.KnockBackPerBullet * b.KnockBackPerBullet,
                ReloadStats = a.ReloadStats / b.ReloadStats,
                AimingStats = a.AimingStats / b.AimingStats,
                ShootingStats = a.ShootingStats / b.ShootingStats
            };
        }
        
        public override bool Equals(object obj)
        {
            return obj is AttackStatsStruct other && this == other;
        }
    
        public override int GetHashCode()
        {
            return (ProjectileSpeed,DamagePerBullet, KnockBackPerBullet, ReloadStats, AimingStats, ShootingStats).GetHashCode();
 
        }
        #endregion
    }
}