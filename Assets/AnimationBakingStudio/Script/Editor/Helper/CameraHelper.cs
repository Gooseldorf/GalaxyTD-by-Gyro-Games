using UnityEngine;

namespace ABS
{
    public static class CameraHelper
    {
        public static void LocateMainCameraToModel(Model model, Studio studio, float turnAngle = 0f)
        {
            if (Camera.main == null || model == null)
                return;

            Vector3 modelToCamDir = new Vector3(0, Mathf.Sin(studio.view.slopeAngle * Mathf.Deg2Rad), Mathf.Cos(studio.view.slopeAngle * Mathf.Deg2Rad));
            float modelToCamDist = 500;

            if (Model.IsMeshModel(model))
            {
                if (studio.cam.distanceType == DistanceType.Relative)
                    modelToCamDist = model.GetSize().magnitude * studio.cam.relativeDistance;
                else if (studio.cam.distanceType == DistanceType.Absolute)
                    modelToCamDist = studio.cam.absoluteDistance;
            }

            Transform mainCamT = Camera.main.transform;

            Vector3 camPos = model.ComputedCenter + modelToCamDir * modelToCamDist;
            camPos.x += model.cameraOffset.x;
            camPos.y -= model.cameraOffset.y;
            camPos.z -= model.cameraOffset.z;
            Quaternion camRot = Quaternion.LookRotation(-modelToCamDir);
            mainCamT.SetPositionAndRotation(camPos, camRot);

            if (studio.view.rotationType == RotationType.Camera && turnAngle > float.Epsilon)
                mainCamT.RotateAround(model.GetPosition(), Vector3.down, turnAngle);

            Camera.main.farClipPlane = modelToCamDist * 2;

            if (studio.lit.com != null)
            {
                if (studio.lit.cameraPositionFollow)
                    studio.lit.com.transform.position = mainCamT.position;
                if (studio.lit.cameraRotationFollow)
                    studio.lit.com.transform.rotation = mainCamT.rotation;
            }
        }

        public static void LookAtModel(Transform transf, Model model)
        {
            if (transf == null || model == null)
                return;

            Vector3 dirToModel = model.ComputedCenter - transf.position;
            Vector3 rightDir = Vector3.Cross(dirToModel, Vector3.up);
            transf.rotation = Quaternion.LookRotation(dirToModel.normalized, Vector3.Cross(dirToModel, rightDir));
        }
    }
}
