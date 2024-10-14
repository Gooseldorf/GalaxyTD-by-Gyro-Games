using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
	public class OpaqueExtractor : Extractor
    {
        #region Play Mode Baking
        private List<Color32[]> pixelsOnBlackList;
        private List<Color32[]> pixelsOnWhiteList;

        public override void Setup(int numOfFrames)
        {
            pixelsOnBlackList = new List<Color32[]>();
            pixelsOnWhiteList = new List<Color32[]>();
        }

        public override void Clear()
        {
            pixelsOnBlackList = null;
            pixelsOnWhiteList = null;
        }

        public override void ReadAndKeepRawPixels_AllFrames(Model model)
        {
            ExtractionHelper.ReadAndKeepRawOpaquePixels(pixelsOnBlackList, pixelsOnWhiteList);
        }

        public override void ExtractValidPixels_OneFrame(int frameIndex, ref Texture2D resultTexture)
        {
            Color32[] pixelsOnBlack = pixelsOnBlackList[frameIndex];
            Color32[] pixelsOnWhite = pixelsOnWhiteList[frameIndex];

            ExtractionHelper.ExtractOpaqueValidPixels(pixelsOnBlack, pixelsOnWhite, ref resultTexture, EngineConstants.CLEAR_COLOR32);
        }
        #endregion

        #region Editor Mode Baking
        public override void CapturePixels(Model model, ref Texture2D resultTexture)
        {
            Color32[] pixelsOnBlack = ExtractionHelper.ReadCameraPixels32(Color.black);
            Color32[] pixelsOnWhite = ExtractionHelper.ReadCameraPixels32(Color.white);

            ExtractionHelper.ExtractOpaqueValidPixels(pixelsOnBlack, pixelsOnWhite, ref resultTexture, EngineConstants.CLEAR_COLOR32);
        }
        #endregion
    }
}
