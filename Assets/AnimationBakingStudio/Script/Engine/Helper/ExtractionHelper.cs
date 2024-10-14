using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public static class ExtractionHelper
    {
        #region Opaque

        private static ComputeShader opaqueExtractionShader = null;
        private static ComputeShader OpaqueExtractionShader
        {
            get
            {
                if (opaqueExtractionShader == null)
                    opaqueExtractionShader = AssetHelper.FindAsset<ComputeShader>("Shader/Compute", "OpaqueExtract");
                return opaqueExtractionShader;
            }
        }

        public static void ReadAndKeepRawOpaquePixels(List<Color32[]> pixelsOnBlackList, List<Color32[]> pixelsOnWhiteList)
        {
            Color32[] pixelsOnBlack = ReadCameraPixels32(Color.black);
            pixelsOnBlackList.Add(pixelsOnBlack);

            Color32[] pixelsOnWhite = ReadCameraPixels32(Color.white);
            pixelsOnWhiteList.Add(pixelsOnWhite);
        }

        public static void ExtractOpaqueValidPixels(Color32[] pixelsOnBlack, Color32[] pixelsOnWhite, ref Texture2D resultTexture, Color32 defaultColor, bool isNormalMap = false)
        {
            int textureSize = resultTexture.width * resultTexture.height;
            Color32[] resultPixels = Enumerable.Repeat(defaultColor, textureSize).ToArray();

            ComputeShader computeShader = OpaqueExtractionShader;
            if (EngineConstants.gpuUse && SystemInfo.supportsComputeShaders && !isNormalMap &&
                computeShader != null && textureSize > EngineConstants.GPU_THREAD_SIZE)
            {
                int kernelIndex = computeShader.FindKernel("OpaqueExtractFunction");

                ComputeBuffer blackBuffer = new ComputeBuffer(textureSize, 4);
                blackBuffer.SetData(pixelsOnBlack);
                computeShader.SetBuffer(kernelIndex, "blackBuffer", blackBuffer);

                ComputeBuffer whiteBuffer = new ComputeBuffer(textureSize, 4);
                whiteBuffer.SetData(pixelsOnWhite);
                computeShader.SetBuffer(kernelIndex, "whiteBuffer", whiteBuffer);

                computeShader.SetInt("width", resultTexture.width);

                ComputeBuffer resultBuffer = new ComputeBuffer(textureSize, 4);
                computeShader.SetBuffer(kernelIndex, "resultBuffer", resultBuffer);

                computeShader.Dispatch(kernelIndex, EngineConstants.GPU_THREAD_SIZE, 1, 1);
                resultBuffer.GetData(resultPixels);

                blackBuffer.Release();
                whiteBuffer.Release();
                resultBuffer.Release();
            }
            else
            {
                for (int i = 0; i < textureSize; ++i)
                {
                    if (pixelsOnWhite[i].r == pixelsOnBlack[i].r)
                        resultPixels[i] = pixelsOnBlack[i];
                }
            }

            resultTexture.SetPixels32(resultPixels);
            resultTexture.Apply();
        }
        #endregion

        public static Color[] ReadCameraPixels(Color color)
        {
            Camera camera = Camera.main;
            camera.backgroundColor = color;
            SetHdrpCameraBackgroundColor(color);
            camera.Render();
            Texture2D tex = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            return tex.GetPixels();
        }

        public static Color32[] ReadCameraPixels32(Color color)
        {
            Camera camera = Camera.main;
            camera.backgroundColor = color;
            SetHdrpCameraBackgroundColor(color);
            camera.Render();
            Texture2D tex = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            return tex.GetPixels32();
        }

        public static void SetHdrpCameraBackgroundColor(Color color)
        {
            Type hdrpCameraType = GetHdrpCameraType();
            if (hdrpCameraType == null)
                return;

            Component hdrpCameraComponent = Camera.main.gameObject.GetComponent(hdrpCameraType);
            FieldInfo hdrpCameraBackgroundColorHdrField = hdrpCameraType.GetField("backgroundColorHDR");
            if (hdrpCameraComponent != null && hdrpCameraBackgroundColorHdrField != null)
                hdrpCameraBackgroundColorHdrField.SetValue(hdrpCameraComponent, color);
        }

        public static Type GetHdrpCameraType()
        {
            if (Camera.main == null)
                return null;

            Type hdrpCameraTypeA = Type.GetType(
                "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, " +
                "Unity.RenderPipelines.HighDefinition.Runtime",
                false, true
            );
            Type hdrpCameraTypeB = Type.GetType(
                "UnityEngine.Experimental.Rendering.HDPipeline.HDAdditionalCameraData, " +
                "Unity.RenderPipelines.HighDefinition.Runtime",
                false, true
            );

            return hdrpCameraTypeA ?? hdrpCameraTypeB;
        }
    }
}
