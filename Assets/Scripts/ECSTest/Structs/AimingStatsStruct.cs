using System;

namespace ECSTest.Structs
{
    [Serializable]
    public struct AimingStatsStruct
    {
        public float Range;
        public float RotationSpeed;
        public float AttackAngle;
        
        #region Operator overloads

         private const float tolerance = 0.0001f;
        
        public static bool operator ==(AimingStatsStruct a, AimingStatsStruct b)
        {
            return Math.Abs(a.Range - b.Range) < tolerance 
                   && Math.Abs(a.RotationSpeed - b.RotationSpeed) < tolerance 
                   && Math.Abs(a.AttackAngle - b.AttackAngle) < tolerance;
        }

        public static bool operator !=(AimingStatsStruct a, AimingStatsStruct b)
        {
            return !(a == b);
        }
    
        public static AimingStatsStruct operator +(AimingStatsStruct a, AimingStatsStruct b)
        {
            return new AimingStatsStruct
            {
                Range = a.Range + b.Range,
                RotationSpeed = a.RotationSpeed + b.RotationSpeed,
                AttackAngle = a.AttackAngle + b.AttackAngle
            };
        }

        public static AimingStatsStruct operator -(AimingStatsStruct a, AimingStatsStruct b)
        {
            return new AimingStatsStruct
            {
                Range = a.Range - b.Range,
                RotationSpeed = a.RotationSpeed - b.RotationSpeed,
                AttackAngle = a.AttackAngle - b.AttackAngle
            };
        }

        public static AimingStatsStruct operator *(AimingStatsStruct a, AimingStatsStruct b)
        {
            return new AimingStatsStruct
            {
                Range = a.Range + a.Range * b.Range,
                RotationSpeed = a.RotationSpeed + a.RotationSpeed * b.Range,
                AttackAngle = a.AttackAngle + a.AttackAngle * b.AttackAngle
            };
        }

        public static AimingStatsStruct operator /(AimingStatsStruct a, AimingStatsStruct b)
        {
            return new AimingStatsStruct
            {
                Range = a.Range - a.Range * b.Range,
                RotationSpeed = a.RotationSpeed - a.RotationSpeed * b.RotationSpeed,
                AttackAngle = a.AttackAngle - a.AttackAngle * b.AttackAngle
            };
        }

        public override bool Equals(object obj)
        {
            return obj is AimingStatsStruct other && this == other;
        }

        public override int GetHashCode()
        {
            return (Range, RotationSpeed, AttackAngle).GetHashCode();
        }

        #endregion
    }
}