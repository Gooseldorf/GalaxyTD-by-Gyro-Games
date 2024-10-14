using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public abstract class MaterialBuilder : MonoBehaviour
    {
        public abstract void BindTextures(Material mat, Texture2D mainTex, Texture2D normalTex = null);
    }
}
