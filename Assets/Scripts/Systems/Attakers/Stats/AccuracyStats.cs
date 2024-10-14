using System;
using UnityEngine;

public class AccuracyStats : ICloneable
{
    [Tooltip("We use degrees")]
    public float Deviation;
    public float Control;
    public float Recoil;

    /// <summary>
    /// add absolute bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static AccuracyStats operator +(AccuracyStats a, AccuracyStats b)
    {
        a.Deviation += b.Deviation;
        a.Control += b.Control;
        a.Recoil += b.Recoil;
        return a;
    }

    /// <summary>
    /// remove absolute bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static AccuracyStats operator -(AccuracyStats a, AccuracyStats b)
    {
        a.Deviation -= b.Deviation;
        a.Control -= b.Control;
        a.Recoil -= b.Recoil;
        return a;
    }

    /// <summary>
    /// add percent bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static AccuracyStats operator *(AccuracyStats a, AccuracyStats b)
    {
        a.Deviation += a.Deviation * b.Deviation;
        a.Control += a.Control * b.Control;
        a.Recoil += a.Recoil * b.Recoil;
        return a;
    }

    /// <summary>
    /// remove percent bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static AccuracyStats operator /(AccuracyStats a, AccuracyStats b)
    {
        a.Deviation -= a.Deviation * b.Deviation;
        a.Control -= a.Control * b.Control;
        a.Recoil -= a.Recoil * b.Recoil;
        return a;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}