using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ABS
{
    public static class TextureHelper
    {
        public static bool GetPixelBound(Texture2D tex, PixelVector pivot, PixelBound pixelBound)
        {
            return GetPixelBound(tex, pivot, pixelBound, new PixelBound());
        }

        public static bool GetPixelBound(Texture2D tex, PixelVector pivot, PixelBound pixelBound, PixelBound minPixelBound)
        {
            if (!GetValidPixelBound(tex, minPixelBound))
                return false;

            pixelBound.min.x = Mathf.Min(minPixelBound.min.x, pivot.x);
            pixelBound.min.y = Mathf.Min(minPixelBound.min.y, pivot.y);
            pixelBound.max.x = Mathf.Max(minPixelBound.max.x, pivot.x);
            pixelBound.max.y = Mathf.Max(minPixelBound.max.y, pivot.y);

            return true;
        }

        public static bool GetValidPixelBound(Texture2D tex, PixelBound pixelBound)
        {
            Color[] colors = tex.GetPixels();

            bool validPixelExist = false;
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    float alpha = colors[y * tex.width + x].a;
                    if (alpha != 0)
                    {
                        pixelBound.min.x = x;
                        validPixelExist = true;
                        goto ENDMINX;
                    }
                }
            }

            ENDMINX:
            if (!validPixelExist)
                return false;

            validPixelExist = false;
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = pixelBound.min.x; x < tex.width; x++)
                {
                    float alpha = colors[y * tex.width + x].a;
                    if (alpha != 0)
                    {
                        pixelBound.min.y = y;
                        validPixelExist = true;
                        goto ENDMINY;
                    }
                }
            }

            ENDMINY:
            if (!validPixelExist)
                return false;

            validPixelExist = false;
            for (int x = tex.width - 1; x >= pixelBound.min.x; x--)
            {
                for (int y = pixelBound.min.y; y < tex.height; y++)
                {
                    float alpha = colors[y * tex.width + x].a;
                    if (alpha != 0)
                    {
                        pixelBound.max.x = x;
                        validPixelExist = true;
                        goto ENDMAXX;
                    }
                }
            }

            ENDMAXX:
            if (!validPixelExist)
                return false;

            validPixelExist = false;
            for (int y = tex.height - 1; y >= pixelBound.min.y; y--)
            {
                for (int x = pixelBound.min.x; x <= pixelBound.max.x; x++)
                {
                    float alpha = colors[y * tex.width + x].a;
                    if (alpha != 0)
                    {
                        pixelBound.max.y = y;
                        validPixelExist = true;
                        goto ENDMAXY;
                    }
                }
            }

            ENDMAXY:
            if (!validPixelExist)
                return false;

            return true;
        }

        public static void ExpandBound(PixelBound bound, PixelBound uniformBound)
        {
            uniformBound.min.x = Mathf.Min(uniformBound.min.x, bound.min.x);
            uniformBound.max.x = Mathf.Max(uniformBound.max.x, bound.max.x);
            uniformBound.min.y = Mathf.Min(uniformBound.min.y, bound.min.y);
            uniformBound.max.y = Mathf.Max(uniformBound.max.y, bound.max.y);
        }

        private static ComputeShader trimShader = null;
        private static ComputeShader TrimShader
        {
            get
            {
                if (trimShader == null)
                    trimShader = AssetHelper.FindAsset<ComputeShader>("Shader/Compute", "Trim");
                return trimShader;
            }
        }

        public static Texture2D TrimTexture(Texture2D tex, PixelBound bound, int margin, Color32 defaultColor, bool isNormalMap = false)
        {
            if (tex == null)
                return Texture2D.whiteTexture;

            PixelBound marginedBound = bound.CopyExtendedBy(margin);

            int marginedWidth = marginedBound.max.x - marginedBound.min.x + 1;
            int marginedHeight = marginedBound.max.y - marginedBound.min.y + 1;

            if (marginedWidth < 0 || marginedHeight < 0)
            {
                Debug.LogError("Minus margin is too much.");
                return Texture2D.whiteTexture;
            }

            Texture2D resultTexture = new Texture2D(marginedWidth, marginedHeight, TextureFormat.ARGB32, false);

            PixelBound minCopyBound = (margin < 0) ? marginedBound : bound.Copy();
            int minCopyWidth = minCopyBound.max.x - minCopyBound.min.x + 1;
            int minCopyHeight = minCopyBound.max.y - minCopyBound.min.y + 1;

            int srcFirstX = minCopyBound.min.x;
            int srcFirstY = minCopyBound.min.y;
            int destFirstX = (margin < 0) ? 0 : margin;
            int destFirstY = (margin < 0) ? 0 : margin;

            Color32[] srcPixels = tex.GetPixels32();
            Color32[] destPixels = Enumerable.Repeat(defaultColor, marginedWidth * marginedHeight).ToArray();

            if (EngineConstants.gpuUse && SystemInfo.supportsComputeShaders && TrimShader != null &&
                !isNormalMap && marginedWidth * marginedHeight > EngineConstants.GPU_THREAD_SIZE)
            {
                int kernelIndex = TrimShader.FindKernel("TrimFunction");

                ComputeBuffer srcBuffer = new ComputeBuffer(tex.width * tex.height, 4);
                srcBuffer.SetData(srcPixels);
                TrimShader.SetBuffer(kernelIndex, "srcBuffer", srcBuffer);

                TrimShader.SetInt("srcFirstX", srcFirstX);
                TrimShader.SetInt("srcFirstY", srcFirstY);
                TrimShader.SetInt("srcWidth", tex.width);

                TrimShader.SetInt("destFirstX", destFirstX);
                TrimShader.SetInt("destFirstY", destFirstY);
                TrimShader.SetInt("destWidth", marginedWidth);

                ComputeBuffer destBuffer = new ComputeBuffer(marginedWidth * marginedHeight, 4);
                TrimShader.SetBuffer(kernelIndex, "destBuffer", destBuffer);

                TrimShader.Dispatch(kernelIndex, EngineConstants.GPU_THREAD_SIZE, 1, 1);
                destBuffer.GetData(destPixels);

                srcBuffer.Release();
                destBuffer.Release();
            }
            else
            {
                for (int i = 0; i < minCopyWidth * minCopyHeight; ++i)
                {
                    int x = i % minCopyWidth;
                    int y = i / minCopyWidth;
                    int srcIndex = (srcFirstY + y) * tex.width + (srcFirstX + x);
                    int destIndex = (destFirstY + y) * marginedWidth + (destFirstX + x);
                    destPixels[destIndex] = srcPixels[srcIndex];
                }
            }

            resultTexture.SetPixels32(destPixels);
            resultTexture.Apply();

            return resultTexture;
        }

        public static Texture2D ScaleTexture(Texture2D source, int destWidth, int destHeight)
        {
            Color[] pixels = Enumerable.Repeat(Color.clear, destWidth * destHeight).ToArray();
            float incX = 1.0f / (float)destWidth;
            float incY = 1.0f / (float)destHeight;
            for (int i = 0; i < pixels.Length; i++)
            {
                float u = incX * ((float)i % destWidth);
                float v = incY * ((float)Mathf.Floor(i / destWidth));
                pixels[i] = source.GetPixelBilinear(u, v);
            }

            Texture2D dest = new Texture2D(destWidth, destHeight, source.format, false);
            dest.SetPixels(pixels, 0);
            dest.Apply();

            return dest;
        }

        public static string SaveTexture(string dirPath, string fileName, Texture2D tex)
        {
            string filePath;

            try
            {
                filePath = Path.Combine(dirPath, fileName + ".png");
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);
            }
            catch (Exception e)
            {
                throw e;
            }

            return filePath;
        }
    }
}
