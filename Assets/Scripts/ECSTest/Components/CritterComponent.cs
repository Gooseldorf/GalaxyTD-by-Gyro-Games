using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECSTest.Components
{
    public struct CritterComponent: IComponentData
    {
        public float2 TargetDirection;
        public float Radius;
        public float MovingSpeed;
        public float RotationSpeed;
        public float CleaningQuality;
        public AllEnums.CritterType CritterType;
        public int SearchTargetPositionMaxRadius;
        
        public bool IsMoving;
        public bool IsRotating;
    }

    //public struct GaussCleanDecalComponent : IComponentData
    //{
    //    public float Radius;
    //}
}