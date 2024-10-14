using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ABS.Demo
{
    public class BumpedMaterialBuilder : MaterialBuilder
    {
        public override void BindTextures(Material mat, Texture2D mainTex, Texture2D normalTex = null)
        {
            mat.SetTexture("_MainTex", mainTex);
            mat.SetTexture("_BumpMap", normalTex);
        }
    }
}
