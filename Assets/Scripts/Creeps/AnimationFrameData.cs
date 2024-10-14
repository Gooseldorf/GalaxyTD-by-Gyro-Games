using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ECSTest.Components
{
    [System.Serializable]
    public struct AnimationFrameData
    {
        public float4 UV;
        public float2 Scale;
        public float2 PositionOffset;
    }
    [System.Serializable]
    public struct AnimationFrameId : IEquatable<AnimationFrameId>
    {
        public byte FrameNumber;
        public AllEnums.AnimationState State;

        public bool Equals(AnimationFrameId other)
        {
            return FrameNumber == other.FrameNumber && State == other.State;
        }

        public override int GetHashCode()
        {
            return (int)(FrameNumber*100) + (int)State;
        }
    }
}
