using UnityEngine;

namespace Data.Managers
{
    public enum GraphicsQuality { None = 0, Low = 1, Medium = 2, High = 3 };

    public static class ScreenResolutionManager
    {
        private static int originalHeight = 0;
        private static int originalWidth = 0;

        private static GraphicsQuality graphicsQuality = GraphicsQuality.None;

        public static GraphicsQuality GraphicsQuality
        {
            get
            {
                if (graphicsQuality == GraphicsQuality.None)
                {
                    originalHeight = Screen.currentResolution.height;
                    originalWidth = Screen.currentResolution.width;
                    graphicsQuality = (GraphicsQuality)PlayerPrefs.GetInt(nameof(GraphicsQuality), 3);
                    UpdateResolution();
                }

                return graphicsQuality;
            }
            set
            {
                graphicsQuality = value;
                Debug.Log($"{graphicsQuality} {(int)graphicsQuality}");
                PlayerPrefs.SetInt(nameof(GraphicsQuality), (int)graphicsQuality);
                PlayerPrefs.Save();
                UpdateResolution();
            }
        }

        private static void UpdateResolution()
        {
            switch (graphicsQuality)
            {
                case GraphicsQuality.Low:
                    Screen.SetResolution(originalWidth / 2, originalHeight / 2, true);
                    break;
                case GraphicsQuality.Medium:
                    Screen.SetResolution((originalWidth * 3) / 4, (originalHeight * 3) / 4, true);
                    break;
                default:
                    Screen.SetResolution(originalWidth, originalHeight, true);
                    break;
            }

            Debug.Log($"{Screen.currentResolution.height}:{Screen.currentResolution.width}");
        }
    }
}