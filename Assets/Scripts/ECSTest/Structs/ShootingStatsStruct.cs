using System;
using UnityEngine;
using static AllEnums;

namespace ECSTest.Structs
{
    [Serializable]
    public struct ShootingStatsStruct
    {
        public int ShotsPerBurst;
        public int ProjectilesPerShot;
        public float BurstDelay => 3 * ShotDelay;
        public float ShotDelay;
        public float WindUpTime;
        public AttackPattern AvailableAttackPatterns;

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
                i = i > (1 << 10) ? 1 : i << 1;
                startingPattern = (AttackPattern)i;
            } while (!AvailableAttackPatterns.HasFlag(startingPattern) || !Enum.IsDefined(typeof(AttackPattern), startingPattern));

            return startingPattern;
        }

        #region Operator overloads

        private const float tolerance = 0.0001f;

        public static bool operator ==(ShootingStatsStruct a, ShootingStatsStruct b)
        {
            return Math.Abs(a.BurstDelay - b.BurstDelay) < tolerance
                   && a.ShotsPerBurst == b.ShotsPerBurst
                   && Math.Abs(a.ShotDelay - b.ShotDelay) < tolerance
                   && a.ProjectilesPerShot == b.ProjectilesPerShot
                   && Math.Abs(a.WindUpTime - b.WindUpTime) < tolerance
                   && a.AvailableAttackPatterns == b.AvailableAttackPatterns;
        }

        public static bool operator !=(ShootingStatsStruct a, ShootingStatsStruct b)
        {
            return !(a == b);
        }

        public static ShootingStatsStruct operator +(ShootingStatsStruct a, ShootingStatsStruct b)
        {
            return new ShootingStatsStruct
            {
                //BurstDelay = a.BurstDelay + b.BurstDelay,
                ShotsPerBurst = a.ShotsPerBurst + b.ShotsPerBurst,
                ShotDelay = a.ShotDelay + b.ShotDelay,
                ProjectilesPerShot = a.ProjectilesPerShot + b.ProjectilesPerShot,
                WindUpTime = a.WindUpTime + b.WindUpTime,
                AvailableAttackPatterns = a.AvailableAttackPatterns,
            };
        }

        public static ShootingStatsStruct operator -(ShootingStatsStruct a, ShootingStatsStruct b)
        {
            return new ShootingStatsStruct
            {
                //BurstDelay = a.BurstDelay - b.BurstDelay,
                ShotsPerBurst = a.ShotsPerBurst - b.ShotsPerBurst,
                ShotDelay = a.ShotDelay - b.ShotDelay,
                ProjectilesPerShot = a.ProjectilesPerShot - b.ProjectilesPerShot,
                WindUpTime = a.WindUpTime - b.WindUpTime,
                AvailableAttackPatterns = a.AvailableAttackPatterns,
            };
        }

        public static ShootingStatsStruct operator *(ShootingStatsStruct a, ShootingStatsStruct b)
        {
            return new ShootingStatsStruct
            {
                //BurstDelay = a.BurstDelay + a.BurstDelay * b.BurstDelay,
                ShotsPerBurst = a.ShotsPerBurst + b.ShotsPerBurst,
                ShotDelay = a.ShotDelay + a.ShotDelay * b.ShotDelay,
                ProjectilesPerShot = a.ProjectilesPerShot + b.ProjectilesPerShot,
                WindUpTime = a.WindUpTime + a.WindUpTime * b.WindUpTime,
                AvailableAttackPatterns = a.AvailableAttackPatterns,
            };
        }

        public static ShootingStatsStruct operator /(ShootingStatsStruct a, ShootingStatsStruct b)
        {
            return new ShootingStatsStruct
            {
                //BurstDelay = a.BurstDelay - a.BurstDelay * b.BurstDelay,
                ShotsPerBurst = a.ShotsPerBurst - b.ShotsPerBurst,
                ShotDelay = a.ShotDelay - a.ShotDelay * b.ShotDelay,
                ProjectilesPerShot = a.ProjectilesPerShot - b.ProjectilesPerShot,
                WindUpTime = a.WindUpTime - a.WindUpTime * b.WindUpTime,
                AvailableAttackPatterns = a.AvailableAttackPatterns,
            };
        }

        public override bool Equals(object obj)
        {
            return obj is ShootingStatsStruct other && this == other;
        }

        public override int GetHashCode()
        {
            return (BurstDelay, ShotsPerBurst, ShotDelay, ProjectilesPerShot, WindUpTime, AvailableAttackPatterns).GetHashCode();
        }

        #endregion
    }
}