using UnityEngine;
using UnityEditor;

namespace ABS
{
    public class ModelEditor : Editor
    {
        protected bool DrawGroundPivotField(Model model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isGroundPivot = EditorGUILayout.Toggle("Ground Pivot", model.isGroundPivot);
            isChanged = EditorGUI.EndChangeCheck();

            return isGroundPivot;
        }

        protected GameObject DrawSpritePrefabField(Model model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            GameObject spritePrefab = EditorGUILayout.ObjectField("Sprite Prefab",
                model.spritePrefab, typeof(GameObject), false) as GameObject;
            isChanged = EditorGUI.EndChangeCheck();

            return spritePrefab;
        }

        protected PrefabBuilder DrawPrefabBuilderField(Model model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            PrefabBuilder prefabBuilder = EditorGUILayout.ObjectField("Prefab Builder",
                model.prefabBuilder, typeof(PrefabBuilder), false) as PrefabBuilder;
            isChanged = EditorGUI.EndChangeCheck();

            return prefabBuilder;
        }

        protected string DrawModelNameSuffix(Model model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            string nameSuffix = EditorGUILayout.TextField("Name Suffix", model.nameSuffix);
            isChanged = EditorGUI.EndChangeCheck();

            return nameSuffix;
        }

        protected void AddToModelList(Model model)
        {
            Studio studio = FindObjectOfType<Studio>();
            if (studio == null)
                return;

            studio.AddModel(model);
        }
    }
}
