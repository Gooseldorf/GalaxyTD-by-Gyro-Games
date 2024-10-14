using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public static class CapturingHelper
    {
        #region Play Mode Baking
        public static void ReadAndKeepRawPixelsManagingShadow(Model model, Studio studio)
        {
            if (studio.shadow.type != ShadowType.None)
            {
                if (studio.shadow.shadowOnly)
                {
                    PickOutSimpleShadow(studio);
                    {
                        Vector3 posBackup = ThrowOutModelFarAway(model);
                        {
                            ReadAndKeepRawPixelsForMain_AllFrames(model, studio);
                        }
                        PutModelBackInPlace(model, posBackup);
                    }
                    PushInSimpleShadow(model, studio);
                }
                else
                {
                    ReadAndKeepRawPixelsForMain_AllFrames(model, studio);
                }
            }
            else
            {
                ReadAndKeepRawPixelsForMain_AllFrames(model, studio);
            }
        }

        public static void ReadAndKeepRawPixelsForMain_AllFrames(Model model, Studio studio)
        {
            RenderTexture.active = Camera.main.targetTexture;
            studio.extraction.com.ReadAndKeepRawPixels_AllFrames(model);
            RenderTexture.active = null;
        }

        public static Texture2D ExtractValidPixelsForMain_OneFrame(int frameIndex, Studio studio)
        {
            if (Camera.main == null || studio.extraction.com == null)
                return Texture2D.whiteTexture;

            RenderTexture targetTex = Camera.main.targetTexture;
            Texture2D resultTexure = new Texture2D(targetTex.width, targetTex.height, TextureFormat.ARGB32, false);

            studio.extraction.com.ExtractValidPixels_OneFrame(frameIndex, ref resultTexure);

            return resultTexure;
        }
        #endregion

        #region Editor Mode Baking
        public static Texture2D CapturePixelsManagingShadow(Model model, Studio studio)
        {
            Texture2D resultTexture;

            if (studio.shadow.type != ShadowType.None)
            {
                if (studio.shadow.shadowOnly)
                {
                    PickOutSimpleShadow(studio);
                    {
                        Vector3 posBackup = ThrowOutModelFarAway(model);
                        {
                            resultTexture = CapturePixelsForMain(model, studio);
                        }
                        PutModelBackInPlace(model, posBackup);
                    }
                    PushInSimpleShadow(model, studio);
                }
                else
                {
                    resultTexture = CapturePixelsForMain(model, studio);
                }
            }
            else
            {
                resultTexture = CapturePixelsForMain(model, studio);
            }

            return resultTexture;
        }

        public static Texture2D CapturePixelsForMain(Model model, Studio studio)
        {
            if (Camera.main == null || studio.extraction.com == null)
                return Texture2D.whiteTexture;

            RenderTexture targetTex = RenderTexture.active = Camera.main.targetTexture;
            Texture2D resultTexure = new Texture2D(targetTex.width, targetTex.height, TextureFormat.ARGB32, false);

            studio.extraction.com.CapturePixels(model, ref resultTexure);

            RenderTexture.active = null;

            return resultTexure;
        }
        #endregion

        private static void PickOutSimpleShadow(Studio studio)
        {
            if (studio.shadow.type == ShadowType.Simple)
                studio.shadow.obj.transform.parent = null;
        }

        private static void PushInSimpleShadow(Model model, Studio studio)
        {
            if (studio.shadow.type == ShadowType.Simple)
                studio.shadow.obj.transform.parent = model.transform;
        }

        private static Vector3 ThrowOutModelFarAway(Model model)
        {
            Vector3 originalPosition = model.transform.position;
            model.transform.position = CreateFarAwayPosition();
            return originalPosition;
        }

        private static void PutModelBackInPlace(Model model, Vector3 originalPosition)
        {
            model.transform.position = originalPosition;
        }

        private static Vector3 CreateFarAwayPosition()
        {
            return new Vector3(10000f, 0f, 0f);
        }

        #region Normal Map
        private static Material normalMapMaterial = null;
        private static Material NormalMapMaterial
        {
            get
            {
                if (normalMapMaterial == null)
                    normalMapMaterial = AssetHelper.FindAsset<Material>("NormalMap", "NormalMap");
                return normalMapMaterial;
            }
        }

        #region NormalMap - Play Mode Baking
        private static List<Color32[]> normalPixelsOnBlackList;
        private static List<Color32[]> normalPixelsOnWhiteList;

        public static void SetupNormalPixelsLists()
        {
            normalPixelsOnBlackList = new List<Color32[]>();
            normalPixelsOnWhiteList = new List<Color32[]>();
        }

        public static void ClearNormalPixelsLists()
        {
            normalPixelsOnBlackList = null;
            normalPixelsOnWhiteList = null;
        }

        public static void ReadAndKeepRawPixelsForNormal_AllFrames(Model model, float rotX, float rotY, GameObject shadowObj)
        {
            model.BackupAllMaterials();

            NormalMapMaterial.SetFloat("_RotX", rotX);
            NormalMapMaterial.SetFloat("_RotY", rotY);
            model.ChangeAllMaterials(NormalMapMaterial);

            if (shadowObj != null)
                shadowObj.SetActive(false);

            RenderTexture.active = Camera.main.targetTexture;
            ExtractionHelper.ReadAndKeepRawOpaquePixels(normalPixelsOnBlackList, normalPixelsOnWhiteList);
            RenderTexture.active = null;

            if (shadowObj != null)
                shadowObj.SetActive(true);

            model.RestoreAllMaterials();
        }

        public static Texture2D ExtractValidPixelsForNormal_OneFrame(int frameIndex)
        {
            if (Camera.main == null)
                return Texture2D.whiteTexture;

            Color32[] pixelsOnBlack = normalPixelsOnBlackList[frameIndex];
            Color32[] pixelsOnWhite = normalPixelsOnWhiteList[frameIndex];

            RenderTexture targetTex = Camera.main.targetTexture;
            Texture2D resultTexture = new Texture2D(targetTex.width, targetTex.height, TextureFormat.ARGB32, false);

            ExtractionHelper.ExtractOpaqueValidPixels(pixelsOnBlack, pixelsOnWhite, ref resultTexture, EngineConstants.NORMALMAP_COLOR32);

            return resultTexture;
        }
        #endregion

        #region NormalMap - Editor Mode Baking
        public static Texture2D CapturePixelsForNormal(Model model, float rotX, float rotY, GameObject shadowObj)
        {
            if (Camera.main == null)
                return Texture2D.whiteTexture;

            model.BackupAllMaterials();

            NormalMapMaterial.SetFloat("_RotX", rotX);
            NormalMapMaterial.SetFloat("_RotY", rotY);
            model.ChangeAllMaterials(NormalMapMaterial);

            if (shadowObj != null)
                shadowObj.SetActive(false);

            RenderTexture targetTex = RenderTexture.active = Camera.main.targetTexture;
            Texture2D resultTexture = new Texture2D(targetTex.width, targetTex.height, TextureFormat.ARGB32, false);

            Color32[] pixelsOnBlack = ExtractionHelper.ReadCameraPixels32(Color.black);
            Color32[] pixelsOnWhite = ExtractionHelper.ReadCameraPixels32(Color.white);
            ExtractionHelper.ExtractOpaqueValidPixels(pixelsOnBlack, pixelsOnWhite, ref resultTexture, EngineConstants.NORMALMAP_COLOR32);

            RenderTexture.active = null;

            if (shadowObj != null)
                shadowObj.SetActive(true);

            model.RestoreAllMaterials();

            return resultTexture;
        }
        #endregion
        #endregion
    }
}
