using System;
using UnityEngine;

namespace ABS
{
	[Serializable]
	public class OutputProperty : PropertyBase
	{
		public bool toMakeAnimationClip = true;
		public int frameRate = 20;
		public int frameInterval = 1;
		public bool toMakeAnimatorController = false;
		public bool toMakePrefab = false;
		public bool isCompactCollider = false;
		public bool toMakeLocationPrefab = false;
		public GameObject locationSpritePrefab;
		public bool toMakeNormalMap = false;
		public bool toMakeMaterial = false;
		public Material spriteMaterial = null;
		public MaterialBuilder materialBuilder = null;

		public bool IsToMakeLocationPrefab()
        {
			return toMakeLocationPrefab && locationSpritePrefab != null;
		}
	}
}
