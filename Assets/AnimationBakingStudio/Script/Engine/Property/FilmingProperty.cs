using System;
using UnityEngine;

namespace ABS
{
    [Serializable]
    public class FilmingProperty : PropertyBase
    {
        public Resolution resolution = new Resolution(500, 400);
        public int numOfFrames = 10;
        public int simulatedIndex = 0; // used only in Editor Mode & for Mesh Model
        public double delay = 0;
    }
}
