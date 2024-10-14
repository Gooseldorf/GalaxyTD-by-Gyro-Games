using System;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public class ParticleModel : Model
    {
        public ParticleSystem mainParticleSystem;
        public float duration = 2;

        public bool isProjectile = false;
        public Vector3 projectileVector = Vector3.forward;

        public bool isLooping = false;

        public bool isSizeChecked;

        public Vector3 maxSize = Vector3.one;
        public Vector3 minPos = Vector3.zero;
        public Vector3 maxPos = Vector3.zero;

        public void SetMainParticleSystem()
        {
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
            if (particleSystems.Length > 0)
            {
                mainParticleSystem = particleSystems[0];
                duration = mainParticleSystem.main.duration;
            }
        }

        public void CheckSizeAndBounds()
        {
            if (isSizeChecked)
                return;
            isSizeChecked = true;

            const int NUMBER_OF_FRAMES = 10;
            for (int i = 0; i < NUMBER_OF_FRAMES; ++i)
            {
                Simulate(i == 0, NUMBER_OF_FRAMES);

                ParticleSystemRenderer[] particleSystemRenderers = mainParticleSystem.GetComponentsInChildren<ParticleSystemRenderer>();
                foreach (ParticleSystemRenderer renderer in particleSystemRenderers)
                {
                    if (maxSize.magnitude < renderer.bounds.size.magnitude)
                    {
                        Vector3 targetSize = renderer.bounds.size;
                        maxSize = new Vector3(targetSize.x, targetSize.y, targetSize.z);
                    }

                    minPos = new Vector3
                    (
                        Mathf.Min(minPos.x, renderer.bounds.min.x),
                        Mathf.Min(minPos.y, renderer.bounds.min.y),
                        Mathf.Min(minPos.z, renderer.bounds.min.z)
                    );

                    maxPos = new Vector3
                    (
                        Mathf.Max(maxPos.x, renderer.bounds.max.x),
                        Mathf.Max(maxPos.y, renderer.bounds.max.y),
                        Mathf.Max(maxPos.z, renderer.bounds.max.z)
                    );
                }
            }
        }

        public override Vector3 GetSize()
        {
            return maxSize;
        }

        public override Vector3 GetMinPos()
        {
            return minPos;
        }

        public override Vector3 GetMaxPos()
        {
            return maxPos;
        }

        public override bool IsReady()
        {
            return mainParticleSystem != null && isSizeChecked;
        }

        public override bool IsTileAvailable()
        {
            return false;
        }

        public void Simulate(bool isFirst, int numOfFrames)
        {
            if (mainParticleSystem == null)
                return;

            if (isFirst)
            {
                mainParticleSystem.Simulate(0, true, true);
            }
            else
            {
                float unitTime = duration / numOfFrames;
                mainParticleSystem.Simulate(unitTime, true, false);
            }
        }

        public void StopAndPlay()
        {
            var particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(false);
            }

            var runtimeInitializer = GetComponent<RuntimeInitializer>();
            if (runtimeInitializer != null)
                runtimeInitializer.Initialize();

        }
    }
}
