using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public class ParticleExtractor : Extractor
    {
        [SerializeField]
        private Color backgroundColor = Color.black;

        [SerializeField]
        private AlpahExtractionChannel alphaExtractionChannel = AlpahExtractionChannel.MinDiff;

        [SerializeField, Range(0, 1)]
        private float alphaThreshold = 0.0f;

        [SerializeField]
        private AnimationCurve alphaAddingCurve = AnimationCurve.Linear(0, 0, 0, 0);

        #region Play Mode Baking
        private List<Color[]> pixelsOnBgList;

        public override void Setup(int numOfFrames)
        {
            pixelsOnBgList = new List<Color[]>();
        }

        public override void Clear()
        {
            pixelsOnBgList = null;
        }

        public override void ReadAndKeepRawPixels_AllFrames(Model model)
        {
            Color[] pixelsOnBg = ExtractionHelper.ReadCameraPixels(backgroundColor);
            pixelsOnBgList.Add(pixelsOnBg);
        }

        public override void ExtractValidPixels_OneFrame(int frameIndex, ref Texture2D resultTexture)
        {
            Color[] pixelsOnBg = pixelsOnBgList[frameIndex];
            ExtractValidPixels(pixelsOnBg, ref resultTexture);
        }
        #endregion

        #region Editor Mode Baking
        public override void CapturePixels(Model model, ref Texture2D resultTexture)
        {
            Color[] pixelsOnBg = ExtractionHelper.ReadCameraPixels(backgroundColor);
            ExtractValidPixels(pixelsOnBg, ref resultTexture);
        }
        #endregion

        private void ExtractValidPixels(Color[] pixelsOnBg, ref Texture2D resultTexture)
        {
            int textureSize = resultTexture.width * resultTexture.height;
            Color[] resultPixels = Enumerable.Repeat(Color.clear, textureSize).ToArray();

            for (int i = 0; i < textureSize; ++i)
            {
                Color pixelOnBg = pixelsOnBg[i];

                float colorDiff = 0f;
                switch (alphaExtractionChannel)
                {
                    case AlpahExtractionChannel.Red:
                        colorDiff = 1f - pixelOnBg.r;
                        break;

                    case AlpahExtractionChannel.Green:
                        colorDiff = 1f - pixelOnBg.g;
                        break;

                    case AlpahExtractionChannel.Blue:
                        colorDiff = 1f - pixelOnBg.b;
                        break;

                    case AlpahExtractionChannel.MinDiff:
                        float redDiff = 1f - pixelOnBg.r;
                        float greenDiff = 1f - pixelOnBg.g;
                        float blueDiff = 1f - pixelOnBg.b;
                        colorDiff = Mathf.Min(Mathf.Min(redDiff, greenDiff), blueDiff);
                        break;
                }

                float alpha = 1f - colorDiff;

                if (alpha <= alphaThreshold)
                    continue;

                Color pixel = pixelOnBg / alpha;
                alpha += alphaAddingCurve.Evaluate(alpha);
                pixel.a = Mathf.Clamp01(alpha);
                pixel.a = alpha;

                resultPixels[i] = pixel;
            }

            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();
        }
    }
}
