using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ABS
{
    public class RuntimeBaker : Baker
    {
        private readonly List<Frame> selectedFrames;

        private int idxOfSelFrames = -1;

        private Texture2D[] frameNormalTextures = null;

        public RuntimeBaker(Model model, Studio studio, string parentFolderPath)
            : base(model, studio, "", parentFolderPath)
        {
            selectedFrames = studio.sampling.selectedFrames;

            stateMachine = new StateMachine<BakingState>();
            stateMachine.AddState(BakingState.Initialize, OnInitialize);
            stateMachine.AddState(BakingState.BeginFrame, OnBeginFrame);
            stateMachine.AddState(BakingState.EndFrame, OnEndFrame);
            stateMachine.AddState(BakingState.Finalize, OnFinalize);

            stateMachine.ChangeState(BakingState.Initialize);
        }

        public void OnInitialize()
        {
            try
            {
                if (studio.triming.isUniformSize)
                    uniformBound = new PixelBound();

                frameMainTextures = new Texture2D[selectedFrames.Count];
                if (studio.output.toMakeNormalMap)
                {
                    frameNormalTextures = new Texture2D[selectedFrames.Count];
                    CapturingHelper.SetupNormalPixelsLists();
                }

                if (studio.packing.on || studio.triming.IsOnUniformSize())
                    framePivots = new PixelVector[selectedFrames.Count];

                Vector3 pivotScreenPos = Camera.main.WorldToScreenPoint(model.GetPivotPosition());
                currFramePivot = new PixelVector(pivotScreenPos);

                idxOfSelFrames = 0;

                fileBaseName = BuildFileBaseName();
                BuildFolderPathAndCreate(modelName);

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
                int numOfSelFrames = idxOfSelFrames + 1;
                float progress = (float)numOfSelFrames / (float)selectedFrames.Count;
                int frameIndex = selectedFrames[idxOfSelFrames].index;
                int frameNumber = frameIndex + 1;
                IsCancelled = EditorUtility.DisplayCancelableProgressBar("Baking...", "Frame: " + frameNumber + " (" + ((int)(progress * 100f)) + "%)", progress);

                if (IsCancelled)
                    throw new Exception("Cancelled");

                Texture2D mainTex = studio.sampling.rawMainTextures[frameIndex];
                Texture2D normalTex = null;
                if (studio.output.toMakeNormalMap)
                    normalTex = studio.sampling.rawNormalTextures[frameIndex];

                PixelBound bound = new PixelBound();
                PixelBound compactBound = new PixelBound();
                if (!TextureHelper.GetPixelBound(mainTex, currFramePivot, bound, compactBound))
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
                        mainTex = TextureHelper.TrimTexture(mainTex, bound, studio.triming.margin, EngineConstants.CLEAR_COLOR32);
                        if (studio.output.toMakeNormalMap)
                            normalTex = TextureHelper.TrimTexture(normalTex, bound, studio.triming.margin, EngineConstants.NORMALMAP_COLOR32);
                    }
                }

                if (studio.packing.on || studio.triming.IsOnUniformSize())
                {
                    frameMainTextures[idxOfSelFrames] = mainTex;
                    if (studio.output.toMakeNormalMap)
                        frameNormalTextures[idxOfSelFrames] = normalTex;
                    framePivots[idxOfSelFrames] = pivot;
                }
                else // !studio.packing.on && !studio.trim.IsOnUniformSize()
                {
                    BakeIndividually(ref mainTex, pivot, studio.appliedSubViewName, idxOfSelFrames, studio.output.toMakeNormalMap);
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
                idxOfSelFrames++;

                if (idxOfSelFrames < selectedFrames.Count)
                    stateMachine.ChangeState(BakingState.BeginFrame);
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

                if (!studio.packing.on && studio.triming.IsOnUniformSize())
                {
                    TrimToUniformSize(framePivots, frameMainTextures, frameNormalTextures);

                    for (int i = 0; i < selectedFrames.Count; i++)
                    {
                        int frameIndex = selectedFrames[i].index;

                        BakeIndividually(ref frameMainTextures[i], framePivots[i], studio.appliedSubViewName, frameIndex);
                        if (studio.output.toMakeNormalMap)
                            BakeIndividually(ref frameNormalTextures[i], framePivots[i], studio.appliedSubViewName, frameIndex, true);
                    }
                }
                else if (studio.packing.on)
                {
                    Sprite[] sprites = null;
                    if (!studio.triming.isUniformSize)
                    {
                        // trimmed or not
                        sprites = BakeWithPacking(framePivots, studio.appliedSubViewName, frameMainTextures, frameNormalTextures);
                    }
                    else
                    {
                        TrimToUniformSize(framePivots, frameMainTextures, frameNormalTextures);
                        sprites = BakeWithPacking(framePivots, studio.appliedSubViewName, frameMainTextures, frameNormalTextures);
                    }

                    if (studio.output.toMakeAnimationClip)
                        MakeAnimationClipsForView(true, sprites, studio.appliedSubViewName);
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
    }
}
