using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class ReloadStats : ICloneable
{
    public float RawMagazineSize;
    public float BulletCost;
    public float ReloadTime;

    public int ReloadCost => Mathf.CeilToInt(BulletCost * MagazineSize);
    public int MagazineSize => (int)math.ceil(RawMagazineSize);

    /// <summary>
    /// add absolute bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static ReloadStats operator +(ReloadStats a, ReloadStats b)
    {
        a.RawMagazineSize += b.RawMagazineSize;
        a.BulletCost += b.BulletCost;
        a.ReloadTime += b.ReloadTime;
        return a;
    }

    /// <summary>
    /// remove absolute bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static ReloadStats operator -(ReloadStats a, ReloadStats b)
    {
        a.RawMagazineSize -= b.RawMagazineSize;
        a.BulletCost -= b.BulletCost;
        a.ReloadTime -= b.ReloadTime;
        return a;
    }

    /// <summary>
    /// add percent bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static ReloadStats operator *(ReloadStats a, ReloadStats b)
    {
        a.RawMagazineSize += a.RawMagazineSize * b.RawMagazineSize;
        a.BulletCost += a.BulletCost * b.BulletCost;
        a.ReloadTime += a.ReloadTime * b.ReloadTime;
        return a;
    }

    /// <summary>
    /// remove percent bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static ReloadStats operator /(ReloadStats a, ReloadStats b)
    {
        a.RawMagazineSize -= a.RawMagazineSize * b.RawMagazineSize;
        a.BulletCost -= a.BulletCost * b.BulletCost;
        a.ReloadTime -= a.ReloadTime * b.ReloadTime;
        return a;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}