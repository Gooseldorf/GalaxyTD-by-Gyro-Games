using CardTD.Utilities;
using ECSTest.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Visual
{
    public class LaserProjectileVisual : ProjectileVisual
    {
        [SerializeField] private List<LineRenderer> lines = new();

        private float showTime = .1f;
        public Action Realize;

        private float3 startPosition;

        private bool isActive;

        private Entity projectile;
        
        public void SetStartPosition(float3 position,float showTime,Entity projectile)
        {
            this.projectile = projectile;
            this.showTime = showTime;
            startPosition = position;
            lines[0].SetPosition(0,startPosition);
            lines[0].SetPosition(1,startPosition);
            lines[0].gameObject.SetActive(true);
            gameObject.SetActive(true);
            StartCoroutine(HideVisual());
            isActive = true;
        }

        private IEnumerator HideVisual()
        {
            yield return new WaitForSeconds(showTime);
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i].SetPosition(0,startPosition);
                lines[i].SetPosition(0,startPosition);
                lines[i].gameObject.SetActive(false);
            }
            isActive = false;
        }

        private void FixedUpdate()
        {
            if(isActive)
                return;
            
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            if(manager.Exists(projectile))
                return;
            
            Realize?.Invoke();
        }

        public void UpdatePosition(NativeArray<Float2Buffer> points,float3 position)
        {
            if(!isActive)
                return;
            NativeList<float3> allPoints = new (Allocator.Temp) {startPosition};

            foreach (Float2Buffer point in points)
                allPoints.Add(((float2)point).ToFloat3());

            allPoints.Add(position);
            
            if (allPoints.Length >= lines.Count)
            {
                LineRenderer lr = Instantiate(lines[0], this.transform, true);
                lr.gameObject.SetActive(false);
                lines.Add(lr);
            }

            for (int i=0;i<allPoints.Length-1;i++)
            {
                lines[i].SetPosition(0,allPoints[i]);
                lines[i].SetPosition(1,allPoints[i+1]);
                lines[i].gameObject.SetActive(true);
            }
        }
    }
}