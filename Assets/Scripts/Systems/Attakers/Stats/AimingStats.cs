using System;

[Serializable]
public class AimingStats : ICloneable
{
    public float Range;
    public float RotationSpeed;
    public float AttackAngle;

    /// <summary>
    /// add absolute bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static AimingStats operator +(AimingStats a, AimingStats b)
    {
        a.Range += b.Range;
        a.RotationSpeed += b.RotationSpeed;
        a.AttackAngle += b.AttackAngle;
        return a;
    }

    /// <summary>
    /// remove absolute bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static AimingStats operator -(AimingStats a, AimingStats b)
    {
        a.Range -= b.Range;
        a.RotationSpeed -= b.RotationSpeed;
        a.AttackAngle -= b.AttackAngle;
        return a;
    }

    /// <summary>
    /// add percent bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static AimingStats operator *(AimingStats a, AimingStats b)
    {
        a.Range += a.Range * b.Range;
        a.RotationSpeed += a.RotationSpeed * b.RotationSpeed;
        a.AttackAngle += a.AttackAngle * b.AttackAngle;
        return a;
    }

    /// <summary>
    /// remove percent bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static AimingStats operator /(AimingStats a, AimingStats b)
    {
        a.Range -= a.Range * b.Range;
        a.RotationSpeed -= a.RotationSpeed * b.RotationSpeed;
        a.AttackAngle -= a.AttackAngle * b.AttackAngle;
        return a;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}