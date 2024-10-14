using System;
using Unity.Mathematics;

namespace ECSTest.Structs
{
    [Serializable]
    public struct ReloadStatsStruct
    {
        public float RawMagazineSize;
        public float BulletCost;
        public float ReloadTime;
        public int MagazineSize => (int)RawMagazineSize;
        public int ReloadCost => BulletCost == 0 ? 0 : (int)math.max(1, math.round(BulletCost * MagazineSize));

        public int ManualReloadCost(int bulletsInMagazine) => (int)((MagazineSize - bulletsInMagazine) * BulletCost);

        #region Operator overloads

        private const float tolerance = 0.0001f;

        public static bool operator ==(ReloadStatsStruct a, ReloadStatsStruct b)
        {
            return a.MagazineSize == b.MagazineSize
                   && Math.Abs(a.BulletCost - b.BulletCost) < tolerance
                   && Math.Abs(a.ReloadTime - b.ReloadTime) < tolerance;
        }

        public static bool operator !=(ReloadStatsStruct a, ReloadStatsStruct b)
        {
            return !(a == b);
        }

        public static ReloadStatsStruct operator +(ReloadStatsStruct a, ReloadStatsStruct b)
        {
            return new ReloadStatsStruct {RawMagazineSize = a.RawMagazineSize + b.RawMagazineSize, BulletCost = a.BulletCost + b.BulletCost, ReloadTime = a.ReloadTime + b.ReloadTime};
        }

        public static ReloadStatsStruct operator -(ReloadStatsStruct a, ReloadStatsStruct b)
        {
            return new ReloadStatsStruct {RawMagazineSize = a.RawMagazineSize - b.RawMagazineSize, BulletCost = a.BulletCost - b.BulletCost, ReloadTime = a.ReloadTime - b.ReloadTime};
        }

        public static ReloadStatsStruct operator *(ReloadStatsStruct a, ReloadStatsStruct b)
        {
            return new ReloadStatsStruct
            {
                RawMagazineSize = a.RawMagazineSize * b.RawMagazineSize, BulletCost = a.BulletCost + a.BulletCost * b.BulletCost, ReloadTime = a.ReloadTime + a.ReloadTime * b.ReloadTime
            };
        }

        public static ReloadStatsStruct operator /(ReloadStatsStruct a, ReloadStatsStruct b)
        {
            return new ReloadStatsStruct
            {
                RawMagazineSize = a.RawMagazineSize / b.RawMagazineSize, BulletCost = a.BulletCost - a.BulletCost * b.BulletCost, ReloadTime = a.ReloadTime - a.ReloadTime * b.ReloadTime
            };
        }

        public override bool Equals(object obj)
        {
            return obj is ReloadStatsStruct other && this == other;
        }

        public override int GetHashCode()
        {
            return (MagazineSize, BulletCost, ReloadTime).GetHashCode();
        }

        #endregion
    }
}