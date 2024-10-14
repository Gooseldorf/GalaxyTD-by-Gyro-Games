using UnityEngine;
using UnityEditor;

namespace ABS
{
    [CustomEditor(typeof(ParticleModel)), CanEditMultipleObjects]
    public class ParticleModelEditor : ModelEditor
    {
        private ParticleModel model = null;

        void OnEnable()
        {
            model = target as ParticleModel;
        }

        public override void OnInspectorGUI()
        {
            GUI.changed = false;

            if (targets != null && targets.Length > 1)
                OnInspectorGUI_Multi();
            else if (model != null)
                OnInspectorGUI_Single();
        }

        private void OnInspectorGUI_Single()
        {
            Undo.RecordObject(model, "Particle Model");

            EditorGUI.BeginChangeCheck();
            model.mainParticleSystem = EditorGUILayout.ObjectField("Main Particle System",
                model.mainParticleSystem, typeof(ParticleSystem), true) as ParticleSystem;
            if (EditorGUI.EndChangeCheck())
                model.isSizeChecked = false;

            if (model.mainParticleSystem == null)
                model.SetMainParticleSystem();

            if (model.mainParticleSystem != null)
                model.CheckSizeAndBounds();

            model.duration = DrawDurationField(model, out _);

            EditorGUILayout.Space();

            model.isGroundPivot = DrawGroundPivotField(model, out _);

            EditorGUILayout.Space();

            model.isProjectile = DrawProjectileField(model, out _);
            if (model.isProjectile)
            {
                EditorGUI.indentLevel++;
                model.projectileVector = DrawProjectileVectorField(model, out _);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            model.isLooping = DrawLoopingField(model, out _);

            EditorGUILayout.Space();

            model.spritePrefab = DrawSpritePrefabField(model, out _);
            if (model.spritePrefab != null)
            {
                EditorGUI.indentLevel++;
                model.prefabBuilder = DrawPrefabBuilderField(model, out _);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            model.nameSuffix = DrawModelNameSuffix(model, out bool isNameSuffixChanged);
            if (isNameSuffixChanged)
                PathHelper.CorrectPathString(ref model.nameSuffix);

            EditorGUILayout.Space();

            if (DrawingHelper.DrawWideButton("Add to the model list"))
                AddToModelList(model);
        }

        protected void OnInspectorGUI_Multi()
        {
            EditorGUILayout.HelpBox("Displayed information is of the first selected model,\nbut any change affects all selected models.", MessageType.Info);

            ParticleModel[] models = new ParticleModel[targets.Length];

            for (int i = 0; i < models.Length; ++i)
                models[i] = targets[i] as ParticleModel;

            ParticleModel firstModel = models[0];

            float duration = DrawDurationField(firstModel, out bool isDurationChanged);

            EditorGUILayout.Space();

            bool isGroundPivot = DrawGroundPivotField(firstModel, out bool isGroundPivotChanged);

            EditorGUILayout.Space();

            bool isProjectile = DrawProjectileField(firstModel, out bool isProjectileChanged);

            bool isAllProjectile = true;
            foreach (Model model in models)
                isAllProjectile &= firstModel.isProjectile;

            Vector3 projectileVector = Vector3.forward;
            bool isProjectileVectorChanged = false;
            if (isAllProjectile)
            {
                EditorGUI.indentLevel++;
                projectileVector = DrawProjectileVectorField(firstModel, out isProjectileVectorChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            bool isLooping = DrawLoopingField(firstModel, out bool isLoopingChanged);

            bool isAllLooping = true;
            foreach (ParticleModel model in models)
                isAllLooping &= model.isLooping;

            EditorGUILayout.Space();

            GameObject spritePrefab = DrawSpritePrefabField(firstModel, out bool isSpritePrefabChanged);

            bool hasAllSpritePrefab = true;
            foreach (ParticleModel model in models)
                hasAllSpritePrefab &= (model.spritePrefab != null);

            PrefabBuilder prefabBuilder = null;
            bool isPrefabBuilderChanged = false;
            if (hasAllSpritePrefab)
            {
                EditorGUI.indentLevel++;
                prefabBuilder = DrawPrefabBuilderField(firstModel, out isPrefabBuilderChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            string nameSuffix = DrawModelNameSuffix(firstModel, out bool isNameSuffixChanged);
            if (isNameSuffixChanged)
                PathHelper.CorrectPathString(ref nameSuffix);

            if (isDurationChanged || isGroundPivotChanged || isProjectileChanged || isProjectileVectorChanged ||
                isLoopingChanged || isSpritePrefabChanged || isPrefabBuilderChanged || isNameSuffixChanged)
            {
                foreach (ParticleModel model in models)
                {
                    Undo.RecordObject(model, "Particle Model");
                    if (isDurationChanged)
                        model.duration = duration;
                    if (isGroundPivotChanged)
                        model.isGroundPivot = isGroundPivot;
                    if (isProjectileChanged)
                        model.isProjectile = isProjectile;
                    if (isProjectileVectorChanged)
                        model.projectileVector = projectileVector;
                    if (isLoopingChanged)
                        model.isLooping = isLooping;
                    if (isSpritePrefabChanged)
                        model.spritePrefab = spritePrefab;
                    if (hasAllSpritePrefab && isPrefabBuilderChanged)
                        model.prefabBuilder = prefabBuilder;
                    if (isNameSuffixChanged)
                        model.nameSuffix = nameSuffix;
                }
            }

            Studio studio = FindObjectOfType<Studio>();
            if (studio == null)
                return;

            EditorGUILayout.Space();

            if (DrawingHelper.DrawWideButton("Add all to the model list"))
            {
                foreach (ParticleModel model in models)
                {
                    AddToModelList(model);

                    if (model.mainParticleSystem == null)
                        model.SetMainParticleSystem();
                    if (model.mainParticleSystem != null)
                        model.CheckSizeAndBounds();
                }
            }
        }

        private float DrawDurationField(ParticleModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            float duration = EditorGUILayout.FloatField("Duration", model.duration);
            isChanged = EditorGUI.EndChangeCheck();

            return duration;
        }

        private bool DrawProjectileField(ParticleModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isProjectile = EditorGUILayout.Toggle("Projectile", model.isProjectile);
            isChanged = EditorGUI.EndChangeCheck();

            return isProjectile;
        }

        private Vector3 DrawProjectileVectorField(ParticleModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 projectileVector = EditorGUILayout.Vector3Field("Speed Vector", model.projectileVector);
            isChanged = EditorGUI.EndChangeCheck();

            return projectileVector;
        }

        private bool DrawLoopingField(ParticleModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isLooping = EditorGUILayout.Toggle("Looping", model.isLooping);
            isChanged = EditorGUI.EndChangeCheck();

            return isLooping;
        }
    }
}
