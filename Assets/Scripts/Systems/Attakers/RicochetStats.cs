using System;
using UnityEngine;

[Serializable]
public class RicochetStats : ICloneable
{
    public int RicochetCount;
    public int PenetrationCount;
    public float RicochetDeviation;
    public float DamageMultPerPenetration;
    public float DamageMultPerRicochet;

    /// <summary>
    /// add absolute bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static RicochetStats operator +(RicochetStats a, RicochetStats b)
    {
        a.RicochetCount += b.RicochetCount;
        a.PenetrationCount += b.PenetrationCount;
        a.RicochetDeviation += b.RicochetDeviation;
        a.DamageMultPerPenetration += b.DamageMultPerPenetration;
        a.DamageMultPerRicochet += b.DamageMultPerRicochet;
        return a;
    }

    /// <summary>
    /// remove absolute bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static RicochetStats operator -(RicochetStats a, RicochetStats b)
    {
        a.RicochetCount -= b.RicochetCount;
        a.PenetrationCount -= b.PenetrationCount;
        a.RicochetDeviation -= b.RicochetDeviation;
        a.DamageMultPerPenetration -= b.DamageMultPerPenetration;
        a.DamageMultPerRicochet -= b.DamageMultPerRicochet;
        return a;
    }

    /// <summary>
    /// add percent bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static RicochetStats operator *(RicochetStats a, RicochetStats b)
    {
        a.RicochetCount += b.RicochetCount;
        a.PenetrationCount += b.PenetrationCount;
        a.RicochetDeviation += a.RicochetDeviation * b.RicochetDeviation;
        a.DamageMultPerPenetration += b.DamageMultPerPenetration;
        a.DamageMultPerRicochet += b.DamageMultPerRicochet;
        return a;
    }

    /// <summary>
    /// remove percent bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static RicochetStats operator /(RicochetStats a, RicochetStats b)
    {
        a.RicochetCount -= b.RicochetCount;
        a.PenetrationCount -= b.PenetrationCount;
        a.RicochetDeviation -= a.RicochetDeviation * b.RicochetDeviation;
        a.DamageMultPerPenetration -= b.DamageMultPerPenetration;
        a.DamageMultPerRicochet -= b.DamageMultPerRicochet;
        return a;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}