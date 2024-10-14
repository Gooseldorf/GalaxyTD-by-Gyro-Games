using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal class UnlitUVShader : BaseShaderGUI
    {
        //MaterialProperty mainTex_UV;
        MaterialProperty outlineColor;
        MaterialProperty outlineSize;
        MaterialProperty treshold;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            //mainTex_UV = FindProperty("_MainTex_UV", properties);
            outlineColor = FindProperty("_OutlineColor", properties);
            outlineSize = FindProperty("_OutlineSize", properties);
            treshold = FindProperty("_Treshold", properties);
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material);
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            DrawTileOffset(materialEditor, baseMapProp);
            EditorGUILayout.LabelField("Outline Settings:");
            materialEditor.ColorProperty(outlineColor, "Outline Color");
            materialEditor.IntegerProperty(outlineSize, "Outline Width");
            materialEditor.RangeProperty(treshold, "Alpha Treshold");
            material.SetFloat("_IsOutline", 1);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Blend", (float)blendMode);

            material.SetFloat("_Surface", (float)surfaceType);
            if (surfaceType == SurfaceType.Opaque)
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }
    }
    //public override void LoadMaterialProperties()
    //{
    //    colorProperty = FindProperty("_MyColor");
    //}

    //public override void OnGUI()
    //{
    //    materialEditor.ShaderProperty(colorProperty, "My Color");
    //}
}
