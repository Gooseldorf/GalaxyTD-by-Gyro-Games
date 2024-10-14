using UnityEngine;

namespace ABS
{
    public abstract class Extractor : MonoBehaviour
    {
        public enum AlpahExtractionChannel
        {
            Red,
            Green,
            Blue,
            MinDiff
        }

        #region Play Mode Baking
        public abstract void Setup(int numOfFrames);
        public abstract void Clear();

        public abstract void ReadAndKeepRawPixels_AllFrames(Model model);

        public abstract void ExtractValidPixels_OneFrame(int frameIndex, ref Texture2D resultTexture);
        #endregion

        #region Editor Mode Baking
        public abstract void CapturePixels(Model model, ref Texture2D resultTexture);
        #endregion
    }
}
