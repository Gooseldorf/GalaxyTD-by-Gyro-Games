using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ABS.Demo
{
    public class SimilarColorExtractor : Extractor
    {
        [SerializeField]
        private Color backgroundColor = Color.black;

        [SerializeField, Range(0, 1)]
        private float colorThreshold = 0;

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
            Color[] resultPixels = Enumerable.Repeat(Color.clear, pixelsOnBg.Length).ToArray();

            for (int i = 0; i < resultTexture.width * resultTexture.height; i++)
            {
                Color pixel = pixelsOnBg[i];

                if (colorThreshold == 0)
                {
                    if (pixel == backgroundColor)
                        continue;
                }
                else
                {
                    Vector3 colorVector1 = new Vector3(pixel.r, pixel.g, pixel.b);
                    Vector3 colorVector2 = new Vector3(backgroundColor.r, backgroundColor.g, backgroundColor.b);
                    if ((colorVector1 - colorVector2).magnitude < colorThreshold)
                        continue;
                }

                resultPixels[i] = pixel;
            }

            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();
        }
    }
}
