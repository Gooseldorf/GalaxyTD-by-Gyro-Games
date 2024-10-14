using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    [DisallowMultipleComponent]
    public abstract class Model : MonoBehaviour
    {
        public bool toUseMyCamSize;
        public float cameraOrthoSize = 2.5f;
        public float cameraFOV = 45;

        public Vector3 cameraOffset;

        public bool isGroundPivot = false;

        public bool toUseMyShadowScale = false;
        public Vector2 simpleShadowScale = Vector3.one;

        public GameObject spritePrefab;
        public PrefabBuilder prefabBuilder;

        public string nameSuffix = "";

        private readonly Dictionary<int, Shader> shaderBackup = new Dictionary<int, Shader>();
        private readonly Dictionary<int, Material[]> materialsBackup = new Dictionary<int, Material[]>();

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public virtual Vector3 ComputedCenter
        {
            get
            {
                return GetPosition();
            }
        }

        public virtual Vector3 ComputedBottom
        {
            get
            {
                Vector3 position = GetPosition();

                float x = position.x;
                float y = isGroundPivot ? 0 : position.y;
                float z = position.z;

                return new Vector3(x, y, z);
            }
        }

        public Vector3 ComputedForward
        {
            get
            {
                return transform.forward;
            }
        }

        public Vector3 ComputedRight
        {
            get
            {
                return transform.right;
            }
        }

        public abstract Vector3 GetSize();
        public virtual Vector3 GetDynamicSize()
        {
            return GetSize();
        }

        public abstract Vector3 GetMinPos();
        public virtual Vector3 GetExactMinPos()
        {
            return GetMinPos();
        }

        public abstract Vector3 GetMaxPos();
        public virtual Vector3 GetExactMaxPos()
        {
            return GetMaxPos();
        }

        public Vector3 GetPivotPosition()
        {
            return ComputedBottom;
        }

        public abstract bool IsReady();

        public abstract bool IsTileAvailable();

        public static bool IsMeshModel(Model model)
        {
            return model is MeshModel;
        }

        public static MeshModel AsMeshModel(Model model)
        {
            return model as MeshModel;
        }

        public static bool IsParticleModel(Model model)
        {
            return model is ParticleModel;
        }

        public static ParticleModel AsParticleModel(Model model)
        {
            return model as ParticleModel;
        }

        public void Rotate(float angle)
        {
            transform.localRotation = Quaternion.identity;
            float angleDiff = Vector3.Angle(ComputedForward, transform.forward);
            transform.localRotation = Quaternion.Euler(new Vector3(0, angleDiff + angle, 0));
        }

        public void BackupAllShaders()
        {
            if (shaderBackup.Keys.Count > 0)
                shaderBackup.Clear();

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rndr in renderers)
            {
                foreach (Material mtrl in rndr.sharedMaterials)
                {
                    if (mtrl != null && mtrl.shader != null)
                    {
                        if (!shaderBackup.ContainsKey(mtrl.GetInstanceID()))
                            shaderBackup.Add(mtrl.GetInstanceID(), mtrl.shader);
                    }
                }
            }
        }

        public void ChangeAllShaders(Shader targetShader)
        {
            if (targetShader == null)
            {
                Debug.LogError("targetShader is null");
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rndr in renderers)
            {
                foreach (Material mtrl in rndr.sharedMaterials)
                {
                    if (mtrl != null && mtrl.shader != null)
                        mtrl.shader = targetShader;
                }
            }
        }

        public void RestoreAllShaders()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rndr in renderers)
            {
                foreach (Material mtrl in rndr.sharedMaterials)
                {
                    if (mtrl != null && mtrl.shader != null)
                        mtrl.shader = shaderBackup[mtrl.GetInstanceID()];
                }
            }

            shaderBackup.Clear();
        }

        public void BackupAllMaterials()
        {
            if (materialsBackup.Count > 0)
                materialsBackup.Clear();

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rndr in renderers)
                materialsBackup.Add(rndr.GetInstanceID(), rndr.sharedMaterials);
        }

        public void ChangeAllMaterials(Material targetMaterial)
        {
            if (targetMaterial == null)
            {
                Debug.LogError("targetMaterial is null");
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rndr in renderers)
            {
                Material[] newMaterials = new Material[rndr.sharedMaterials.Length];
                for (int i = 0; i < newMaterials.Length; ++i)
                    newMaterials[i] = targetMaterial;
                rndr.sharedMaterials = newMaterials;
            }
        }

        public void SetFloatToAllMaterials(string name, float value)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rndr in renderers)
            {
                foreach (Material mtrl in rndr.materials)
                    mtrl.SetFloat(name, value);
            }
        }

        public void RestoreAllMaterials()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rndr in renderers)
                rndr.sharedMaterials = materialsBackup[rndr.GetInstanceID()];

            materialsBackup.Clear();
        }

        void OnDrawGizmos()
        {
            if (GetSize().magnitude == 0.0f)
                return;

            Vector3 position = GetPosition();

            Gizmos.color = Color.yellow;
            float lineLength = GetSize().magnitude;
            Vector3 headPos = position + transform.forward * lineLength;
            Gizmos.DrawLine(position, headPos);
            float arrowLength = lineLength / 10.0f;
            Vector3 arrowEnd = headPos + (-transform.forward) * arrowLength;
            Vector3 arrowEnd1 = arrowEnd + transform.right * arrowLength;
            Vector3 arrowEnd2 = arrowEnd + (-transform.right) * arrowLength;
            Gizmos.DrawLine(headPos, arrowEnd1);
            Gizmos.DrawLine(headPos, arrowEnd2);

            Gizmos.color = Color.magenta;
            headPos = ComputedCenter + ComputedForward * lineLength;
            Gizmos.DrawLine(ComputedCenter, headPos);
            arrowEnd = headPos + (-ComputedForward) * arrowLength;
            arrowEnd1 = arrowEnd + ComputedRight * arrowLength;
            arrowEnd2 = arrowEnd + (-ComputedRight) * arrowLength;
            Gizmos.DrawLine(headPos, arrowEnd1);
            Gizmos.DrawLine(headPos, arrowEnd2);

            DrawGizmoMore();
        }

        public virtual void DrawGizmoMore() { }
    }
}
