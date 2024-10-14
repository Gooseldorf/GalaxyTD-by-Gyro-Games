using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ABS
{
	public class MeshBaker : Baker
	{
        private readonly MeshModel meshModel = null;

        private readonly List<MeshAnimation> meshAnimations;

        private string fileNameForModel = "";

        private readonly Vector3 simpleShadowBaseScale;
        private Vector3 modelSizeForView; // for dynamic simple shadow

        private GameObject outRootPrefab = null;

        public int AnimationIndex { get; set; }

        private PixelBound[] frameBounds = null;
        private Texture2D[] frameNormalTextures = null;

        private bool isPrefabCompactCollider = false;
        private bool wasColliderSetup = false;
        private PixelBound[] frameCompactBounds = null;
        private Vector2[] frameCompactVectors = null; // vector ratio from compact area's center to texture area's center in trimmed texture

        class LocationMapping
        {
            public Location location3d;
            public GameObject location2dObj;
            public PixelVector[] frameLocationPositions;
            public Vector2[] frameRatioPositions;

            public LocationMapping(Location location3d, GameObject location2dObj)
            {
                this.location3d = location3d;
                this.location2dObj = location2dObj;
            }
        }
        private LocationMapping[] locationMappings = null;

        public MeshBaker(Model model, List<MeshAnimation> animations, Studio studio, string sIndex, string parentFolderPath)
            : base(model, studio, sIndex, parentFolderPath)
        {
            this.meshAnimations = animations;

            meshModel = model as MeshModel;

            if (studio.shadow.type == ShadowType.Simple && studio.shadow.simple.isDynamicScale)
                simpleShadowBaseScale = studio.shadow.obj.transform.localScale;

            stateMachine = new StateMachine<BakingState>();
            stateMachine.AddState(BakingState.Initialize, OnInitialize);
            stateMachine.AddState(BakingState.BeginAnimation, OnBeginAnimation);
            stateMachine.AddState(BakingState.BeginView, OnBeginView);
            stateMachine.AddState(BakingState.BeginFrame, OnBeginFrame);
            stateMachine.AddState(BakingState.CaptureFrame, OnCaptureFrame);
            stateMachine.AddState(BakingState.EndFrame, OnEndFrame);
            stateMachine.AddState(BakingState.EndView, OnEndView);
            stateMachine.AddState(BakingState.EndAnimation, OnEndAnimation);
            stateMachine.AddState(BakingState.Finalize, OnFinalize);

            stateMachine.ChangeState(BakingState.Initialize);
        }

        public void OnInitialize()
        {
            try
            {
                if (studio.view.rotationType == RotationType.Model)
                    CameraHelper.LocateMainCameraToModel(model, studio);

                ShadowHelper.LocateShadowToModel(model, studio);

                if (studio.shadow.type == ShadowType.TopDown)
                {
                    ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out Camera camera, out GameObject fieldObj);

                    CameraHelper.LookAtModel(camera.transform, model);
                    ShadowHelper.ScaleShadowField(camera, fieldObj);
                }
                else if (studio.shadow.type == ShadowType.Matte)
                {
                    ShadowHelper.ScaleMatteField(meshModel, studio.shadow.obj, studio.lit);
                }

                fileNameForModel = BuildFileBaseName();
                BuildFolderPathAndCreate(modelName);

                if (IsToMakePrefab())
                {
                    outRootPrefab = PrefabUtility.InstantiatePrefab(model.spritePrefab) as GameObject;
                    outRootPrefab.SetActive(false);

                    if (studio.output.IsToMakeLocationPrefab())
                    {
                        Location[] locations = model.GetComponentsInChildren<Location>();
                        if (locations.Length > 0)
                        {
                            locationMappings = new LocationMapping[locations.Length];
                            for (int i = 0; i < locations.Length; ++i)
                            {
                                Location loc = locations[i];
                                GameObject locObj = PrefabUtility.InstantiatePrefab(studio.output.locationSpritePrefab) as GameObject;
                                locObj.transform.parent = model.prefabBuilder.GetLocationsParent(outRootPrefab);
                                locObj.transform.localPosition = Vector3.zero;
                                locObj.transform.localRotation = Quaternion.identity;
                                locObj.name += "_" + loc.name;
                                locationMappings[i] = new LocationMapping(loc, locObj);
                            }
                        }
                    }
                }

                AnimationIndex = 0;

                stateMachine.ChangeState(BakingState.BeginAnimation);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnBeginAnimation()
        {
            try
            {
                MeshAnimation meshAnim = meshAnimations[AnimationIndex];

                if (!isMultiModel && meshAnimations.Count == 1 && studio.sampling != null)
                {
                    List<Frame> selectedFrames = studio.sampling.selectedFrames;
                    Debug.Assert(selectedFrames.Count > 0);
                    framesForView = new Frame[selectedFrames.Count];
                    selectedFrames.CopyTo(framesForView);
                }
                else
                {
                    framesForView = new Frame[studio.filming.numOfFrames];
                    for (int fi = 0; fi < framesForView.Length; ++fi)
                    {
                        float frameRatio = 0.0f;
                        if (fi > 0 && fi < framesForView.Length)
                            frameRatio = (float)fi / (float)(framesForView.Length - 1);

                        float time = meshModel.GetTimeForRatio(meshAnim.clip, frameRatio);
                        framesForView[fi] = new Frame(fi, time);
                    }
                }

                string animName;
                if (meshModel.referenceController != null)
                    animName = meshAnim.stateName;
                else
                    animName = meshAnim.clip.name;

                fileBaseName = fileNameForModel + "_" + animName;

                if (studio.packing.on && studio.output.toMakeAnimatorController)
                    BuildAnimatorController();

                if (EditorApplication.isPlaying)
                    frameInterval = meshAnim.clip.length / studio.filming.numOfFrames;

                viewIndex = 0;

                stateMachine.ChangeState(BakingState.BeginView);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnBeginView()
        {
            try
            {
                studio.extraction.com.Setup(framesForView.Length);

                if (EditorApplication.isPlaying)
                    meshModel.StopAndPlay(meshAnimations[AnimationIndex]);

                if (studio.triming.IsOnUniformSize())
                    uniformBound = new PixelBound();

                frameMainTextures = new Texture2D[framesForView.Length];
                if (studio.output.toMakeNormalMap)
                {
                    frameNormalTextures = new Texture2D[framesForView.Length];
                    CapturingHelper.SetupNormalPixelsLists();
                }

                if (studio.packing.on || studio.triming.IsOnUniformSize())
                    framePivots = new PixelVector[framesForView.Length];

                studio.view.checkedSubViews[viewIndex].func(model);
                viewName = studio.view.checkedSubViews[viewIndex].name;

                modelSizeForView = model.GetSize();

                frameBounds = new PixelBound[framesForView.Length];

                isPrefabCompactCollider = outRootPrefab && model.prefabBuilder.GetBoxCollider2D(outRootPrefab) != null && studio.output.isCompactCollider;
                if (isPrefabCompactCollider)
                {
                    frameCompactBounds = new PixelBound[framesForView.Length];
                    frameCompactVectors = new Vector2[framesForView.Length];
                }

                if (locationMappings != null)
                {
                    foreach (LocationMapping locMapping in locationMappings)
                    {
                        locMapping.frameLocationPositions = new PixelVector[framesForView.Length];
                        locMapping.frameRatioPositions = new Vector2[framesForView.Length];
                    }
                }

                frameIndex = 0;

                stateMachine.ChangeState(BakingState.BeginFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnBeginFrame()
        {
            try
            {
                int shownCurrFrameIndex = frameIndex + 1;
                float progress = (float)(viewIndex * framesForView.Length + shownCurrFrameIndex) / (studio.view.checkedSubViews.Count * framesForView.Length);

                if (studio.view.checkedSubViews.Count == 0)
                    IsCancelled = EditorUtility.DisplayCancelableProgressBar("Progress...", "Frame: " + shownCurrFrameIndex + " (" + ((int)(progress * 100f)) + "%)", progress);
                else
                    IsCancelled = EditorUtility.DisplayCancelableProgressBar("Progress...", "View: " + viewName + " | Frame: " + shownCurrFrameIndex + " (" + ((int)(progress * 100f)) + "%)", progress);

                if (IsCancelled)
                    throw new Exception("Cancelled");

                if (!EditorApplication.isPlaying)
                {
                    Frame frame = framesForView[frameIndex];
                    meshModel.Simulate(meshAnimations[AnimationIndex], frame);
                }

                Vector3 pivotScreenPos = Camera.main.WorldToScreenPoint(model.GetPivotPosition());
                currFramePivot = new PixelVector(pivotScreenPos);

                if (locationMappings != null)
                {
                    foreach (LocationMapping locMapping in locationMappings)
                    {
                        Vector3 locScreenPos = Camera.main.WorldToScreenPoint(locMapping.location3d.transform.position);
                        locMapping.frameLocationPositions[frameIndex] = new PixelVector(locScreenPos);
                    }
                }

                stateMachine.ChangeState(BakingState.CaptureFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnCaptureFrame()
        {
            try
            {
                if (EditorApplication.isPlaying)
                {
                    double deltaTime = EditorApplication.timeSinceStartup - prevTime;
                    if (deltaTime < frameInterval)
                        return;
                    prevTime = EditorApplication.timeSinceStartup;
                }
                else // Editor Mode
                {
                    double deltaTime = EditorApplication.timeSinceStartup - prevTime;
                    if (deltaTime < studio.filming.delay)
                        return;
                    prevTime = EditorApplication.timeSinceStartup;
                }

                if (studio.shadow.type == ShadowType.Simple && studio.shadow.simple.isDynamicScale)
                    ShadowHelper.ScaleSimpleShadowDynamically(modelSizeForView, simpleShadowBaseScale, meshModel, studio);

                CapturingHelper.ReadAndKeepRawPixelsManagingShadow(model, studio);
                if (studio.output.toMakeNormalMap)
                {
                    float rotX = studio.view.slopeAngle;
                    float rotY = studio.view.rotationType == RotationType.Camera ? studio.view.checkedSubViews[viewIndex].angle : 0;
                    CapturingHelper.ReadAndKeepRawPixelsForNormal_AllFrames(model, rotX, rotY, studio.shadow.obj);
                }

                stateMachine.ChangeState(BakingState.EndFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnEndFrame()
        {
            try
            {
                frameIndex++;

                if (frameIndex < framesForView.Length)
                    stateMachine.ChangeState(BakingState.BeginFrame);
                else
                    stateMachine.ChangeState(BakingState.EndView);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnEndView()
        {
            try
            {
                for (int fi = 0; fi < framesForView.Length; ++fi)
                {
                    Texture2D mainTex = CapturingHelper.ExtractValidPixelsForMain_OneFrame(fi, studio);
                    Texture2D normalTex = null;
                    if (studio.output.toMakeNormalMap)
                        normalTex = CapturingHelper.ExtractValidPixelsForNormal_OneFrame(fi);

                    PixelBound bound = new PixelBound();
                    PixelBound compactBound = new PixelBound();
                    if (!TextureHelper.GetPixelBound(mainTex, currFramePivot, bound, compactBound))
                    {
                        bound.min.x = currFramePivot.x - 1;
                        bound.max.x = currFramePivot.x + 1;
                        bound.min.y = currFramePivot.y - 1;
                        bound.max.y = currFramePivot.y + 1;
                    }

                    frameBounds[fi] = bound;

                    if (isPrefabCompactCollider)
                        frameCompactBounds[fi] = compactBound;

                    PixelVector pivot = new PixelVector(currFramePivot);

                    if (studio.triming.on)
                    {
                        if (studio.triming.isUniformSize)
                        {
                            TextureHelper.ExpandBound(bound, uniformBound);
                        }
                        else
                        {
                            pivot.SubtractWithMargin(bound.min, studio.triming.margin);

                            mainTex = TextureHelper.TrimTexture(mainTex, bound, studio.triming.margin, EngineConstants.CLEAR_COLOR32);
                            if (studio.output.toMakeNormalMap)
                                normalTex = TextureHelper.TrimTexture(normalTex, bound, studio.triming.margin, EngineConstants.NORMALMAP_COLOR32, true);

                            if (isPrefabCompactCollider)
                                frameCompactVectors[fi] = CalcCompactVector(mainTex, bound, frameCompactBounds[fi]);

                            if (locationMappings != null)
                            {
                                foreach (LocationMapping locMapping in locationMappings)
                                    locMapping.frameLocationPositions[fi].SubtractWithMargin(bound.min, studio.triming.margin);
                            }

                            frameMainTextures[fi] = mainTex;
                            if (studio.output.toMakeNormalMap)
                                frameNormalTextures[fi] = normalTex;
                        }
                    }

                    if (studio.packing.on || studio.triming.IsOnUniformSize())
                    {
                        framePivots[fi] = new PixelVector(pivot);
                        frameMainTextures[fi] = mainTex;
                        if (studio.output.toMakeNormalMap)
                            frameNormalTextures[fi] = normalTex;
                    }
                    else
                    {
                        string mtrlFullName = BakeIndividually(ref mainTex, pivot, viewName, fi);
                        if (studio.output.toMakeNormalMap)
                            BakeIndividually(ref normalTex, pivot, viewName, fi, true);
                        CreateMaterial(mtrlFullName, mainTex, normalTex);
                    }
                }

                if (!studio.packing.on && studio.triming.IsOnUniformSize())
                {
                    TrimToUniformSize(framePivots, frameMainTextures, frameNormalTextures);
                    BakeIndividually_Group(framePivots, viewName, frameMainTextures, frameNormalTextures);
                }
                else if (studio.packing.on)
                {
                    if (studio.triming.IsOnUniformSize())
                    {
                        TrimToUniformSize(framePivots, frameMainTextures, frameNormalTextures);

                        if (isPrefabCompactCollider)
                        {
                            for (int fi = 0; fi < framesForView.Length; ++fi)
                                frameCompactVectors[fi] = CalcCompactVector(frameMainTextures[fi], uniformBound, frameCompactBounds[fi]);
                        }

                        if (locationMappings != null)
                        {
                            foreach (LocationMapping locMapping in locationMappings)
                            {
                                for (int fi = 0; fi < framesForView.Length; ++fi)
                                    locMapping.frameLocationPositions[fi].SubtractWithMargin(uniformBound.min, studio.triming.margin);
                            }
                        }
                    }

                    Sprite[] sprites = BakeWithPacking(framePivots, viewName, frameMainTextures, frameNormalTextures);

                    if (locationMappings != null)
                    {
                        foreach (LocationMapping locMapping in locationMappings)
                        {
                            Debug.Assert(frameMainTextures.Length == locMapping.frameLocationPositions.Length);

                            for (int fi = 0; fi < framesForView.Length; ++fi)
                            {
                                Texture2D tex = frameMainTextures[fi];
                                float locRatioX = (float)locMapping.frameLocationPositions[fi].x / (float)tex.width;
                                float locRatioY = (float)locMapping.frameLocationPositions[fi].y / (float)tex.height;
                                float pivotRatioX = (float)framePivots[fi].x / (float)tex.width;
                                float pivotRatioY = (float)framePivots[fi].y / (float)tex.height;
                                locMapping.frameRatioPositions[fi] = new Vector2(locRatioX - pivotRatioX, locRatioY - pivotRatioY);
                            }
                        }
                    }

                    if (studio.output.toMakeAnimationClip)
                    {
                        MeshAnimation meshAnim = meshAnimations[AnimationIndex];
                        AnimationClip animClip = MakeAnimationClipsForView(meshAnim.isLooping, sprites, viewName);

                        if (animClip != null && outRootPrefab != null)
                        {
                            BoxCollider2D collider = model.prefabBuilder.GetBoxCollider2D(outRootPrefab);
                            if (collider != null)
                                AddBoxColliderCurve(animClip, collider, sprites);

                            if (locationMappings != null)
                                AddLocationPositionCurve(animClip, sprites);
                        }
                    }
                }

                studio.extraction.com.Clear();

                if (studio.output.toMakeNormalMap)
                    CapturingHelper.ClearNormalPixelsLists();

                viewIndex++;

                if (viewIndex < studio.view.checkedSubViews.Count)
                    stateMachine.ChangeState(BakingState.BeginView);
                else
                    stateMachine.ChangeState(BakingState.EndAnimation);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnEndAnimation()
        {
            try
            {
                EditorUtility.ClearProgressBar();

                AnimationIndex++;

                if (AnimationIndex < meshAnimations.Count)
                    stateMachine.ChangeState(BakingState.BeginAnimation);
                else
                    stateMachine.ChangeState(BakingState.Finalize);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnFinalize()
        {
            try
            {
                stateMachine = null;

                if (outRootPrefab != null)
                {
                    if (meshModel.outputController != null)
                        model.prefabBuilder.BindController(outRootPrefab, meshModel.outputController);
                    if (firstSprite != null)
                        model.prefabBuilder.BindFirstSprite(outRootPrefab, firstSprite);
                    if (firstMaterial != null)
                        model.prefabBuilder.BindFirstMaterial(outRootPrefab, firstMaterial);

                    SaveAsPrefab(outRootPrefab, fileNameForModel);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                Finish();
            }
        }

        public override void Finish()
        {
            stateMachine = null;

            if (!EditorApplication.isPlaying)
                meshModel.Simulate(meshAnimations[0], Frame.BEGIN);
        }

        private void BuildAnimatorController()
        {
            if (meshModel.referenceController == null)
                return;

            animatorStates = null;

            MeshAnimation meshAnim = meshAnimations[AnimationIndex];

            if (meshModel.outputController == null || isMultiModel || meshAnimations.Count >= 2)
            {
                string filePath = Path.Combine(folderPath, fileNameForModel + ".controller");
                meshModel.outputController = AnimatorController.CreateAnimatorControllerAtPath(filePath);
            }
            AnimatorStateMachine outStateMachine = meshModel.outputController.layers[0].stateMachine;

            AddParameterIfNotExist(meshModel.outputController, ANGLE_PARAM_NAME, AnimatorControllerParameterType.Int);
            foreach (AnimatorControllerParameter refParam in meshModel.referenceController.parameters)
                AddParameterIfNotExist(meshModel.outputController, refParam.name, refParam.type);

            AnimatorStateMachine refStateMachine = meshModel.referenceController.layers[0].stateMachine;

            AnimatorState refMainAnimState = FindState(refStateMachine, meshAnim.stateName);
            Debug.Assert(refMainAnimState != null);

            animatorStates = new List<AnimatorState>();
            foreach (SubView subView in studio.view.checkedSubViews)
            {
                AnimatorStateMachine outSubStateMachine = GetOrCreateStateMachine(outStateMachine, subView.name, subView.angle);
                AnimatorState outStateForView = GetOrCreateState(outSubStateMachine, meshAnim.stateName);
                CopyState(refMainAnimState, outStateForView);
                animatorStates.Add(outStateForView);
            }
            Debug.Assert(studio.view.checkedSubViews.Count == animatorStates.Count);

            bool hasAnyStateTransition = false;
            foreach (SubView subView in studio.view.checkedSubViews)
            {
                AnimatorStateMachine outViewStateMachine = FindStateMachine(outStateMachine, subView.name);
                Debug.Assert(outViewStateMachine != null);

                AnimatorState outMainAnimViewState = FindState(outViewStateMachine, meshAnim.stateName);
                Debug.Assert(outMainAnimViewState != null);

                foreach (AnimatorTransition refEntryTransition in refStateMachine.entryTransitions)
                {
                    AnimatorState outDestState = FindState(outViewStateMachine, refEntryTransition.destinationState.name);
                    if (outDestState != null)
                        CopyOrCreateEntryTransition(outViewStateMachine, refEntryTransition, outDestState);
                }

                foreach (AnimatorStateTransition refAnyStateTransition in refStateMachine.anyStateTransitions)
                {
                    AnimatorState outDestState = FindState(outViewStateMachine, refAnyStateTransition.destinationState.name);
                    if (outDestState != null)
                    {
                        CopyOrCreateAnyStateTransition(outStateMachine, refAnyStateTransition, outDestState, subView.angle);
                        if (refAnyStateTransition.destinationState.name == meshAnim.stateName)
                            hasAnyStateTransition = true;
                    }
                }

                foreach (ChildAnimatorState refChildState in refStateMachine.states)
                {
                    foreach (AnimatorStateTransition refTransition in refChildState.state.transitions)
                    {
                        AnimatorState outStartState = FindState(outViewStateMachine, refChildState.state.name);
                        if (outStartState == null)
                            continue;

                        if (refTransition.isExit)
                        {
                            CopyOrCreateExitTransition(refTransition, outStartState);
                        }
                        else
                        {
                            AnimatorState outDestState = FindState(outViewStateMachine, refTransition.destinationState.name);
                            if (outDestState != null)
                            {
                                CopyOrCreateTransition(refTransition, outStartState, outDestState);

                                foreach (AnimatorStateTransition refReverseTransition in refTransition.destinationState.transitions)
                                {
                                    if (refReverseTransition.destinationState == refMainAnimState)
                                        CopyOrCreateTransition(refReverseTransition, outDestState, outStartState);
                                }
                            }
                        }
                    }
                }
            }

            if (!hasAnyStateTransition)
            {
                for (int ai = 0; ai < animatorStates.Count; ++ai)
                {
                    for (int bi = 0; bi < animatorStates.Count; ++bi)
                    {
                        if (ai == bi)
                            continue;
                        AddDirectionTransitionA2BIfNotExist(animatorStates[ai], animatorStates[bi], studio.view.checkedSubViews[bi].angle);
                    }
                }
            }
        }

        private void CopyState(AnimatorState refState, AnimatorState outState)
        {
            outState.mirrorParameterActive = refState.mirrorParameterActive;
            outState.cycleOffsetParameterActive = refState.cycleOffsetParameterActive;
            outState.speedParameterActive = refState.speedParameterActive;
            outState.mirrorParameter = refState.mirrorParameter;
            outState.cycleOffsetParameter = refState.cycleOffsetParameter;
            outState.speedParameter = refState.speedParameter;
            outState.tag = refState.tag;
            outState.writeDefaultValues = refState.writeDefaultValues;
            outState.iKOnFeet = refState.iKOnFeet;
            outState.mirror = refState.mirror;
            outState.cycleOffset = refState.cycleOffset;
            outState.speed = refState.speed;
        }

        private void CopyOrCreateEntryTransition(AnimatorStateMachine outStateMachine, AnimatorTransition refTransition, AnimatorState outState)
        {
            AnimatorTransition outTransition = FindEntryTransition(outStateMachine, outState);
            if (outTransition == null)
                outTransition = outStateMachine.AddEntryTransition(outState);
            outTransition.solo = refTransition.solo;
            outTransition.mute = refTransition.mute;
            outTransition.isExit = refTransition.isExit;

            foreach (AnimatorCondition refCondition in refTransition.conditions)
                RemoveAllAndAddCondition(outTransition, refCondition.parameter, refCondition.mode, refCondition.threshold);
        }

        private AnimatorTransition FindEntryTransition(AnimatorStateMachine outStateMachine, AnimatorState outState)
        {
            foreach (AnimatorTransition outEntryTransition in outStateMachine.entryTransitions)
            {
                if (outEntryTransition.destinationState == outState)
                    return outEntryTransition;
            }
            return null;
        }

        private void CopyOrCreateAnyStateTransition(AnimatorStateMachine outStateMachine, AnimatorStateTransition refTransition, AnimatorState outState, int angle)
        {
            AnimatorStateTransition outTransition = FindAnyStateTransition(outStateMachine, outState);
            if (outTransition == null)
            {
                outTransition = outStateMachine.AddAnyStateTransition(outState);
                outTransition.AddCondition(AnimatorConditionMode.Equals, angle, ANGLE_PARAM_NAME);
            }
            CopyStateTransition(refTransition, outTransition);
        }

        private AnimatorStateTransition FindAnyStateTransition(AnimatorStateMachine outStateMachine, AnimatorState outState)
        {
            foreach (AnimatorStateTransition outAnyStateTransition in outStateMachine.anyStateTransitions)
            {
                if (outAnyStateTransition.destinationState == outState)
                    return outAnyStateTransition;
            }
            return null;
        }

        private void CopyOrCreateExitTransition(AnimatorStateTransition refTransition, AnimatorState outState)
        {
            AnimatorStateTransition outTransition = FindExitTransition(outState);
            if (outTransition == null)
                outTransition = outState.AddExitTransition();
            CopyStateTransition(refTransition, outTransition);
        }

        private AnimatorStateTransition FindExitTransition(AnimatorState outState)
        {
            foreach (AnimatorStateTransition outTransition in outState.transitions)
            {
                if (outTransition.isExit)
                    return outTransition;
            }
            return null;
        }

        private void CopyOrCreateTransition(AnimatorStateTransition refTransition, AnimatorState outStartState, AnimatorState outEndState)
        {
            AnimatorStateTransition outTransition = FindTransitionA2B(outStartState, outEndState);
            if (outTransition == null)
                outTransition = outStartState.AddTransition(outEndState);
            CopyStateTransition(refTransition, outTransition);
        }

        private void CopyStateTransition(AnimatorStateTransition refTransition, AnimatorStateTransition outTransition)
        {
            outTransition.solo = refTransition.solo;
            outTransition.mute = refTransition.mute;
            outTransition.isExit = refTransition.isExit;
            outTransition.duration = refTransition.duration;
            outTransition.offset = refTransition.offset;
            outTransition.interruptionSource = refTransition.interruptionSource;
            outTransition.orderedInterruption = refTransition.orderedInterruption;
            outTransition.exitTime = refTransition.exitTime;
            outTransition.hasExitTime = refTransition.hasExitTime;
            outTransition.hasFixedDuration = refTransition.hasFixedDuration;
            outTransition.canTransitionToSelf = refTransition.canTransitionToSelf;

            foreach (AnimatorCondition refCondition in refTransition.conditions)
                RemoveAllAndAddCondition(outTransition, refCondition.parameter, refCondition.mode, refCondition.threshold);
        }

        private AnimatorStateMachine GetOrCreateStateMachine(AnimatorStateMachine stateMachine, string stateMachineName, int angle)
        {
            AnimatorStateMachine subStateMachine = FindStateMachine(stateMachine, stateMachineName);
            if (subStateMachine == null)
                subStateMachine = stateMachine.AddStateMachine(stateMachineName, new Vector3(450 + Mathf.Cos(angle * Mathf.Deg2Rad) * 200, Mathf.Sin(angle * Mathf.Deg2Rad) * 150));
            return subStateMachine;
        }

        private AnimatorStateMachine FindStateMachine(AnimatorStateMachine stateMachine, string stateMachineName)
        {
            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
            {
                if (childStateMachine.stateMachine.name == stateMachineName)
                    return childStateMachine.stateMachine;
            }
            return null;
        }

        private Vector2 CalcCompactVector(Texture2D tex, PixelBound bound, PixelBound compactBound)
        {
            compactBound.min.SubtractWithMargin(bound.min, studio.triming.margin);
            compactBound.max.SubtractWithMargin(bound.min, studio.triming.margin);

            float textureCenterX = tex.width / 2f;
            float textureCenterY = tex.height / 2f;
            float compactCenterX = (float)(compactBound.min.x + compactBound.max.x) / 2f;
            float compactCenterY = (float)(compactBound.min.y + compactBound.max.y) / 2f;
            float ratioVectorX = (compactCenterX - textureCenterX) / tex.width;
            float ratioVectorY = (compactCenterY - textureCenterY) / tex.height;

            return new Vector2(ratioVectorX, ratioVectorY);
        }

        private void AddBoxColliderCurve(AnimationClip animClip, BoxCollider2D collider, Sprite[] sprites)
        {
            Debug.Assert(framePivots != null && sprites.Length == framePivots.Length);

            AnimationCurve xSizeCurve = new AnimationCurve();
            AnimationCurve ySizeCurve = new AnimationCurve();
            AnimationCurve xOffsetCurve = new AnimationCurve();
            AnimationCurve yOffsetCurve = new AnimationCurve();

            for (int i = 0; i < sprites.Length; ++i)
            {
                float unitTime = 1f / animClip.frameRate;
                float time = studio.output.frameInterval * i * unitTime;

                Texture2D mainTex = frameMainTextures[i];

                // size curve
                Vector3 spriteSize = sprites[i].bounds.size;
                if (isPrefabCompactCollider)
                {
                    int compactWidth = frameCompactBounds[i].max.x - frameCompactBounds[i].min.x + 1;
                    int compactHeight = frameCompactBounds[i].max.y - frameCompactBounds[i].min.y + 1;
                    float widthRatio = (float)compactWidth / (float)mainTex.width;
                    float heightRatio = (float)compactHeight / (float)mainTex.height;
                    spriteSize.x *= widthRatio;
                    spriteSize.y *= heightRatio;
                }
                xSizeCurve.AddKey(time, spriteSize.x);
                ySizeCurve.AddKey(time, spriteSize.y);

                // offset curve
                float xPivotRatio = (float)framePivots[i].x / (float)mainTex.width;
                float yPivotRatio = (float)framePivots[i].y / (float)mainTex.height;
                float xCenterRatio = 0.5f;
                float yCenterRatio = 0.5f;
                if (frameCompactVectors != null)
                {
                    xCenterRatio += frameCompactVectors[i].x;
                    yCenterRatio += frameCompactVectors[i].y;
                }
                float offsetX = (xCenterRatio - xPivotRatio) * sprites[i].bounds.size.x;
                float offsetY = (yCenterRatio - yPivotRatio) * sprites[i].bounds.size.y;
                xOffsetCurve.AddKey(time, offsetX);
                yOffsetCurve.AddKey(time, offsetY);

                // setup for firstSprite
                if (!wasColliderSetup && i == 0)
                {
                    wasColliderSetup = true;
                    collider.size = new Vector2(spriteSize.x, spriteSize.y);
                    collider.offset = new Vector2(offsetX, offsetY);
                }
            }

            string path = AnimationUtility.CalculateTransformPath(collider.transform, outRootPrefab.transform);

            EditorCurveBinding xSizeCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(BoxCollider2D), "m_Size.x");
            EditorCurveBinding ySizeCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(BoxCollider2D), "m_Size.y");
            EditorCurveBinding xOffsetCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(BoxCollider2D), "m_Offset.x");
            EditorCurveBinding yOffsetCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(BoxCollider2D), "m_Offset.y");

            AnimationUtility.SetEditorCurve(animClip, xSizeCurveBinding, xSizeCurve);
            AnimationUtility.SetEditorCurve(animClip, ySizeCurveBinding, ySizeCurve);
            AnimationUtility.SetEditorCurve(animClip, xOffsetCurveBinding, xOffsetCurve);
            AnimationUtility.SetEditorCurve(animClip, yOffsetCurveBinding, yOffsetCurve);
        }

        private void AddLocationPositionCurve(AnimationClip animClip, Sprite[] sprites)
        {
            foreach (LocationMapping locMapping in locationMappings)
            {
                Debug.Assert(locMapping.frameRatioPositions.Length == sprites.Length);

                AnimationCurve xCurve = new AnimationCurve();
                AnimationCurve yCurve = new AnimationCurve();

                for (int i = 0; i < sprites.Length; ++i)
                {
                    float unitTime = 1f / animClip.frameRate;
                    float time = studio.output.frameInterval * i * unitTime;

                    Vector3 spriteSize = sprites[i].bounds.size;
                    float x = spriteSize.x * locMapping.frameRatioPositions[i].x;
                    float y = spriteSize.y * locMapping.frameRatioPositions[i].y;

                    xCurve.AddKey(time, x);
                    yCurve.AddKey(time, y);

                    if (i == 0)
                        locMapping.location2dObj.transform.localPosition = new Vector3(x, y);
                }

                string path = AnimationUtility.CalculateTransformPath(locMapping.location2dObj.transform, outRootPrefab.transform);

                EditorCurveBinding xCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x");
                EditorCurveBinding yCurveBinding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y");

                AnimationUtility.SetEditorCurve(animClip, xCurveBinding, xCurve);
                AnimationUtility.SetEditorCurve(animClip, yCurveBinding, yCurve);
            }
        }
    }
}
