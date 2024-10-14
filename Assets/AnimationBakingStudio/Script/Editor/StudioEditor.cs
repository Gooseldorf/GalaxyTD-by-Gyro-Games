using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace ABS
{
    [CustomEditor(typeof(Studio))]
    public class StudioEditor : Editor
    {
        private Studio studio = null;

        private ReorderableList modelReorderableList = null;

        private const string MODEL_INDEX_KEY = "ModelIndex";
        private int modelIndex = -1;
        private bool modelChanged = false;

        private Model SelectedModel
        {
            get
            {
                if (studio.model.list.Count > modelIndex && modelIndex >= 0)
                    return studio.model.list[modelIndex];
                return null;
            }
        }

        private int reservedModelIndex = -1;

        private MeshAnimation selectedAnimation = null;

        private float CurrentTurnAngle
        {
            get
            {
                return studio.view.baseTurnAngle + studio.appliedSubViewTurnAngle;
            }
        }

        private bool shadowWithoutModel = false;

        private Sampler sampler = null;

        private Batcher batcher = null;
        private readonly List<Model> bakingModels = new List<Model>();

        private CameraClearFlags cameraClearFlagsBackup = CameraClearFlags.SolidColor;
        private Color cameraBackgroundColorBackup = lightGreenColor;

        private object hdrpCameraClearColorModeBackup;
        private object hdrpCameraBackgroundColorHdrBackup;

        private readonly Dictionary<Model, bool> modelActivationBackup = new Dictionary<Model, bool>();

        private Texture2D previewTexture = null;

        private Texture arrowDownTexture = null;
        private Texture ArrowDownTexture
        {
            get
            {
                if (arrowDownTexture == null)
                    arrowDownTexture = AssetHelper.FindAsset<Texture>(EditorConstants.GUI_FOLDER_NAME,
                        "ArrowDown" + (EditorGUIUtility.isProSkin ? "_pro" : ""));
                return arrowDownTexture;
            }
        }

        private Texture arrowRightTexture = null;
        private Texture ArrowRightTexture
        {
            get
            {
                if (arrowRightTexture == null)
                    arrowRightTexture = AssetHelper.FindAsset<Texture>(EditorConstants.GUI_FOLDER_NAME,
                        "ArrowRight" + (EditorGUIUtility.isProSkin ? "_pro" : ""));
                return arrowRightTexture;
            }
        }

        private int editorY;
        private int editorWidth;
        private int editorHeight;

        private static readonly Color lightGreenColor = new Color32(0, 200, 0, 255);
        private static readonly Color darkGreenColor = new Color32(0, 50, 0, 255);

        private void SetModelByIndex(int index)
        {
            if (index >= 0 && studio.model.list.Count > index)
            {
                if (modelIndex != index && studio.model.list[index] != null)
                {
                    EditorPrefs.SetInt(MODEL_INDEX_KEY, index);
                    modelIndex = index;
                    modelChanged = true;
                    studio.sampling = null;
                    studio.filming.simulatedIndex = 0;
                    selectedAnimation = null;
                }
                modelReorderableList.index = index;

                CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
            }
        }

        void OnEnable()
        {
            studio = target as Studio;

            modelReorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("model.list"))
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Models");
                },

                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = modelReorderableList.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(
                        new Rect(rect.x + 100, rect.y, rect.width - 100, EditorGUIUtility.singleLineHeight),
                        element, GUIContent.none);
                    modelChanged = EditorGUI.EndChangeCheck();

                    if (modelChanged)
                    {
                        //Model model = element.objectReferenceValue as Model;
                        studio.sampling = null;
                        studio.filming.simulatedIndex = 0;
                    }

                    if (modelChanged && index == modelIndex)
                        CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
                },

                onAddCallback = (ReorderableList l) => {
                    var index = l.serializedProperty.arraySize;
                    l.serializedProperty.arraySize++;
                    SerializedProperty element = l.serializedProperty.GetArrayElementAtIndex(index);
                    element.objectReferenceValue = null;
                },

                onSelectCallback = (ReorderableList l) =>
                {
                    SetModelByIndex(l.index);
                },

                onRemoveCallback = (ReorderableList l) =>
                {
                    int index = l.index;
                    SerializedProperty element = l.serializedProperty.GetArrayElementAtIndex(index);
                    if (element.objectReferenceValue != null)
                        l.serializedProperty.DeleteArrayElementAtIndex(index);
                    l.serializedProperty.DeleteArrayElementAtIndex(index);

                    if (l.serializedProperty.arraySize > index)
                        reservedModelIndex = index;
                    else if (l.serializedProperty.arraySize > 0)
                        reservedModelIndex = l.serializedProperty.arraySize - 1;

                    studio.sampling = null;
                }
            };

            modelIndex = EditorPrefs.GetInt(MODEL_INDEX_KEY, -1);
            if (studio.model.list.Count > 0 && modelIndex < 0)
                modelIndex = 0;

            SetModelByIndex(modelIndex);

            ConfigurationWindow.UpdateGlobalVariables();
        }

        void OnDisable()
        {
            if (sampler != null)
                EditorApplication.update -= sampler.UpdateState;

            if (batcher != null)
                EditorApplication.update -= batcher.UpdateState;
        }

        public override void OnInspectorGUI()
        {
            if (studio == null)
                return;

            Rect rect = EditorGUILayout.BeginVertical(); EditorGUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                //editorY = (int)rect.y;
                editorY = 200;
                editorWidth = (int)rect.width;
            }

            int screenHeight = Screen.height;
#if UNITY_EDITOR_OSX
            screenHeight /= 2; // scale factor
