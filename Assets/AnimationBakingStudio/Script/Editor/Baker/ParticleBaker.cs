using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ABS
{
	public class ParticleBaker : Baker
    {
        private readonly ParticleModel particleModel = null;

        private AnimatorController outputController;

        private Projectile projectile;

        public ParticleBaker(Model model, Studio studio, string sIndex, string parentFolderPath)
            : base(model, studio, sIndex, parentFolderPath)
        {
            particleModel = model as ParticleModel;

            stateMachine = new StateMachine<BakingState>();
            stateMachine.AddState(BakingState.Initialize, OnInitialize);
            stateMachine.AddState(BakingState.BeginView, OnBeginView);
            stateMachine.AddState(BakingState.BeginFrame, OnBeginFrame);
            stateMachine.AddState(BakingState.CaptureFrame, OnCaptureFrame);
            stateMachine.AddState(BakingState.EndFrame, OnEndFrame);
            stateMachine.AddState(BakingState.EndView, OnEndView);
            stateMachine.AddState(BakingState.Finalize, OnFinalize);

            stateMachine.ChangeState(BakingState.Initialize);
        }

        public void OnInitialize()
        {
            try
            {
                if (studio.view.rotationType == RotationType.Model)
                    CameraHelper.LocateMainCameraToModel(model, studio);

                if (!isMultiModel && studio.sampling != null)
                {
                    List<Frame> selectedFrames = studio.sampling.selectedFrames;
                    Debug.Assert(selectedFrames.Count > 0);
                    framesForView = new Frame[selectedFrames.Count];
                    selectedFrames.CopyTo(framesForView);
                }
                else
                {
                    framesForView = new Frame[studio.filming.numOfFrames];
                    for (int i = 0; i < framesForView.Length; ++i)
                        framesForView[i] = new Frame(i, 0);
                }

                fileBaseName = BuildFileBaseName();
                BuildFolderPathAndCreate(modelName);

                if (studio.packing.on && studio.output.toMakeAnimatorController)
                    BuildAnimatorController();

                if (EditorApplication.isPlaying)
                    frameInterval = particleModel.duration / studio.filming.numOfFrames;

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
                    particleModel.StopAndPlay();

                if (EditorApplication.isPlaying && particleModel.isProjectile)
                {
                    projectile = particleModel.gameObject.AddComponent<Projectile>();
                    projectile.MovingVector = particleModel.projectileVector;
                }

                if (studio.triming.IsOnUniformSize())
                    uniformBound = new PixelBound();

                frameMainTextures = new Texture2D[framesForView.Length];

                if (studio.packing.on || studio.triming.IsOnUniformSize())
                    framePivots = new PixelVector[framesForView.Length];

                studio.view.checkedSubViews[viewIndex].func(model);
                viewName = studio.view.checkedSubViews[viewIndex].name;

                Vector3 screenPivotPos = Camera.main.WorldToScreenPoint(model.GetPivotPosition());
                currFramePivot = new PixelVector(screenPivotPos);

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
                    particleModel.Simulate(frameIndex == 0, studio.filming.numOfFrames);

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

                CapturingHelper.ReadAndKeepRawPixelsManagingShadow(model, studio);

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
                    Texture2D tex = CapturingHelper.ExtractValidPixelsForMain_OneFrame(fi, studio);

                    PixelBound bound = new PixelBound();
                    if (!TextureHelper.GetPixelBound(tex, currFramePivot, bound))
                    {
                        bound.min.x = currFramePivot.x - 1;
                        bound.max.x = currFramePivot.x + 1;
                        bound.min.y = currFramePivot.y - 1;
                        bound.max.y = currFramePivot.y + 1;
                    }

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
                            tex = TextureHelper.TrimTexture(tex, bound, studio.triming.margin, EngineConstants.CLEAR_COLOR32);

                            frameMainTextures[fi] = tex;
                        }
                    }

                    if (studio.packing.on || studio.triming.IsOnUniformSize())
                        framePivots[fi] = pivot;
                    else // !studio.packing.on && !studio.trim.isUniformSize
                        BakeIndividually(ref tex, pivot, viewName, fi);
                }

                if (!studio.packing.on && studio.triming.IsOnUniformSize())
                {
                    TrimToUniformSize(framePivots, frameMainTextures);
                    BakeIndividually_Group(framePivots, viewName, frameMainTextures);
                }
                else if (studio.packing.on)
                {
                    Sprite[] sprites = null;
                    if (studio.triming.IsOnUniformSize())
                        TrimToUniformSize(framePivots, frameMainTextures);

                    sprites = BakeWithPacking(framePivots, viewName, frameMainTextures);

                    if (studio.output.toMakeAnimationClip)
                        MakeAnimationClipsForView(particleModel.isLooping, sprites, viewName);
                }

                studio.extraction.com.Clear();

                if (EditorApplication.isPlaying && particleModel.isProjectile)
                    GameObject.Destroy(projectile);

                viewIndex++;

                if (viewIndex < studio.view.checkedSubViews.Count)
                    stateMachine.ChangeState(BakingState.BeginView);
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

                if (IsToMakePrefab())
                {
                    GameObject obj = PrefabUtility.InstantiatePrefab(model.spritePrefab) as GameObject;
                    if (obj != null)
                    {
                        if (outputController != null)
                            model.prefabBuilder.BindController(obj, outputController);
                        if (firstSprite != null)
                            model.prefabBuilder.BindFirstSprite(obj, firstSprite);
                        if (firstMaterial != null)
                            model.prefabBuilder.BindFirstMaterial(obj, firstMaterial);

                        SaveAsPrefab(obj, fileBaseName);
                    }
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

        private void BuildAnimatorController()
        {
            animatorStates = null;

            string filePath = Path.Combine(folderPath, particleModel.name + ".controller");
            outputController = AnimatorController.CreateAnimatorControllerAtPath(filePath);

            AnimatorStateMachine stateMachine = outputController.layers[0].stateMachine;

            AddParameterIfNotExist(outputController, ANGLE_PARAM_NAME, AnimatorControllerParameterType.Int);

            animatorStates = new List<AnimatorState>();
            foreach (SubView subView in studio.view.checkedSubViews)
            {
                AnimatorState state = GetOrCreateState(stateMachine, subView.name);
                animatorStates.Add(state);
            }

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
}
