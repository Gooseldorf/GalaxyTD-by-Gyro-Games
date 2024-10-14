using System.IO;
using UnityEngine;
using UnityEditor;

namespace ABS
{
	public static class ShadowHelper
    {
        public static void LocateShadowToModel(Model model, Studio studio)
        {
            if (model == null || studio.shadow.obj == null || studio.shadow.type == ShadowType.None)
                return;

            Vector3 modelBottom = model.ComputedBottom;
            modelBottom.y -= 0.01f;

            Transform shadowT = studio.shadow.obj.transform;

            if (studio.shadow.type == ShadowType.Simple)
            {
                if (shadowT.parent != model.transform)
                {
                    shadowT.parent = model.transform;
                    shadowT.localRotation = Quaternion.identity;
                }
                shadowT.position = modelBottom;
            }
            else
            {
                shadowT.position = modelBottom;
            }
        }

        public static void ScaleSimpleShadow(Model model, Studio studio)
        {
            if (model == null || studio.shadow.obj == null)
                return;

            Renderer shadowRenderer = studio.shadow.obj.GetComponent<Renderer>();
            if (!model.IsReady() || shadowRenderer == null)
                return;

            Vector2 shadowScale = model.toUseMyShadowScale ? model.simpleShadowScale : studio.shadow.simple.scale;
            if (shadowScale.magnitude == 0f)
                return;

            Transform shadowT = studio.shadow.obj.transform;
            shadowT.localScale = Vector3.one;

            Vector3 modelSize = model.GetDynamicSize();
            Debug.Assert(shadowRenderer.bounds.size.x == shadowRenderer.bounds.size.z);
            float shadowLength = shadowRenderer.bounds.size.x;
            float xScaleRatio = modelSize.x / shadowLength;
            float zScaleRatio = modelSize.z / shadowLength;

            if (xScaleRatio > 0f && zScaleRatio > 0f)
            {
                xScaleRatio *= shadowScale.x;
                zScaleRatio *= shadowScale.y;

                shadowT.localScale = new Vector3
                (
                    shadowT.localScale.x * xScaleRatio,
                    1.0f,
                    shadowT.localScale.z * zScaleRatio
                );
            }
            else
            {
                shadowT.localScale = new Vector3
                (
                    shadowScale.x,
                    1.0f,
                    shadowScale.y
                );
            }
        }

        public static void ScaleSimpleShadowDynamically(Vector3 modelBaseSize, Vector3 simpleShadowBaseScale, MeshModel meshModel, Studio studio)
        {
            Vector3 modelCurrentSize = meshModel.GetSize();

            float xScaleRatio = modelBaseSize.x / modelCurrentSize.x;
            float zScaleRatio = modelBaseSize.z / modelCurrentSize.z;
            Debug.Assert(xScaleRatio > 0f && zScaleRatio > 0f);

            Transform shadowT = studio.shadow.obj.transform;
            shadowT.localScale = simpleShadowBaseScale;

            shadowT.localScale = new Vector3
            (
                shadowT.localScale.x * xScaleRatio,
                1.0f,
                shadowT.localScale.z * zScaleRatio
            );
        }

        public static void GetCameraAndFieldObject(GameObject shadowObj, out Camera camera, out GameObject fieldObj)
        {
            camera = null;
            Transform cameraT = shadowObj.transform.Find("Camera");
            if (cameraT != null)
            {
                camera = cameraT.gameObject.GetComponent<Camera>();
                camera.orthographicSize = Camera.main.orthographicSize;
            }

            fieldObj = null;
            Transform fieldT = shadowObj.transform.Find("Field");
            if (fieldT != null)
                fieldObj = fieldT.gameObject;
        }

        public static void ScaleShadowField(Camera camera, GameObject fieldObj)
        {
            Renderer renderer = fieldObj.GetComponent<Renderer>();
            if (renderer == null)
                return;

            fieldObj.transform.localScale = Vector3.one;

            Vector3 maxWorldPos = camera.ScreenToWorldPoint(new Vector3(camera.pixelWidth, camera.pixelHeight));
            Vector3 minWorldPos = camera.ScreenToWorldPoint(Vector3.zero);
            Vector3 texWorldSize = maxWorldPos - minWorldPos;

            fieldObj.transform.localScale = new Vector3
            (
                texWorldSize.x / renderer.bounds.size.x,
                1f,
                texWorldSize.z / renderer.bounds.size.z
            );
        }

        public static void ScaleMatteField(MeshModel model, GameObject fieldObj, LightProperty lit)
        {
            if (model == null || model.mainRenderer == null || fieldObj == null || lit == null)
                return;

            Renderer fieldRenderer = fieldObj.GetComponent<Renderer>();
            if (fieldRenderer == null)
                return;

            fieldObj.transform.localScale = Vector3.one;

            float tan = Mathf.Tan(lit.com.transform.rotation.eulerAngles.x * Mathf.Deg2Rad);
            Vector3 modelSize = model.mainRenderer.bounds.size;
            float modelHalfWidth = Mathf.Max(modelSize.x, modelSize.z) / 2;
            float fieldWidth = (modelSize.y / tan + modelHalfWidth) * 2;

            fieldObj.transform.localScale = new Vector3
            (
                fieldWidth / fieldRenderer.bounds.size.x,
                1f,
                fieldWidth / fieldRenderer.bounds.size.z
            );
        }
    }
}
