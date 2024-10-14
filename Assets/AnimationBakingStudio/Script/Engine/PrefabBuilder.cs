using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace ABS
{
	public class PrefabBuilder : MonoBehaviour
	{
#if UNITY_EDITOR
		public static EditorCurveBinding GetSpriteCurveBinding()
		{
			return EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
		}

		public virtual void BindController(GameObject rootObject, AnimatorController controller)
		{
			Animator animator = rootObject.GetComponent<Animator>();
			if (animator != null)
				animator.runtimeAnimatorController = controller;
		}

		public static EditorCurveBinding GetMaterialCurveBinding()
		{
			return EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Materials.Array.data[0]");
		}
#endif

		public virtual void BindFirstSprite(GameObject rootObject, Sprite firstSprite)
		{
			SpriteRenderer renderer = rootObject.GetComponent<SpriteRenderer>();
			if (renderer != null)
				renderer.sprite = firstSprite;
		}

		public virtual void BindFirstMaterial(GameObject rootObject, Material firstMaterial)
		{
			SpriteRenderer renderer = rootObject.GetComponent<SpriteRenderer>();
			if (renderer != null)
				renderer.sharedMaterial = firstMaterial;
		}

		public virtual Transform GetLocationsParent(GameObject rootObject)
		{
			return rootObject.transform;
		}

		public virtual BoxCollider2D GetBoxCollider2D(GameObject rootObject)
		{
			return rootObject.GetComponentInChildren<BoxCollider2D>();
		}
	}
}
