using Sirenix.OdinInspector;
using System;
using UnityEngine;
using static AllEnums;

[Serializable]
public class ShootingStats : ICloneable
{
    [ShowInInspector, ReadOnly]
    public float BurstDelay => 3 * ShotDelay;
    public int ShotsPerBurst;
    public float ShotDelay;
    public AttackPattern AvailableAttackPatterns = AttackPattern.All;
    public int ProjectilesPerShot;
    public float WindUpTime;

    public AttackPattern GetNextAvailableAttackPattern(AttackPattern startingPattern)
    {
        if (AvailableAttackPatterns == 0)
        {
            Debug.LogError("No available Patterns for this tower");
            AvailableAttackPatterns = AttackPattern.All;
        }
        int i = (int)startingPattern;
        do
        {
            i = i >= (1 << 3) ? 1 : i << 1;
            startingPattern = (AttackPattern)i;
        } while (!AvailableAttackPatterns.HasFlag(startingPattern));
        return startingPattern;
    }

    /// <summary>
    /// add absolute bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static ShootingStats operator +(ShootingStats a, ShootingStats b)
    {
        //a.BurstDelay += b.BurstDelay;
        a.ShotsPerBurst += b.ShotsPerBurst;
        a.ShotDelay += b.ShotDelay;
        a.ProjectilesPerShot += b.ProjectilesPerShot;
        a.WindUpTime += b.WindUpTime;
        return a;
    }

    /// <summary>
    /// remove absolute bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static ShootingStats operator -(ShootingStats a, ShootingStats b)
    {
        //a.BurstDelay -= b.BurstDelay;
        a.ShotsPerBurst -= b.ShotsPerBurst;
        a.ShotDelay -= b.ShotDelay;
        a.ProjectilesPerShot -= b.ProjectilesPerShot;
        a.WindUpTime -= b.WindUpTime;
        return a;
    }

    /// <summary>
    /// add percent bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static ShootingStats operator *(ShootingStats a, ShootingStats b)
    {
        //a.BurstDelay += a.BurstDelay * b.BurstDelay;
        a.ShotsPerBurst += b.ShotsPerBurst;
        a.ShotDelay += a.ShotDelay * b.ShotDelay;
        a.ProjectilesPerShot += b.ProjectilesPerShot;
        a.WindUpTime += a.WindUpTime * b.WindUpTime;
        return a;
    }

    /// <summary>
    /// remove percent bonus
    /// </summary>
    /// <param name="a">Attack Stats</param>
    /// <param name="b">Bonus</param>
    public static ShootingStats operator /(ShootingStats a, ShootingStats b)
    {
        //a.BurstDelay -= a.BurstDelay * b.BurstDelay;
        a.ShotsPerBurst -= b.ShotsPerBurst;
        a.ShotDelay -= a.ShotDelay * b.ShotDelay;
        a.ProjectilesPerShot -= b.ProjectilesPerShot;
        a.WindUpTime -= a.WindUpTime * b.WindUpTime;
        return a;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}