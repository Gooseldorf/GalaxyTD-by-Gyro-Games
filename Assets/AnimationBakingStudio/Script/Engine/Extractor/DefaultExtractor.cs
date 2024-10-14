using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public class DefaultExtractor : Extractor
    {
        [SerializeField]
        private ComputeShader computeShader;

        [SerializeField, Range(0, 1)]
        private float alphaThreshold = 0.0f;

        #region Play Mode Baking
        private List<Color[]> pixelsOnBlackList;
        private List<Color[]> pixelsOnWhiteList;

        public override void Setup(int numOfFrames)
        {
            pixelsOnBlackList = new List<Color[]>();
            pixelsOnWhiteList = new List<Color[]>();
        }

        public override void Clear()
        {
            pixelsOnBlackList = null;
            pixelsOnWhiteList = null;
        }

        public override void ReadAndKeepRawPixels_AllFrames(Model model)
        {
            Color[] pixelsOnBlack = ExtractionHelper.ReadCameraPixels(Color.black);
            pixelsOnBlackList.Add(pixelsOnBlack);

            Color[] pixelsOnWhite = ExtractionHelper.ReadCameraPixels(Color.white);
            pixelsOnWhiteList.Add(pixelsOnWhite);
        }

        public override void ExtractValidPixels_OneFrame(int frameIndex, ref Texture2D resultTexture)
        {
            Color[] pixelsOnBlack = pixelsOnBlackList[frameIndex];
            Color[] pixelsOnWhite = pixelsOnWhiteList[frameIndex];

            ExtractValidPixels(pixelsOnBlack, pixelsOnWhite, ref resultTexture);
        }
        #endregion

        #region Editor Mode Baking
        public override void CapturePixels(Model model, ref Texture2D resultTexture)
        {
            Color[] pixelsOnBlack = ExtractionHelper.ReadCameraPixels(Color.black);
            Color[] pixelsOnWhite = ExtractionHelper.ReadCameraPixels(Color.white);

            ExtractValidPixels(pixelsOnBlack, pixelsOnWhite, ref resultTexture);
        }
        #endregion

        private void ExtractValidPixels(Color[] pixelsOnBlack, Color[] pixelsOnWhite, ref Texture2D resultTexture)
        {
            int textureSize = resultTexture.width * resultTexture.height;
            Color[] resultPixels = Enumerable.Repeat(Color.clear, textureSize).ToArray();

            if (EngineConstants.gpuUse && SystemInfo.supportsComputeShaders && computeShader != null &&
                textureSize > EngineConstants.GPU_THREAD_SIZE && alphaThreshold == 0)
            {
                int kernelIndex = computeShader.FindKernel("DefaultExtractFunction");

                ComputeBuffer blackBuffer = new ComputeBuffer(textureSize, 16);
                blackBuffer.SetData(pixelsOnBlack);
                computeShader.SetBuffer(kernelIndex, "blackBuffer", blackBuffer);

                ComputeBuffer whiteBuffer = new ComputeBuffer(textureSize, 16);
                whiteBuffer.SetData(pixelsOnWhite);
                computeShader.SetBuffer(kernelIndex, "whiteBuffer", whiteBuffer);

                computeShader.SetInt("width", resultTexture.width);

                ComputeBuffer resultBuffer = new ComputeBuffer(textureSize, 16);
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
                    Color pixelOnBlack = pixelsOnBlack[i];
                    Color pixelOnWhite = pixelsOnWhite[i];

                    float redDiff = pixelOnWhite.r - pixelOnBlack.r;
                    float greenDiff = pixelOnWhite.g - pixelOnBlack.g;
                    float blueDiff = pixelOnWhite.b - pixelOnBlack.b;

                    float alpha = 1f - Mathf.Min(Mathf.Min(redDiff, greenDiff), blueDiff);
                    if (alpha <= alphaThreshold)
                        continue;

                    Color pixel = pixelOnBlack / alpha;
                    pixel.a = alpha;

                    resultPixels[i] = pixel;
                }
            }

            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();
        }
    }
}
