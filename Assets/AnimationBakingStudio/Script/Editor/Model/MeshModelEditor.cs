using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ABS
{
    [CustomEditor(typeof(MeshModel)), CanEditMultipleObjects]
    public class MeshModelEditor : ModelEditor
    {
        private MeshModel model = null;

        private UnityEditorInternal.ReorderableList animReorderableList = null;

        private int animIndex = -1;

        private MeshAnimation SelectedAnimation
        {
            get
            {
                if (model.animations.Count > animIndex && animIndex >= 0)
                    return model.animations[animIndex];
                return null;
            }
        }

        private void SetAnimationByIndex(int index)
        {
            if (index >= 0 && model.animations.Count > index)
            {
                if (animIndex != index && model.animations[index] != null)
                {
                    EditorPrefs.SetInt(model.GetInstanceID().ToString(), index);
                    animIndex = index;
                }
                animReorderableList.index = index;
            }
        }

        private int reservedAnimIndex = -1;

        private AnimatorStateMachine refStateMachine = null;

        private static Texture loopMarkTexture = null;
        private static Texture LoopMarkTexture
        {
            get
            {
                if (loopMarkTexture == null)
                    loopMarkTexture = AssetHelper.FindAsset<Texture>("GUI", "LoopMark");
                return loopMarkTexture;
            }
        }

        void OnEnable()
        {
            model = target as MeshModel;

            animReorderableList = new UnityEditorInternal.ReorderableList(serializedObject, serializedObject.FindProperty("animations"))
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Animations");
                },

                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = animReorderableList.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += 2;

                    const float LEFT_MARGIN = 50;
                    float rectWidth = rect.width - LEFT_MARGIN;
                    float popupWidth = rect.width * 0.4f;
                    const float CHECKBOX_WIDTH = 15;

                    SerializedProperty clipProperty = element.FindPropertyRelative("clip");
                    float animClipWidth = rectWidth - popupWidth - CHECKBOX_WIDTH;
                    EditorGUI.PropertyField(
                        new Rect(rect.x + LEFT_MARGIN, rect.y, animClipWidth, EditorGUIUtility.singleLineHeight),
                        clipProperty, GUIContent.none);

                    if (model.referenceController != null && refStateMachine != null && refStateMachine.states.Length > 0 && clipProperty.objectReferenceValue != null)
                    {
                        string[] stateNames = new string[refStateMachine.states.Length];
                        for (int i = 0; i < refStateMachine.states.Length; ++i)
                            stateNames[i] = refStateMachine.states[i].state.name;

                        SerializedProperty stateIndexProperty = element.FindPropertyRelative("stateIndex");
                        stateIndexProperty.intValue =
                            EditorGUI.Popup(new Rect(rect.x + LEFT_MARGIN + animClipWidth + 5, rect.y, popupWidth - CHECKBOX_WIDTH - 10, EditorGUIUtility.singleLineHeight),
                                            stateIndexProperty.intValue, stateNames);

                        SerializedProperty stateNameProperty = element.FindPropertyRelative("stateName");
                        if (stateNames.Length > stateIndexProperty.intValue)
                            stateNameProperty.stringValue = stateNames[stateIndexProperty.intValue];
                    }

                    SerializedProperty loopingProperty = element.FindPropertyRelative("isLooping");
                    loopingProperty.boolValue =
                        GUI.Toggle(new Rect(rect.x + rect.width - CHECKBOX_WIDTH - 10, rect.y, CHECKBOX_WIDTH + 10, EditorGUIUtility.singleLineHeight),
                                   loopingProperty.boolValue, LoopMarkTexture, GUI.skin.button);
                },

                onAddCallback = (UnityEditorInternal.ReorderableList l) => {
                    var index = l.serializedProperty.arraySize;
                    l.serializedProperty.arraySize++;
                    SerializedProperty element = l.serializedProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("clip").objectReferenceValue = null;
                },

                onSelectCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    SetAnimationByIndex(l.index);
                },

                onRemoveCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    int index = l.index;
                    UnityEditorInternal.ReorderableList.defaultBehaviours.DoRemoveButton(l);

                    if (l.serializedProperty.arraySize > index)
                        reservedAnimIndex = index;
                    else if (l.serializedProperty.arraySize > 0)
                        reservedAnimIndex = l.serializedProperty.arraySize - 1;
                }
            };

            animIndex = EditorPrefs.GetInt(model.GetInstanceID().ToString(), -1);
            if (model.animations.Count > 0 && animIndex < 0)
                animIndex = 0;

            SetAnimationByIndex(animIndex);
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
            Undo.RecordObject(model, "Mesh Model");

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            model.mainRenderer = EditorGUILayout.ObjectField("Main Renderer",
                model.mainRenderer, typeof(Renderer), true) as Renderer;
            bool mainRendererChanged = EditorGUI.EndChangeCheck();

            if (model.mainRenderer == null)
                model.SetMainRenderer();

            if (model.IsSkinnedModel())
            {
                EditorGUILayout.Space();

                if (mainRendererChanged)
                    model.pivotType = PivotType.Bottom;

                model.pivotType = DrawPivotTypeField(model, out bool isPivotTypeChanged);
                if (isPivotTypeChanged)
                    UpdateSceneWindow();
            }
            else
            {
                model.pivotType = PivotType.Center;
            }

            if (model.toFixToOrigin && model.toFixToGround) GUI.enabled = false;
            {
                model.isGroundPivot = DrawGroundPivotField(model, out bool isGroundPivotChanged);
                if (isGroundPivotChanged)
                    UpdateSceneWindow();
            }
            if (model.toFixToOrigin && model.toFixToGround) GUI.enabled = true;

            if (!EditorApplication.isPlaying && model.IsSkinnedModel())
            {
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();

                model.rootBone = EditorGUILayout.ObjectField("Root Bone",
                    model.rootBone, typeof(Transform), true) as Transform;

                if (model.rootBone == null) GUI.enabled = false;
                EditorGUI.indentLevel++;
                {
                    model.toFixToOrigin = DrawFixToOriginField(model, out _);
                    if (model.toFixToOrigin)
                    {
                        EditorGUI.indentLevel++;
                        if (model.isGroundPivot) GUI.enabled = false;
                        model.toFixToGround = DrawFixToGroundField(model, out _);
                        if (model.isGroundPivot) GUI.enabled = true;
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
                if (model.rootBone == null) GUI.enabled = true;

                if (EditorGUI.EndChangeCheck())
                    model.Simulate(SelectedAnimation, Frame.BEGIN);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (reservedAnimIndex >= 0)
            {
                SetAnimationByIndex(reservedAnimIndex);
                reservedAnimIndex = -1;
            }

            Rect animBoxRect = EditorGUILayout.BeginVertical();
            serializedObject.Update();
            animReorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    if (!animBoxRect.Contains(Event.current.mousePosition))
                        break;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (object draggedObj in DragAndDrop.objectReferences)
                        {
                            AnimationClip clip = draggedObj as AnimationClip;
                            if (clip != null)
                            {
                                MeshAnimation anim = new MeshAnimation();
                                anim.clip = clip;
                                model.AddAnimation(anim);
                            }
                        }
                    }
                }
                Event.current.Use();
                break;
            }

            if (model.animations.Count > 0 && DrawingHelper.DrawMiddleButton("Clear all"))
                model.animations.Clear();

            EditorGUILayout.Space();

            DrawReferenceControllerField();
            if (model.referenceController != null)
            {
                EditorGUI.indentLevel++;
                model.outputController = EditorGUILayout.ObjectField("Output Controller",
                    model.outputController, typeof(AnimatorController), false) as AnimatorController;
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            model.spritePrefab = DrawSpritePrefabField(model, out _);
            if (model.spritePrefab != null)
            {
                EditorGUI.indentLevel++;
                model.prefabBuilder = DrawPrefabBuilderField(model, out _);
                EditorGUI.indentLevel--;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.Space();

                if (SelectedAnimation != null)
                {
                    SelectedAnimation.customizer = EditorGUILayout.ObjectField("Customizer",
                        SelectedAnimation.customizer, typeof(AnimationCustomizer), true) as AnimationCustomizer;
                }
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

            MeshModel[] models = new MeshModel[targets.Length];

            for (int i = 0; i < models.Length; ++i)
                models[i] = targets[i] as MeshModel;

            MeshModel firstModel = models[0];

            bool isAllSkinnedModel = true;
            foreach (MeshModel model in models)
                isAllSkinnedModel &= model.IsSkinnedModel();

            bool isAllNotFixingToGround = true;
            foreach (MeshModel model in models)
                isAllNotFixingToGround &= (!model.toFixToOrigin || !model.toFixToGround);

            PivotType pivotType = PivotType.Bottom;
            bool isPivotTypeChanged = false;
            bool isGroundPivot = false;
            bool isGroundPivotChanged = false;

            if (isAllSkinnedModel || isAllNotFixingToGround)
            {
                EditorGUILayout.Space();

                if (isAllSkinnedModel)
                    pivotType = DrawPivotTypeField(firstModel, out isPivotTypeChanged);

                if (isAllNotFixingToGround)
                    isGroundPivot = DrawGroundPivotField(firstModel, out isGroundPivotChanged);
            }

            bool hasAllRootBone = true;
            foreach (MeshModel model in models)
                hasAllRootBone &= model.rootBone != null;

            bool isFixingToOrigin = false;
            bool isFixingToOriginChanged = false;

            bool isAllFixingToOrigin = true;
            bool isAllNotGroundPivot = true;
            bool isFixingToGround = false;
            bool isFixingToGroundChanged = false;

            if (isAllSkinnedModel && hasAllRootBone)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Root Bone");

                EditorGUI.indentLevel++;
                {
                    isFixingToOrigin = DrawFixToOriginField(firstModel, out isFixingToOriginChanged);

                    foreach (MeshModel model in models)
                        isAllFixingToOrigin &= model.toFixToOrigin;
                    foreach (MeshModel model in models)
                        isAllNotGroundPivot &= !model.isGroundPivot;

                    if (isAllFixingToOrigin && isAllNotGroundPivot)
                    {
                        EditorGUI.indentLevel++;
                        isFixingToGround = DrawFixToGroundField(firstModel, out isFixingToGroundChanged);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            GameObject spritePrefab = DrawSpritePrefabField(firstModel, out bool isSpritePrefabChanged);

            bool hasAllSpritePrefab = true;
            foreach (MeshModel model in models)
                hasAllSpritePrefab &= (model.spritePrefab != null);

            PrefabBuilder prefabBuilder = null;
            bool isPrefabBuilderChanged = false;
            if (hasAllSpritePrefab)
            {
                EditorGUI.indentLevel++;
                prefabBuilder = DrawPrefabBuilderField(model, out isPrefabBuilderChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            string nameSuffix = DrawModelNameSuffix(firstModel, out bool isNameSuffixChanged);
            if (isNameSuffixChanged)
                PathHelper.CorrectPathString(ref nameSuffix);

            if (isPivotTypeChanged || isGroundPivotChanged || isFixingToOriginChanged || isFixingToGroundChanged ||
                isSpritePrefabChanged || isPrefabBuilderChanged || isNameSuffixChanged)
            {
                foreach (MeshModel model in models)
                {
                    Undo.RecordObject(model, "Mesh Model");

                    if (isAllSkinnedModel && isPivotTypeChanged)
                        model.pivotType = pivotType;
                    if (isAllNotFixingToGround && isGroundPivotChanged)
                        model.isGroundPivot = isGroundPivot;

                    if (isAllSkinnedModel && hasAllRootBone)
                    {
                        if (isFixingToOriginChanged)
                            model.toFixToOrigin = isFixingToOrigin;
                        if (isAllFixingToOrigin && isAllNotGroundPivot && isFixingToGroundChanged)
                            model.toFixToGround = isFixingToGround;
                    }

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
                foreach (MeshModel model in models)
                {
                    AddToModelList(model);

                    if (model.mainRenderer == null)
                    {
                        model.SetMainRenderer();
                        model.pivotType = model.IsSkinnedModel() ? PivotType.Bottom : PivotType.Center;
                    }
                }
            }
        }

        private PivotType DrawPivotTypeField(MeshModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            PivotType pivotType = (PivotType)EditorGUILayout.EnumPopup("Pivot Type", model.pivotType);
            isChanged = EditorGUI.EndChangeCheck();

            return pivotType;
        }

        private void UpdateSceneWindow()
        {
            EditorWindow sceneWindow = EditorWindow.GetWindow<SceneView>();
            sceneWindow.Repaint();
        }

        private bool DrawFixToOriginField(MeshModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isFixingToOrigin = EditorGUILayout.Toggle("Fix to Origin", model.toFixToOrigin);
            isChanged = EditorGUI.EndChangeCheck();

            return isFixingToOrigin;
        }

        private bool DrawFixToGroundField(MeshModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isFixingToGround = EditorGUILayout.Toggle("Fix to Ground", model.toFixToGround);
            isChanged = EditorGUI.EndChangeCheck();

            return isFixingToGround;
        }

        private void DrawReferenceControllerField()
        {
            model.referenceController = EditorGUILayout.ObjectField("Reference Controller",
                model.referenceController, typeof(AnimatorController), false) as AnimatorController;

            if (model.referenceController == null)
            {
                Animator animator = model.GetComponentInChildren<Animator>();
                if (animator != null)
                    model.referenceController = animator.runtimeAnimatorController as AnimatorController;
            }

            if (model.referenceController != null)
            {
                if (model.referenceController.layers.Length > 1)
                {
                    Debug.LogError("Reference controller in which has layers more than 1 is not supported.");
                    model.referenceController = null;
                }
                else if (model.referenceController.layers[0].stateMachine.stateMachines.Length > 0)
                {
                    Debug.LogError("Reference controller in which has any sub machine is not supported.");
                    model.referenceController = null;
                }

                refStateMachine = model.referenceController.layers[0].stateMachine;
                if (refStateMachine.states.Length == 0)
                {
                    Debug.LogError("Reference controller in which has no state is not supported.");
                    model.referenceController = null;
                }
            }
        }
    }
}