#endif
            editorHeight = screenHeight - editorY - 100;

            if (!EditorApplication.isPlaying)
            {
                if (sampler != null || batcher != null)
                {
                    ProgressDrawer.DrawCapturingProgress(editorWidth, editorHeight, bakingModels, sampler, batcher);
                    return;
                }
            }

            GUI.changed = false;

            Undo.RecordObject(studio, "Studio");
            if (SelectedModel != null)
                Undo.RecordObject(SelectedModel, "Selected Model");

            studio.isSamplingReady = true;
            studio.isBakingReady = true;

            //if (EditorApplication.isPlaying)
            //{
            //    if (DrawingHelper.DrawWideButton("Exit Play Mode"))
            //        EditorApplication.ExitPlaymode();
            //}
            //else
            //{
            //    if (DrawingHelper.DrawWideButton("Enter Play Mode"))
            //        EditorApplication.EnterPlaymode();
            //}

            //EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck(); // check any changes
            {
                DrawModelFields();

                bool hasPreparedModel = false;
                for (int i = 0; i < studio.model.list.Count; ++i)
                {
                    Model model = studio.model.list[i];
                    if (model != null)
                    {
                        if (studio.view.rotationType == RotationType.Camera)
                        {
                            if (Model.IsMeshModel(model))
                                Model.AsMeshModel(model).currentAngle = 0;
                            model.transform.rotation = Quaternion.identity;
                        }

                        if (model.IsReady())
                        {
                            hasPreparedModel = true;
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(string.Format("{0} at index {1} will not be baked because it is not ready.",
                                model.name, i), MessageType.Warning);
                            studio.model.opened = true;
                        }
                    }
                }
                if (!hasPreparedModel)
                {
                    EditorGUILayout.HelpBox("No prepared model to capture!", MessageType.Error);
                    studio.isSamplingReady = false;
                    studio.isBakingReady = false;
                    studio.model.opened = true;
                }

                if (studio.model.list.Count > 0 && SelectedModel == null)
                    SetModelByIndex(0);

                EditorGUILayout.Space();

                DrawCameraFields();

                if (Camera.main == null)
                {
                    studio.isSamplingReady = false;
                    studio.isBakingReady = false;
                    studio.cam.opened = true;
                }

                EditorGUILayout.Space();

                DrawLightFields();

                EditorGUILayout.Space();

                DrawViewFields();

                if (studio.view.checkedSubViews.Count == 0)
                {
                    EditorGUILayout.HelpBox("No selected view!", MessageType.Error);
                    studio.isBakingReady = false;
                    studio.view.opened = true;
                }

                if (Model.IsMeshModel(SelectedModel))
                {
                    EditorGUILayout.Space();
                    DrawShadowFields();
                }

                EditorGUILayout.Space();

                DrawExtractionFields();

                if (studio.extraction.com == null)
                {
                    EditorGUILayout.HelpBox("No extractor!", MessageType.Error);
                    studio.isSamplingReady = false;
                    studio.isBakingReady = false;
                    studio.extraction.opened = true;
                }

                EditorGUILayout.Space();

                DrawPreviewFields();

                EditorGUILayout.Space();

                DrawFilmingFields();

                EditorGUILayout.Space();

                if (studio.isSamplingReady)
                    DrawSamplingFields();

                EditorGUILayout.Space();

                DrawTrimmingFields();

                EditorGUILayout.Space();

                DrawPackingFields();

                EditorGUILayout.Space();

                DrawOutputFields();

                EditorGUILayout.Space();
            }

            if (studio.preview.on)
            {
                if (EditorGUI.EndChangeCheck() | modelChanged || previewTexture == null)
                    UpdatePreviewTexture();
            }

            DrawDirectoryFields();

            if (studio.dir.exportPath == null || studio.dir.exportPath.Length == 0 || !Directory.Exists(studio.dir.exportPath))
            {
                EditorGUILayout.HelpBox("Invalid directory!", MessageType.Error);
                studio.isBakingReady = false;
                studio.dir.opened = true;
            }
            else
            {
                if (studio.dir.exportPath.IndexOf(Application.dataPath) < 0)
                {
                    EditorGUILayout.HelpBox(string.Format("{0} is out of Assets folder.", studio.dir.exportPath), MessageType.Error);
                    studio.isBakingReady = false;
                    studio.dir.opened = true;
                }
            }

            EditorGUILayout.Space();

            DrawNamingFields();

            EditorGUILayout.Space();

            if (studio.isBakingReady)
                DrawBakingFields();

            modelChanged = false;
        }

        private void DrawModelFields()
        {
            if (!DrawGroupOrPass("Model", ref studio.model.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();

            if (reservedModelIndex >= 0)
            {
                SetModelByIndex(reservedModelIndex);
                reservedModelIndex = -1;
            }

            Rect modelBoxRect = EditorGUILayout.BeginVertical();
            serializedObject.Update();
            modelReorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        if (!modelBoxRect.Contains(Event.current.mousePosition))
                            break;
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (Event.current.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            foreach (object draggedObj in DragAndDrop.objectReferences)
                            {
                                GameObject go = draggedObj as GameObject;
                                if (go == null)
                                    continue;

                                Model model = go.GetComponent<Model>();
                                if (model != null)
                                    studio.AddModel(model);
                            }
                        }
                    }
                    Event.current.Use();
                    break;
            }

            if (studio.model.list.Count > 0 && DrawingHelper.DrawMiddleButton("Clear all"))
                studio.model.list.Clear();

            if (SelectedModel != null && Model.IsMeshModel(SelectedModel))
            {
                MeshModel meshModel = Model.AsMeshModel(SelectedModel);

                List <MeshAnimation> validAnimations = meshModel.GetValidAnimations();
                if (validAnimations.Count > 0)
                {
                    string[] popupStrings = new string[validAnimations.Count];
                    for (int i = 0; i < validAnimations.Count; ++i)
                    {
                        MeshAnimation anim = validAnimations[i];
                        string stateName = (meshModel.referenceController != null) ? anim.stateName : anim.clip.name;
                        popupStrings[i] = stateName;
                    }

                    if (meshModel.animIndex < 0 || meshModel.animIndex >= popupStrings.Length)
                        meshModel.animIndex = 0;

                    EditorGUI.BeginChangeCheck();
                    meshModel.animIndex = EditorGUILayout.Popup("Animation", meshModel.animIndex, popupStrings);
                    if (EditorGUI.EndChangeCheck())
                    {
                        studio.sampling = null;
                        studio.filming.simulatedIndex = 0;
                    }
                    selectedAnimation = validAnimations[meshModel.animIndex];
                }
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawCameraFields()
        {
            if (!DrawGroupOrPass("Camera", ref studio.cam.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            if (Camera.main != null)
            {
                EditorGUILayout.LabelField("Main Camera Exists.", EditorStyles.boldLabel);
            }
            else
            {
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = Color.red;
                labelStyle.fontStyle = FontStyle.Bold;
                EditorGUILayout.LabelField("No Main Camera!", labelStyle);

                if (DrawingHelper.DrawMiddleButton("Create a Main Camera"))
                {
                    ObjectHelper.GetOrCreateObject("Main Camera", "Prefab", new Vector3(0, 100, 0));
                    CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
                }
            }

            bool isParticleModel = Model.IsParticleModel(SelectedModel);

            if (Camera.main != null)
            {
                string[] texts = new string[2] { "Orthographic", "Perspective" };

                CameraMode mode = isParticleModel ? CameraMode.Orthographic : studio.cam.mode;

                if (isParticleModel) GUI.enabled = false;
                mode = (CameraMode)GUILayout.Toolbar((int)mode, texts);
                if (isParticleModel) GUI.enabled = true;

                if (!isParticleModel)
                    studio.cam.mode = mode;

                Camera.main.orthographic = mode == CameraMode.Orthographic;

                EditorGUI.indentLevel++;
                if (Camera.main.orthographic)
                {
                    if (SelectedModel != null && SelectedModel.toUseMyCamSize)
                    {
                        SelectedModel.cameraOrthoSize = EditorGUILayout.FloatField("Orthographic Size", SelectedModel.cameraOrthoSize);
                        Camera.main.orthographicSize = SelectedModel.cameraOrthoSize;
                    }
                    else
                    {
                        studio.cam.orthographicSize = EditorGUILayout.FloatField("Orthographic Size", studio.cam.orthographicSize);
                        Camera.main.orthographicSize = studio.cam.orthographicSize;
                    }
                }
                else
                {
                    if (SelectedModel != null && SelectedModel.toUseMyCamSize)
                    {
                        SelectedModel.cameraFOV = EditorGUILayout.FloatField("Field of View", SelectedModel.cameraFOV);
                        Camera.main.fieldOfView = SelectedModel.cameraFOV;
                    }
                    else
                    {
                        studio.cam.fieldOfView = EditorGUILayout.FloatField("Field of View", studio.cam.fieldOfView);
                        Camera.main.fieldOfView = studio.cam.fieldOfView;
                    }
                }

                if (SelectedModel != null)
                {
                    EditorGUI.indentLevel++;
                    SelectedModel.toUseMyCamSize = EditorGUILayout.Toggle("Model-Specific", SelectedModel.toUseMyCamSize);
                    EditorGUI.indentLevel--;

                    EditorGUI.BeginChangeCheck();
                    SelectedModel.cameraOffset = EditorGUILayout.Vector3Field("Model Offset", SelectedModel.cameraOffset);
                    if (EditorGUI.EndChangeCheck())
                        CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
                }
                EditorGUI.indentLevel--;
            }

            if (SelectedModel != null && !isParticleModel)
            {
                EditorGUI.BeginChangeCheck();

                string[] texts = new string[2] { "Relative Distance", "Absolute Distance" };
                studio.cam.distanceType = (DistanceType)GUILayout.Toolbar((int)studio.cam.distanceType, texts);

                EditorGUI.indentLevel++;
                {
                    if (studio.cam.distanceType == DistanceType.Relative)
                        studio.cam.relativeDistance = EditorGUILayout.FloatField("Distance", studio.cam.relativeDistance);
                    else if (studio.cam.distanceType == DistanceType.Absolute)
                        studio.cam.absoluteDistance = EditorGUILayout.FloatField("Distance", studio.cam.absoluteDistance);
                }
                EditorGUI.indentLevel--;

                if (EditorGUI.EndChangeCheck())
                    CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawLightFields()
        {
            if (!DrawGroupOrPass("Light", ref studio.lit.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            studio.lit.com = EditorGUILayout.ObjectField("Directional Light", studio.lit.com, typeof(Light), true) as Light;
            if (studio.lit.com == null)
            {
                GameObject lightObj = GameObject.Find("Directional Light");
                if (lightObj == null)
                    lightObj = GameObject.Find("Directional light");
                if (lightObj != null)
                    studio.lit.com = lightObj.GetComponent<Light>();
            }

            if (studio.lit.com != null)
            {
                Transform lightT = studio.lit.com.transform;

                if (!studio.lit.cameraRotationFollow || studio.shadow.type == ShadowType.Matte)
                {
                    EditorGUI.indentLevel++;
                    if (studio.lit.cameraRotationFollow) GUI.enabled = false;
                    {
                        studio.lit.slopeAngle = EditorGUILayout.FloatField("Slope Angle (10 ~ 90)", studio.lit.slopeAngle);
                        studio.lit.slopeAngle = Mathf.Clamp(studio.lit.slopeAngle, 10f, 90f);
                        studio.lit.turnAngle = EditorGUILayout.FloatField("Turn Angle", studio.lit.turnAngle);
                        float turnAngle = studio.lit.turnAngle + 180f;
                        if (turnAngle > 360f)
                            turnAngle %= 360f;
                        lightT.rotation = Quaternion.Euler(studio.lit.slopeAngle, turnAngle, 0);

                        if (DrawingHelper.DrawMiddleButton("Look at Model"))
                        {
                            CameraHelper.LookAtModel(lightT, SelectedModel);
                            Vector3 camEulerAngles = lightT.rotation.eulerAngles;
                            studio.lit.slopeAngle = camEulerAngles.x;
                            studio.lit.turnAngle = camEulerAngles.y;
                        }
                    }
                    if (studio.lit.cameraRotationFollow) GUI.enabled = true;
                    EditorGUI.indentLevel--;
                }

                studio.lit.cameraRotationFollow = EditorGUILayout.Toggle("Follow Camera Rotation", studio.lit.cameraRotationFollow);
                if (studio.lit.cameraRotationFollow)
                {
                    if (Camera.main != null)
                        lightT.rotation = Camera.main.transform.rotation;
                }

                studio.lit.cameraPositionFollow = EditorGUILayout.Toggle("Follow Camera Position", studio.lit.cameraPositionFollow);
                if (studio.lit.cameraPositionFollow)
                {
                    if (Camera.main != null && studio.lit.com != null)
                        lightT.position = Camera.main.transform.position;
                }
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawViewFields()
        {
            bool groupOpened = DrawGroupOrPass("View", ref studio.view.opened);

            bool rotationTypeChanged = false;
            bool baseTurnAngleChanged = false;

            if (groupOpened)
            {
                GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

                EditorGUI.BeginChangeCheck();
                {
                    string[] texts = new string[2] { "Camera Rotation", "Model Rotation" };
                    studio.view.rotationType = (RotationType)GUILayout.Toolbar((int)studio.view.rotationType, texts);
                }
                rotationTypeChanged = EditorGUI.EndChangeCheck();
                if (rotationTypeChanged)
                {
                    if (studio.view.rotationType == RotationType.Camera)
                        RotateAllModels(0);
                    else if (studio.view.rotationType == RotationType.Model)
                        CameraHelper.LocateMainCameraToModel(SelectedModel, studio);
                }

                EditorGUI.BeginChangeCheck();
                studio.view.slopeAngle = EditorGUILayout.FloatField("Slope Angle (0 ~ 90)", studio.view.slopeAngle);
                studio.view.slopeAngle = Mathf.Clamp(studio.view.slopeAngle, 0f, 90f);
                bool slopeAngleChanged = EditorGUI.EndChangeCheck();

                if (studio.IsSideView())
                    studio.view.isTileVisible = false;
                else
                    DrawTileFields(ref slopeAngleChanged);

                if (slopeAngleChanged)
                    CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);

                EditorGUI.BeginChangeCheck();
                studio.view.size = EditorGUILayout.IntField("View Size", studio.view.size);
                if (studio.view.size < 1)
                    studio.view.size = 1;
                bool viewSizeChanged = EditorGUI.EndChangeCheck();

                if (viewSizeChanged || studio.view.size != studio.view.subViewToggles.Length)
                {
                    SubViewToggle[] oldSubViewToggles = (SubViewToggle[])studio.view.subViewToggles.Clone();
                    studio.view.subViewToggles = new SubViewToggle[studio.view.size];
                    for (int i = 0; i < studio.view.subViewToggles.Length; ++i)
                        studio.view.subViewToggles[i] = new SubViewToggle(false);
                    MigrateViews(oldSubViewToggles);
                }

                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                studio.view.unitTurnAngle = 360f / studio.view.size;
                string label = string.Format("Base Angle (0 ~ {0})", (int)studio.view.unitTurnAngle);
                studio.view.baseTurnAngle = EditorGUILayout.FloatField(label, studio.view.baseTurnAngle);
                studio.view.baseTurnAngle = Mathf.Clamp(studio.view.baseTurnAngle, 0, (int)studio.view.unitTurnAngle);
                baseTurnAngleChanged = EditorGUI.EndChangeCheck();
            }

            List<SubView> checkedSubViews = new List<SubView>();

            bool subViewChanged = false;
            for (int i = 0; i < studio.view.size; i++)
            {
                float subViewTurnAngle = studio.view.unitTurnAngle * i;
                float turnAngle = studio.view.baseTurnAngle + subViewTurnAngle;
                int intTurnAngle = Mathf.RoundToInt(turnAngle);
                string viewName = (studio.view.subViewToggles[i].name.Length > 0) ? studio.view.subViewToggles[i].name : (intTurnAngle + "deg");

                RotationCallback callback = (Model model) =>
                {
                    if (model == null)
                        return;

                    if (studio.view.rotationType == RotationType.Camera)
                    {
                        CameraHelper.LocateMainCameraToModel(model, studio, turnAngle);
                    }
                    else if (studio.view.rotationType == RotationType.Model)
                    {
                        if (Model.IsMeshModel(model))
                            Model.AsMeshModel(model).currentAngle = turnAngle;
                        model.Rotate(turnAngle);
                    }
                };

                if (groupOpened)
                {
                    bool applied = DrawEachView(string.Format("{0}", intTurnAngle) + "º", studio.view.subViewToggles[i]);
                    if (applied)
                    {
                        studio.appliedSubViewTurnAngle = subViewTurnAngle;
                        studio.appliedSubViewName = viewName;
                        callback(SelectedModel);
                    }
                    subViewChanged |= applied;
                }

                if (studio.view.subViewToggles[i].check)
                    checkedSubViews.Add(new SubView(intTurnAngle, viewName, callback));
            }

            studio.view.checkedSubViews = checkedSubViews;

            if (groupOpened)
            {
                EditorGUI.indentLevel--;

                if (studio.view.size > 1)
                    DrawViewSelectionButtons(studio.view.subViewToggles);

                if (rotationTypeChanged || baseTurnAngleChanged || subViewChanged)
                {
                    if (studio.view.rotationType == RotationType.Camera)
                        CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
                    else if (studio.view.rotationType == RotationType.Model)
                        RotateAllModels(CurrentTurnAngle);
                }

                GUILayout.EndVertical(); // Group Box
            }
        }

        private void DrawTileFields(ref bool slopeAngleChanged)
        {
            EditorGUI.BeginChangeCheck();
            studio.view.isTileVisible = EditorGUILayout.Toggle("Show Reference Tile", studio.view.isTileVisible);
            bool isTileVisibleChanged = EditorGUI.EndChangeCheck();

            if (studio.view.isTileVisible)
            {
                EditorGUI.indentLevel++;

                studio.view.tileType = (TileType)EditorGUILayout.EnumPopup("Tile Type", studio.view.tileType);

                EditorGUI.BeginChangeCheck();
                studio.view.tileAspectRatio = EditorGUILayout.Vector2Field("Aspect Ratio", studio.view.tileAspectRatio);
                bool aspectRatioChanged = EditorGUI.EndChangeCheck();

                if (studio.view.tileAspectRatio.x < 1f)
                    studio.view.tileAspectRatio.x = 1f;
                if (studio.view.tileAspectRatio.y < 1f)
                    studio.view.tileAspectRatio.y = 1f;
                if (studio.view.tileAspectRatio.x < studio.view.tileAspectRatio.y)
                    studio.view.tileAspectRatio.x = studio.view.tileAspectRatio.y;

                EditorGUI.indentLevel--;

                if (isTileVisibleChanged || slopeAngleChanged)
                {
                    studio.view.tileAspectRatio.x = studio.view.tileAspectRatio.y / Mathf.Sin(studio.view.slopeAngle * Mathf.Deg2Rad);
                }
                else if (aspectRatioChanged)
                {
                    studio.view.slopeAngle = Mathf.Asin(studio.view.tileAspectRatio.y / studio.view.tileAspectRatio.x) * Mathf.Rad2Deg;
                    slopeAngleChanged = true;
                }
            }

            if (SelectedModel != null)
            {
                if (studio.view.isTileVisible)
                {
                    if (studio.view.tileObj == null)
                        studio.view.tileObj = ObjectHelper.GetOrCreateObject(EditorConstants.HELPER_TILES_NAME, EditorConstants.TILE_FOLDER_NAME, Vector3.zero);

                    if (studio.view.tileObj != null)
                    {
                        Vector3 modelBottom = SelectedModel.ComputedBottom;
                        modelBottom.y -= 0.1f;
                        studio.view.tileObj.transform.position = modelBottom;
                    }

                    TileHelper.UpdateTileToModel(SelectedModel, studio);
                }
                else
                {
                    if (isTileVisibleChanged)
                        ObjectHelper.DeleteObject(EditorConstants.HELPER_TILES_NAME);
                }
            }
        }

        private void MigrateViews(SubViewToggle[] oldSubViewToggles)
        {
            if (oldSubViewToggles.Length < studio.view.subViewToggles.Length)
            {
                for (int oldIndex = 0; oldIndex < oldSubViewToggles.Length; ++oldIndex)
                {
                    float ratio = (float)oldIndex / oldSubViewToggles.Length;
                    int newIndex = Mathf.FloorToInt(studio.view.subViewToggles.Length * ratio);
                    studio.view.subViewToggles[newIndex].name = oldSubViewToggles[oldIndex].name;

                    if (oldSubViewToggles[oldIndex].check)
                        studio.view.subViewToggles[newIndex].check = true;
                }
            }
            else if (oldSubViewToggles.Length > studio.view.subViewToggles.Length)
            {
                for (int newIndex = 0; newIndex < studio.view.subViewToggles.Length; ++newIndex)
                {
                    float ratio = (float)newIndex / studio.view.subViewToggles.Length;
                    int oldIndex = Mathf.FloorToInt(oldSubViewToggles.Length * ratio);
                    studio.view.subViewToggles[newIndex].name = oldSubViewToggles[oldIndex].name;

                    if (oldSubViewToggles[oldIndex].check)
                        studio.view.subViewToggles[newIndex].check = true;
                }
            }
        }

        private void DrawViewSelectionButtons(SubViewToggle[] subViewToggles)
        {
            EditorGUILayout.BeginHorizontal();
            if (DrawingHelper.DrawNarrowButton("Select all"))
            {
                for (int i = 0; i < subViewToggles.Length; i++)
                    subViewToggles[i].check = true;
            }
            if (DrawingHelper.DrawNarrowButton("Clear all"))
            {
                for (int i = 0; i < subViewToggles.Length; i++)
                    subViewToggles[i].check = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool DrawEachView(string label, SubViewToggle subViewToggle)
        {
            bool applied = false;
            Rect rect = EditorGUILayout.BeginHorizontal();
            {
                subViewToggle.check = EditorGUILayout.Toggle(label, subViewToggle.check);

                Rect textFieldRect = new Rect(rect.x + 50, rect.y, rect.width * 0.2f, EditorConstants.NARROW_BUTTON_HEIGHT);
                if (!subViewToggle.check) GUI.enabled = false;
                {
                    EditorGUI.BeginChangeCheck();
                    subViewToggle.name = EditorGUI.TextField(textFieldRect, subViewToggle.name);
                    if (EditorGUI.EndChangeCheck())
                        PathHelper.CorrectPathString(ref subViewToggle.name);
                }
                if (!subViewToggle.check) GUI.enabled = true;

                if (DrawingHelper.DrawNarrowButton("Apply", 60))
                    applied = true;
            }
            EditorGUILayout.EndHorizontal();

            return applied;
        }

        private void DrawShadowFields()
        {
            if (SelectedModel == null || Camera.main == null)
                return;

            if (!DrawGroupOrPass("Shadow", ref studio.shadow.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();
            studio.shadow.type = (ShadowType)EditorGUILayout.EnumPopup("Shadow Type", studio.shadow.type);
            bool shadowTypeChanged = EditorGUI.EndChangeCheck();

            if (shadowTypeChanged)
            {
                if (studio.shadow.type != ShadowType.None)
                {
                    studio.shadow.shadowOnly = shadowWithoutModel;
                }
                else
                {
                    shadowWithoutModel = studio.shadow.shadowOnly;
                    studio.shadow.shadowOnly = false;
                }
            }

            if (studio.shadow.type == ShadowType.Simple)
            {
                EditorGUI.indentLevel++;

                if (shadowTypeChanged)
                {
                    ObjectHelper.DeleteObject(EditorConstants.DYNAMIC_SHADOW_NAME);
                    ObjectHelper.DeleteObject(EditorConstants.MATTE_SHADOW_NAME);
                }

                if (studio.shadow.obj == null)
                    studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorConstants.SIMPLE_SHADOW_NAME, EditorConstants.SHADOW_FOLDER_NAME, Vector3.zero);

                ShadowHelper.LocateShadowToModel(SelectedModel, studio);

                if (SelectedModel.toUseMyShadowScale)
                    DrawSimpleShadowScaleField(ref SelectedModel.simpleShadowScale);
                else
                    DrawSimpleShadowScaleField(ref studio.shadow.simple.scale);

                EditorGUI.indentLevel++;
                SelectedModel.toUseMyShadowScale = EditorGUILayout.Toggle("Model-Specific", SelectedModel.toUseMyShadowScale);
                EditorGUI.indentLevel--;

                studio.shadow.simple.isDynamicScale = EditorGUILayout.Toggle("Dynamic Scale", studio.shadow.simple.isDynamicScale);

                studio.shadow.simple.toKeepSquare = EditorGUILayout.Toggle("Keep Square", studio.shadow.simple.toKeepSquare);
                if (studio.shadow.simple.toKeepSquare)
                {
                    Vector3 modelSize = SelectedModel.GetDynamicSize();
                    float ratio = modelSize.x / modelSize.z;
                    if (SelectedModel.toUseMyShadowScale)
                        SelectedModel.simpleShadowScale.y = SelectedModel.simpleShadowScale.x * ratio;
                    else
                        studio.shadow.simple.scale.y = studio.shadow.simple.scale.x * ratio;
                }

                DrawShadowOpacityField(studio.shadow.obj);

                DrawShadowOnlyField();

                ShadowHelper.ScaleSimpleShadow(SelectedModel, studio);

                EditorGUI.indentLevel--;
            }
            else if (studio.shadow.type == ShadowType.TopDown)
            {
                EditorGUI.indentLevel++;

                if (shadowTypeChanged)
                {
                    ObjectHelper.DeleteObjectUnder(EditorConstants.SIMPLE_SHADOW_NAME, SelectedModel.transform);
                    ObjectHelper.DeleteObject(EditorConstants.MATTE_SHADOW_NAME);
                }

                if (studio.shadow.obj == null)
                    studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorConstants.DYNAMIC_SHADOW_NAME, EditorConstants.SHADOW_FOLDER_NAME, Vector3.zero);

                ShadowHelper.LocateShadowToModel(SelectedModel, studio);

                Camera camera;
                GameObject fieldObj;
                ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);

                CameraHelper.LookAtModel(camera.transform, SelectedModel);
                ShadowHelper.ScaleShadowField(camera, fieldObj);

                DrawShadowOpacityField(fieldObj);

                DrawShadowOnlyField();

                EditorGUI.indentLevel--;
            }
            else if (studio.shadow.type == ShadowType.Matte)
            {
                EditorGUI.indentLevel++;

                if (shadowTypeChanged)
                {
                    ObjectHelper.DeleteObjectUnder(EditorConstants.SIMPLE_SHADOW_NAME, SelectedModel.transform);
                    ObjectHelper.DeleteObject(EditorConstants.DYNAMIC_SHADOW_NAME);
                }

                if (studio.shadow.obj == null)
                    studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorConstants.MATTE_SHADOW_NAME, EditorConstants.SHADOW_FOLDER_NAME, Vector3.zero);

                ShadowHelper.LocateShadowToModel(SelectedModel, studio);

                if (Model.IsMeshModel(SelectedModel))
                    ShadowHelper.ScaleMatteField(Model.AsMeshModel(SelectedModel), studio.shadow.obj, studio.lit);

                string message = "Rotate the directional light by adjusting 'Slope Angle' and 'Turn Angle'.";
                if (studio.lit.cameraRotationFollow)
                    message = "Rotate the directional light by adjusting 'Slope Angle' and 'Turn Angle' by turning off 'Follow Camera Rotation'.";
                EditorGUILayout.HelpBox(message, MessageType.Info);

                DrawShadowOpacityField(studio.shadow.obj);

                EditorGUILayout.HelpBox("Other shadow details is in Light component.", MessageType.Info);

                EditorGUI.indentLevel--;
            }
            else
            {
                if (shadowTypeChanged)
                {
                    ObjectHelper.DeleteObjectUnder(EditorConstants.SIMPLE_SHADOW_NAME, SelectedModel.transform);
                    ObjectHelper.DeleteObject(EditorConstants.DYNAMIC_SHADOW_NAME);
                    ObjectHelper.DeleteObject(EditorConstants.MATTE_SHADOW_NAME);
                }

                studio.shadow.shadowOnly = false;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawSimpleShadowScaleField(ref Vector2 scale)
        {
            scale = EditorGUILayout.Vector2Field("Scale", scale);
            if (scale.x < 0.01f)
                scale.x = 0.01f;
            if (scale.y < 0.01f)
                scale.y = 0.01f;
        }

        private void DrawShadowOpacityField(GameObject shadowObj)
        {
            if (shadowObj == null)
                return;

            Renderer renderer = shadowObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.sharedMaterial.color;
                float opacity = EditorGUILayout.Slider("Opacity", color.a, 0, 1);
                color.a = Mathf.Clamp01(opacity);
                renderer.sharedMaterial.color = color;
            }
        }

        private void DrawShadowOnlyField()
        {
            studio.shadow.shadowOnly = EditorGUILayout.Toggle("Shadow Only", studio.shadow.shadowOnly);
        }

        private void DrawExtractionFields()
        {
            if (!DrawGroupOrPass("Extraction", ref studio.extraction.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            studio.extraction.com = EditorGUILayout.ObjectField("Extractor",
                studio.extraction.com, typeof(Extractor), false) as Extractor;
            if (studio.extraction.com == null)
            {
                GameObject prefab = AssetHelper.FindAsset<GameObject>("Extractor", "DefaultExtractor");
                if (prefab != null)
                    studio.extraction.com = prefab.GetComponent<DefaultExtractor>();
            }

            if (studio.extraction.com != null)
            {
                EditorGUI.indentLevel++;

                var so = new SerializedObject(studio.extraction.com);
                var itr = so.GetIterator();
                bool isFirst = true;
                //for (bool enterChildren = true; itr.NextVisible(enterChildren); enterChildren = false)
                while (itr.NextVisible(true))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        continue;
                    }
                    EditorGUILayout.PropertyField(itr);
                }
                so.ApplyModifiedProperties();

                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawPreviewFields()
        {
            if (EditorApplication.isPlaying)
                return;

            if (Model.IsParticleModel(SelectedModel))
                return;

            if (!DrawGroupOrPass("Preview", ref studio.preview.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            EditorGUI.BeginChangeCheck();
            studio.preview.on = EditorGUILayout.Toggle("Show Preview", studio.preview.on);
            bool anyChanged = EditorGUI.EndChangeCheck();

            if (studio.preview.on)
            {
                EditorGUI.indentLevel++;

                if (studio.output.toMakeNormalMap)
                {
                    EditorGUI.BeginChangeCheck();
                    studio.preview.isNormalMap = EditorGUILayout.Toggle("Normal Map", studio.preview.isNormalMap);
                    anyChanged |= EditorGUI.EndChangeCheck();
                }

                studio.preview.backgroundType = (PreviewBackgroundType)EditorGUILayout.EnumPopup("Background Type", studio.preview.backgroundType);
                if (studio.preview.backgroundType == PreviewBackgroundType.SingleColor)
                {
                    EditorGUI.indentLevel++;
                    studio.preview.backgroundColor = EditorGUILayout.ColorField("Color", studio.preview.backgroundColor);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;

                if (anyChanged || DrawingHelper.DrawMiddleButton("Update Preview"))
                    UpdatePreviewTexture();

                EditorGUILayout.HelpBox("Studio gets slower because frequent preview updates occur.", MessageType.Warning);
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawFilmingFields()
        {
            if (!DrawGroupOrPass("Filming", ref studio.filming.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            Vector2 resolVec = new Vector2(studio.filming.resolution.width, studio.filming.resolution.height);
            resolVec = EditorGUILayout.Vector2Field("Resolution", resolVec);
            studio.filming.resolution = new Resolution(resolVec);

            EditorGUI.BeginChangeCheck();
            studio.filming.numOfFrames = EditorGUILayout.IntField("Number of Frames", studio.filming.numOfFrames);
            if (studio.filming.numOfFrames < 1)
                studio.filming.numOfFrames = 1;
            if (EditorGUI.EndChangeCheck())
            {
                studio.sampling = null;
                studio.filming.simulatedIndex = 0;
            }

            if (!EditorApplication.isPlaying)
            {
                if (Model.IsMeshModel(SelectedModel) && selectedAnimation != null) // SelectedModel is not null
                {
                    EditorGUI.BeginChangeCheck();
                    string label = string.Format("Simulate (0~{0})", studio.filming.numOfFrames - 1);
                    studio.filming.simulatedIndex = EditorGUILayout.IntSlider(label,
                        studio.filming.simulatedIndex, 0, studio.filming.numOfFrames - 1);
                    if (EditorGUI.EndChangeCheck())
                    {
                        float frameRatio = 0.0f;
                        if (studio.filming.simulatedIndex > 0 && studio.filming.simulatedIndex < studio.filming.numOfFrames)
                            frameRatio = (float)studio.filming.simulatedIndex / (float)(studio.filming.numOfFrames - 1);

                        MeshModel meshModel = Model.AsMeshModel(SelectedModel);
                        float frameTime = meshModel.GetTimeForRatio(selectedAnimation.clip, frameRatio);
                        meshModel.Simulate(selectedAnimation, new Frame(studio.filming.simulatedIndex, frameTime));
                    }
                }
            }

            if (!EditorApplication.isPlaying)
            {
                studio.filming.delay = EditorGUILayout.DoubleField("Delay", studio.filming.delay);
                if (studio.filming.delay < 0.0)
                    studio.filming.delay = 0.0;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawSamplingFields()
        {
            if (SelectedModel == null)
                return;

            if (Model.IsMeshModel(SelectedModel) && selectedAnimation == null)
            {
                EditorGUILayout.HelpBox("No mesh animation selected.", MessageType.Info);
                return;
            }

            if (DrawingHelper.DrawWideButton("Sample"))
            {
                studio.filming.simulatedIndex = 0;

                HideSelectorAndViewer();

                HideAllModels();
                SelectedModel.gameObject.SetActive(true);
                SetUpCameraBeforeCapturing();
                TileHelper.HideAllTiles();

                if (Model.IsMeshModel(SelectedModel))
                    sampler = new MeshSampler(SelectedModel, selectedAnimation, studio);
                else if (Model.IsParticleModel(SelectedModel))
                    sampler = new ParticleSampler(SelectedModel, studio);

                sampler.SampleFrames(() =>
                {
                    RestoreAllModels();
                    SetUpCameraAfterCapturing();
                    TileHelper.UpdateTileToModel(SelectedModel, studio);

                    if (sampler.IsCancelled)
                        studio.sampling = null;
                    else
                        ShowSelectorAndPreviewer();

                    sampler = null;
                });
            }

            if (studio.sampling == null)
                return;

            if (DrawingHelper.DrawWideButton(studio.sampling.selectedFrames.Count + " frame(s) selected."))
                ShowSelectorAndPreviewer();
        }

        public void ShowSelectorAndPreviewer()
        {
            if (FrameSelector.instance != null || AnimationPreviewer.instance != null)
                return;

            FrameSelector selector = ScriptableWizard.DisplayWizard<FrameSelector>("Frame Selector");
            if (selector != null)
                selector.SetInfo(studio);

            AnimationPreviewer previewer = ScriptableWizard.DisplayWizard<AnimationPreviewer>("Animation Previewer");
            if (previewer != null)
                previewer.SetInfo(studio);
        }

        public void HideSelectorAndViewer()
        {
            if (FrameSelector.instance != null)
                FrameSelector.instance.Close();

            if (AnimationPreviewer.instance != null)
                AnimationPreviewer.instance.Close();
        }

        private void DrawTrimmingFields()
        {
            if (!DrawGroupOrPass("Trimming", ref studio.triming.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            studio.triming.on = EditorGUILayout.Toggle("Trim", studio.triming.on);
            if (studio.triming.on)
            {
                EditorGUI.indentLevel++;

                studio.triming.margin = EditorGUILayout.IntField("Margin", studio.triming.margin);
                if (studio.triming.margin < 0)
                {
                    int absMargin = Mathf.Abs(studio.triming.margin);
                    if (absMargin > studio.filming.resolution.width / 2)
                        studio.triming.margin = -(studio.filming.resolution.width / 2);
                    if (absMargin > studio.filming.resolution.height / 2)
                        studio.triming.margin = -(studio.filming.resolution.height / 2);
                }

                studio.triming.isUniformSize = EditorGUILayout.Toggle("Uniform Size", studio.triming.isUniformSize);

                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawPackingFields()
        {
            if (!DrawGroupOrPass("Packing", ref studio.packing.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            studio.packing.on = EditorGUILayout.Toggle("Pack", studio.packing.on);

            if (studio.packing.on)
            {
                EditorGUI.indentLevel++;

                studio.packing.method = (PackingMethod)EditorGUILayout.EnumPopup("Method", studio.packing.method);

                EditorGUI.indentLevel++;
                if (studio.packing.method == PackingMethod.Optimized)
                    studio.packing.maxAtlasSizeIndex = EditorGUILayout.Popup("Max Size", studio.packing.maxAtlasSizeIndex, studio.atlasSizes);
                else if (studio.packing.method == PackingMethod.InOrder)
                    studio.packing.minAtlasSizeIndex = EditorGUILayout.Popup("Min Size", studio.packing.minAtlasSizeIndex, studio.atlasSizes);
                EditorGUI.indentLevel--;

                studio.packing.padding = EditorGUILayout.IntField("Padding", studio.packing.padding);
                if (studio.packing.padding < 0)
                    studio.packing.padding = 0;

                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawOutputFields()
        {
            if (!DrawGroupOrPass("Output", ref studio.output.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            if (studio.packing.on)
            {
                studio.output.toMakeAnimationClip = EditorGUILayout.Toggle("Make Animation Clip", studio.output.toMakeAnimationClip);
                if (studio.output.toMakeAnimationClip)
                {
                    EditorGUI.indentLevel++;
                    AnimationPreviewer.DrawFrameRateField(ref studio.output.frameRate);
                    AnimationPreviewer.DrawIntervalField(ref studio.output.frameInterval);
                    EditorGUI.indentLevel--;
                }

                studio.output.toMakeAnimatorController = EditorGUILayout.Toggle("Make Animator Controller", studio.output.toMakeAnimatorController);
            }

            studio.output.toMakePrefab = EditorGUILayout.Toggle("Make Prefab", studio.output.toMakePrefab);
            if (studio.output.toMakePrefab)
            {
                if (Model.IsMeshModel(SelectedModel))
                {
                    EditorGUI.indentLevel++;

                    studio.output.isCompactCollider = EditorGUILayout.Toggle("Compact Collider", studio.output.isCompactCollider);

                    studio.output.toMakeLocationPrefab = EditorGUILayout.Toggle("Make Location Prefab", studio.output.toMakeLocationPrefab);
                    if (studio.output.toMakeLocationPrefab)
                    {
                        EditorGUI.indentLevel++;
                        studio.output.locationSpritePrefab = EditorGUILayout.ObjectField("Location Prefab",
                            studio.output.locationSpritePrefab, typeof(GameObject), false) as GameObject;
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }
            }

            if (Model.IsMeshModel(SelectedModel))
            {
                studio.output.toMakeNormalMap = EditorGUILayout.Toggle("Make Normal Map", studio.output.toMakeNormalMap);

                studio.output.toMakeMaterial = EditorGUILayout.Toggle("Make Material", studio.output.toMakeMaterial);
                if (studio.output.toMakeMaterial)
                {
                    EditorGUI.indentLevel++;

                    studio.output.spriteMaterial = EditorGUILayout.ObjectField("Sprite Material",
                        studio.output.spriteMaterial, typeof(Material), false) as Material;

                    if (studio.output.spriteMaterial != null)
                    {
                        EditorGUI.indentLevel++;
                        studio.output.materialBuilder = EditorGUILayout.ObjectField("Material Builder",
                            studio.output.materialBuilder, typeof(MaterialBuilder), false) as MaterialBuilder;
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawDirectoryFields()
        {
            if (!DrawGroupOrPass("Directory", ref studio.dir.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            EditorGUILayout.LabelField("Export Path:");
            {
                int assetRootIndex = studio.dir.exportPath.IndexOf("Assets");
                if (assetRootIndex > 0)
                {
                    EditorGUI.indentLevel++;

                    string rootPath = studio.dir.exportPath.Substring(assetRootIndex - 1);

                    string oneModelPath = "One Model → ";
                    oneModelPath += rootPath;
                    oneModelPath += "/[Model]_[YYMMDD_hhmmss]/";
                    EditorGUILayout.LabelField(oneModelPath, EditorStyles.miniLabel);

                    string allModelsPath = "All Models → ";
                    allModelsPath += rootPath;
                    allModelsPath += "/[YYMMDD_hhmmss]/#_[Model]/";
                    EditorGUILayout.LabelField(allModelsPath, EditorStyles.miniLabel);

                    EditorGUI.indentLevel--;
                }
            }

            string title = "Choose a directory under 'Assets'";
            if (DrawingHelper.DrawMiddleButton(title, 250))
            {
                string selectedPath = EditorUtility.SaveFolderPanel(title, Application.dataPath, "Output");
                if (selectedPath != null && selectedPath.Length > 0)
                    studio.dir.exportPath = selectedPath;
                GUIUtility.ExitGUI();
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawNamingFields()
        {
            if (!DrawGroupOrPass("Naming", ref studio.naming.opened))
                return;

            GUILayout.BeginVertical(EditorConstants.GROUP_BOX_STYLE);

            EditorGUILayout.LabelField("File Name:");
            {
                EditorGUI.indentLevel++;

                string fileName = "";
                fileName += studio.naming.fileNamePrefix;
                fileName += "[Model]";

                string prefabName = "Prefab → " + fileName + ".prefab";
                EditorGUILayout.LabelField(prefabName, EditorStyles.miniLabel);

                string othersName = "Others → " + fileName + "_[Animation]_[View]";
                if (!studio.packing.on)
                    othersName += "_#";
                if (studio.output.toMakeNormalMap)
                    othersName += "(_normal)";
                othersName += ".*";
                EditorGUILayout.LabelField(othersName, EditorStyles.miniLabel);

                EditorGUI.BeginChangeCheck();
                studio.naming.fileNamePrefix = EditorGUILayout.TextField("Prefix", studio.naming.fileNamePrefix);
                if (EditorGUI.EndChangeCheck())
                    PathHelper.CorrectPathString(ref studio.naming.fileNamePrefix);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("Sprite Name:");
            {
                EditorGUI.indentLevel++;

                string spriteName = "";
                if (studio.naming.isModelPrefixSprite)
                    spriteName += "[Model]_";
                spriteName += "#";
                EditorGUILayout.LabelField(spriteName, EditorStyles.miniLabel);
                studio.naming.isModelPrefixSprite = EditorGUILayout.Toggle("Model Name Prefix", studio.naming.isModelPrefixSprite);

                EditorGUI.indentLevel--;
            }

            GUILayout.EndVertical(); // Group Box
        }

        private void DrawBakingFields()
        {
            if (SelectedModel != null && SelectedModel.IsReady())
            {
                if (Model.IsMeshModel(SelectedModel))
                {
                    string title;
                    MeshModel meshModel = Model.AsMeshModel(SelectedModel);

                    if (selectedAnimation != null && selectedAnimation.clip != null)
                    {
                        Sampling sampling = studio.sampling;
                        if (sampling != null && sampling.selectedFrames.Count == 0)
                        {
                            EditorGUILayout.HelpBox("No selected frames after sampling.", MessageType.Info);
                        }
                        else // sampling == null || sampling.selectedFrames.Count > 0
                        {
                            if (EditorApplication.isPlaying && sampling != null)
                            {
                                if (DrawingHelper.DrawWideButton("Save the sampled textures"))
                                {
                                    bakingModels.Clear();
                                    bakingModels.Add(SelectedModel);
                                    ExecuteToBake(true);
                                }
                            }

                            title = sampling != null ? "Bake the selected frames" : "Bake the selected animation";
                            if (DrawingHelper.DrawWideButton(title))
                            {
                                bakingModels.Clear();
                                bakingModels.Add(SelectedModel);

                                List<MeshAnimation> animBackups = meshModel.animations;
                                List<MeshAnimation> tempAnims = new List<MeshAnimation>();
                                tempAnims.Add(selectedAnimation);
                                meshModel.animations = tempAnims;

                                ExecuteToBake(false, () =>
                                {
                                    meshModel.animations = animBackups;
                                });
                            }
                        }
                    }

                    title = "Bake the selected model";
                    if (meshModel.animations.Count > 0)
                        title += " with its all animations.";
                    if (DrawingHelper.DrawWideButton(title))
                    {
                        bakingModels.Clear();
                        bakingModels.Add(SelectedModel);
                        ExecuteToBake(false);
                    }
                }
                else
                {
                    Sampling sampling = studio.sampling;
                    if (sampling != null && sampling.selectedFrames.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No selected frames after sampling.", MessageType.Info);
                    }
                    else // sampling == null || sampling.selectedFrames.Count > 0
                    {
                        string title = sampling != null ? "Bake the selected frames" : "Bake the selected model";
                        if (DrawingHelper.DrawWideButton(title))
                        {
                            bakingModels.Clear();
                            bakingModels.Add(SelectedModel);
                            ExecuteToBake(false);
                        }
                    }
                }
            }

            int meshModelCount = 0;
            int particleModelCount = 0;
            foreach (Model model in studio.model.list)
            {
                if (model == null)
                    continue;
                if (Model.IsMeshModel(model))
                    meshModelCount++;
                else if (Model.IsParticleModel(model))
                    particleModelCount++;
            }

            if (meshModelCount > 0 && particleModelCount > 0)
            {
                EditorGUILayout.HelpBox("Can't bake multiple types of model at the same time.", MessageType.Info);
            }
            else
            {
                if (DrawingHelper.DrawWideButton("Bake all models"))
                {
                    bakingModels.Clear();
                    foreach (Model model in studio.model.list)
                    {
                        if (model != null && model.IsReady())
                            bakingModels.Add(model);
                    }
                    Debug.Assert(bakingModels.Count > 0);

                    ExecuteToBake(false);
                }
            }
        }

        private void ExecuteToBake(bool isDirectSaving, CompletionCallback completion = null)
        {
            studio.filming.simulatedIndex = 0;
            HideSelectorAndViewer();

            HideAllModels();
            SetUpCameraBeforeCapturing();
            TileHelper.HideAllTiles();

            batcher = new Batcher(bakingModels, studio, isDirectSaving);
            batcher.Batch(() =>
            {
                batcher = null;

                EditorUtility.SetDirty(studio.gameObject);

                RestoreAllModels();
                SetUpCameraAfterCapturing();

                if (studio.view.rotationType == RotationType.Camera)
                    CameraHelper.LocateMainCameraToModel(SelectedModel, studio, CurrentTurnAngle);
                else if (studio.view.rotationType == RotationType.Model)
                    RotateAllModels(CurrentTurnAngle);

                ShadowHelper.LocateShadowToModel(SelectedModel, studio);
                if (Model.IsMeshModel(SelectedModel))
                    ShadowHelper.ScaleMatteField(Model.AsMeshModel(SelectedModel), studio.shadow.obj, studio.lit);

                TileHelper.UpdateTileToModel(SelectedModel, studio);

                completion?.Invoke();
            });
        }

        private bool DrawGroupOrPass(string name, ref bool opened)
        {
            Rect groupRect = EditorGUILayout.BeginVertical(); EditorGUILayout.EndVertical();

            Texture arrowTex = opened ? ArrowDownTexture : ArrowRightTexture;
            if (arrowTex != null)
            {
                float plusY = opened ? 10 : 5;
                Rect arrowRect = new Rect(groupRect.x - arrowTex.width - 2, groupRect.y + plusY, arrowTex.width, arrowTex.height);
                GUI.DrawTexture(arrowRect, arrowTex);

                if (Event.current.type == EventType.MouseDown)
                {
                    if (Event.current.mousePosition.x >= arrowRect.x && Event.current.mousePosition.x <= arrowRect.x + arrowRect.width &&
                        Event.current.mousePosition.y >= arrowRect.y && Event.current.mousePosition.y <= arrowRect.y + arrowRect.height)
                    {
                        opened = !opened;
                        Event.current.Use();
                    }
                }
            }
            else
            {
                opened = true;
            }

            if (opened)
            {
                return true;
            }
            else
            {
                Rect labelRect = EditorGUILayout.BeginVertical();
                {
                    DrawingHelper.FillRect(labelRect, EditorGUIUtility.isProSkin ? darkGreenColor : lightGreenColor);

                    GUIStyle headerStyle = new GUIStyle();
                    headerStyle.alignment = TextAnchor.MiddleCenter;
                    headerStyle.fontSize = 12;
                    headerStyle.fontStyle = FontStyle.Bold;
                    headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                    EditorGUILayout.LabelField(name, headerStyle);

                    if (Event.current.type == EventType.MouseDown)
                    {
                        if (Event.current.mousePosition.x >= labelRect.x && Event.current.mousePosition.x <= labelRect.x + labelRect.width &&
                            Event.current.mousePosition.y >= labelRect.y && Event.current.mousePosition.y <= labelRect.y + labelRect.height)
                        {
                            opened = true;
                            Event.current.Use();
                        }
                    }
                }
                EditorGUILayout.EndVertical();

                return false;
            }
        }

        private void RotateAllModels(float turnAngle)
        {
            foreach (Model model in studio.model.list)
            {
                if (model == null)
                    continue;

                if (Model.IsMeshModel(model))
                    Model.AsMeshModel(model).currentAngle = turnAngle;

                model.Rotate(turnAngle);
            }
        }

        private void HideAllModels()
        {
            modelActivationBackup.Clear();

            Model[] allModels = Resources.FindObjectsOfTypeAll<Model>();
            foreach (Model model in allModels)
            {
                if (!modelActivationBackup.ContainsKey(model))
                {
                    modelActivationBackup.Add(model, model.gameObject.activeSelf);
                    model.gameObject.SetActive(false);
                }
            }
        }

        private void RestoreAllModels()
        {
            foreach (KeyValuePair<Model, bool> pair in modelActivationBackup)
                pair.Key.gameObject.SetActive(pair.Value);
        }

        private void SetUpCameraBeforeCapturing()
        {
            cameraClearFlagsBackup = Camera.main.clearFlags;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            cameraClearFlagsBackup = Camera.main.clearFlags;
            cameraBackgroundColorBackup = Camera.main.backgroundColor;
            Camera.main.targetTexture = new RenderTexture(studio.filming.resolution.width, studio.filming.resolution.height, 24, RenderTextureFormat.ARGB32);

            Type hdrpCameraType = ExtractionHelper.GetHdrpCameraType();
            if (hdrpCameraType != null)
            {
                Component hdrpCameraComponent = Camera.main.gameObject.GetComponent(hdrpCameraType);
                if (hdrpCameraComponent != null)
                {
                    FieldInfo hdrpCameraClearColorModeField = hdrpCameraType.GetField("clearColorMode");
                    if (hdrpCameraClearColorModeField != null)
                    {
                        hdrpCameraClearColorModeBackup = hdrpCameraClearColorModeField.GetValue(hdrpCameraComponent);

                        Type hdrpClearColorModeEnumA = Type.GetType(
                            "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData+ClearColorMode, " +
                            "Unity.RenderPipelines.HighDefinition.Runtime",
                            false, true
                        );
                        Type hdrpClearColorModeEnumB = Type.GetType(
                            "UnityEngine.Experimental.Rendering.HDPipeline.HDAdditionalCameraData+ClearColorMode, " +
                            "Unity.RenderPipelines.HighDefinition.Runtime",
                            false, true
                        );
                        Type hdrpClearColorModeEnum = hdrpClearColorModeEnumA ?? hdrpClearColorModeEnumB;

                        object hdrpClearColorMode = null;
                        if (hdrpClearColorModeEnum != null)
                        {
                            try
                            {
                                hdrpClearColorMode = Enum.Parse(hdrpClearColorModeEnum, "Color");
                            }
                            catch (Exception) { }

                            if (hdrpClearColorMode == null)
                            {
                                try
                                {
                                    hdrpClearColorMode = Enum.Parse(hdrpClearColorModeEnum, "BackgroundColor");
                                }
                                catch (Exception) { }
                            }
                        }

                        hdrpCameraClearColorModeField.SetValue(hdrpCameraComponent, hdrpClearColorMode);
                    }

                    FieldInfo hdrpCameraBackgroundColorHdrField = hdrpCameraType.GetField("backgroundColorHDR");
                    if (hdrpCameraBackgroundColorHdrField != null)
                        hdrpCameraBackgroundColorHdrBackup = hdrpCameraBackgroundColorHdrField.GetValue(hdrpCameraComponent);
                }
            }
        }

        private void SetUpCameraAfterCapturing()
        {
            Camera.main.clearFlags = cameraClearFlagsBackup;
            Camera.main.backgroundColor = cameraBackgroundColorBackup;
            Camera.main.targetTexture = null;

            Type hdrpCameraType = ExtractionHelper.GetHdrpCameraType();
            if (hdrpCameraType != null)
            {
                Component hdrpCameraComponent = Camera.main.gameObject.GetComponent(hdrpCameraType);
                if (hdrpCameraComponent != null)
                {
                    FieldInfo hdrpCameraClearColorModeField = hdrpCameraType.GetField("clearColorMode");
                    if (hdrpCameraClearColorModeField != null)
                        hdrpCameraClearColorModeField.SetValue(hdrpCameraComponent, hdrpCameraClearColorModeBackup);

                    FieldInfo hdrpCameraBackgroundColorHdrField = hdrpCameraType.GetField("backgroundColorHDR");
                    if (hdrpCameraBackgroundColorHdrField != null)
                        hdrpCameraBackgroundColorHdrField.SetValue(hdrpCameraComponent, hdrpCameraBackgroundColorHdrBackup);
                }
            }
        }

        private void UpdatePreviewTexture()
        {
            if (SelectedModel == null)
                return;
            if (sampler != null || batcher != null)
                return;

            HideAllModels();
            SelectedModel.gameObject.SetActive(true);
            SetUpCameraBeforeCapturing();
            TileHelper.HideAllTiles();

            previewTexture = CapturingHelper.CapturePixelsManagingShadow(SelectedModel, studio);

            Texture2D normalTex = null;
            if (studio.output.toMakeNormalMap && studio.preview.isNormalMap)
            {
                float rotX = studio.view.slopeAngle;
                float rotY = studio.view.rotationType == RotationType.Camera ? CurrentTurnAngle : 0;
                normalTex = CapturingHelper.CapturePixelsForNormal(SelectedModel, rotX, rotY, studio.shadow.obj);
            }

            if (studio.triming.on)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(SelectedModel.GetPivotPosition());
                PixelVector pivot2D = new PixelVector(screenPos);

                PixelBound texBound = new PixelBound();
                if (!TextureHelper.GetPixelBound(previewTexture, pivot2D, texBound))
                {
                    texBound.min.x = pivot2D.x - 1;
                    texBound.max.x = pivot2D.x + 1;
                    texBound.min.y = pivot2D.y - 1;
                    texBound.max.y = pivot2D.y + 1;
                }

                pivot2D.SubtractWithMargin(texBound.min, studio.triming.margin);

                previewTexture = TextureHelper.TrimTexture(previewTexture, texBound, studio.triming.margin, EngineConstants.CLEAR_COLOR32);
                if (normalTex != null)
                    normalTex = TextureHelper.TrimTexture(normalTex, texBound, studio.triming.margin, EngineConstants.NORMALMAP_COLOR32, studio.output.toMakeNormalMap);
            }

            if (normalTex != null)
                previewTexture = normalTex;

            RestoreAllModels();
            SetUpCameraAfterCapturing();
            TileHelper.UpdateTileToModel(SelectedModel, studio);
        }

        public override bool HasPreviewGUI()
        {
            if (EditorApplication.isPlaying)
                return false;
            if (studio == null)
                return false;
            if (sampler != null || batcher != null)
                return false;
            return studio.preview.on && Model.IsMeshModel(SelectedModel);
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Preview");
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (rect.width <= 1 || rect.height <= 1)
                return;

            if (previewTexture == null)
                return;

            Rect scaledRect = PreviewHelper.ScalePreviewRect(previewTexture, rect);
            Texture2D scaledTex = TextureHelper.ScaleTexture(previewTexture, (int)scaledRect.width, (int)scaledRect.height);

            if (studio.preview.backgroundType == PreviewBackgroundType.Checker)
            {
                EditorGUI.DrawTextureTransparent(scaledRect, scaledTex);
            }
            else if (studio.preview.backgroundType == PreviewBackgroundType.SingleColor)
            {
                EditorGUI.DrawRect(scaledRect, studio.preview.backgroundColor);
                GUI.DrawTexture(scaledRect, scaledTex);
            }

            EditorGUI.LabelField(rect, string.Format("{0} X {1}", previewTexture.width, previewTexture.height), EditorStyles.whiteLabel);
        }
    }
}
