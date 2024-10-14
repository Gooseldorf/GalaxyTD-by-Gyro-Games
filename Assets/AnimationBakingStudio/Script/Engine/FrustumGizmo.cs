using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace ABS
{
    [RequireComponent(typeof(Camera))]
    public class FrustumGizmo : MonoBehaviour
    {
        public bool show = true;

        void OnDrawGizmos()
        {
            if (!show)
                return;

            Vector3[] nearCorners = new Vector3[4];
            Vector3[] farCorners = new Vector3[4];
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

            Plane temp = planes[1]; planes[1] = planes[2]; planes[2] = temp;
            for (int i = 0; i < 4; i++)
            {
                nearCorners[i] = Plane3Intersect(planes[4], planes[i], planes[(i + 1) % 4]);
                farCorners[i] = Plane3Intersect(planes[5], planes[i], planes[(i + 1) % 4]);
            }

            Gizmos.color = Color.white;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(nearCorners[i], nearCorners[(i + 1) % 4]);
                Gizmos.DrawLine(farCorners[i], farCorners[(i + 1) % 4]);
                Gizmos.DrawLine(nearCorners[i], farCorners[i]);
            }
        }

        private Vector3 Plane3Intersect(Plane p1, Plane p2, Plane p3)
        {
            return ((-p1.distance * Vector3.Cross(p2.normal, p3.normal)) +
                    (-p2.distance * Vector3.Cross(p3.normal, p1.normal)) +
                    (-p3.distance * Vector3.Cross(p1.normal, p2.normal))) /
             Vector3.Dot(p1.normal, Vector3.Cross(p2.normal, p3.normal));
        }
    }
}
