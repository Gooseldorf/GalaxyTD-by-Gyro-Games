using System;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public class Sampling
    {
        [NonSerialized]
        public readonly Texture2D[] rawMainTextures = null;
        [NonSerialized]
        public readonly Texture2D[] trimMainTextures = null;
        [NonSerialized]
        public Texture2D[] rawNormalTextures = null;
        [NonSerialized]
        public readonly float[] frameTimes = null;
        [NonSerialized]
        public readonly List<Frame> selectedFrames = null;

        public Sampling(int numOfFrames)
        {
            rawMainTextures = new Texture2D[numOfFrames];
            trimMainTextures = new Texture2D[numOfFrames];
            rawNormalTextures = null;
            frameTimes = new float[numOfFrames];
            selectedFrames = new List<Frame>();
        }
    }
}
