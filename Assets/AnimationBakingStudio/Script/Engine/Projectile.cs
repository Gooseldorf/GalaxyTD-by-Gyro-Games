using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public class Projectile : MonoBehaviour
    {
        private Vector3 vecFromCameraToModel;

        public Vector3 MovingVector { get; set; }

        void Start()
        {
            vecFromCameraToModel = transform.position - Camera.main.transform.position;
        }

        void Update()
        {
            transform.Translate(MovingVector, Space.Self);
            Camera.main.transform.position = transform.position - vecFromCameraToModel;
        }
    }
}
