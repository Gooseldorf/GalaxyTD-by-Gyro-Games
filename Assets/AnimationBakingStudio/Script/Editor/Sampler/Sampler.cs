using System;
using UnityEngine;
using UnityEditor;

namespace ABS
{
    public abstract class Sampler
    {
        private readonly Model model = null;
        protected readonly Studio studio = null;

        public CompletionCallback completion;

        public enum SamplingState
        {
            Initialize = 0,
            BeginFrame,
            CaptureFrame,
            EndFrame,
            Finalize
        }
        private StateMachine<SamplingState> stateMachine;

        private float frameInterval;
        protected int frameIndex = 0;

        private double prevTime = 0.0;

        public bool IsCancelled { get; set; }

        public Sampler(Model model, Studio studio)
        {
            this.model = model;
            this.studio = studio;
        }

        public void SampleFrames(CompletionCallback completion)
        {
            this.completion = completion;

            EditorApplication.update -= UpdateState;
            EditorApplication.update += UpdateState;

            stateMachine = new StateMachine<SamplingState>();
            stateMachine.AddState(SamplingState.Initialize, OnInitialize);
            stateMachine.AddState(SamplingState.BeginFrame, OnBeginFrame);
            stateMachine.AddState(SamplingState.CaptureFrame, OnCaptureFrame);
            stateMachine.AddState(SamplingState.EndFrame, OnEndFrame);
            stateMachine.AddState(SamplingState.Finalize, OnFinalize);

            stateMachine.ChangeState(SamplingState.Initialize);
        }

        public void UpdateState()
        {
            if (stateMachine != null)
                stateMachine.Update();
        }

        public void OnInitialize()
        {
            try
            {
                studio.extraction.com.Setup(studio.filming.numOfFrames);

                Initialize_More();

                if (EditorApplication.isPlaying)
                {
                    StopAndPlayModel();
                    frameInterval = GetFrameInterval();
                }

                frameIndex = 0;

                stateMachine.ChangeState(SamplingState.BeginFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        protected virtual void Initialize_More() { }

        protected abstract void StopAndPlayModel();

        protected abstract float GetFrameInterval();

        public void OnBeginFrame()
		{
            try
            {
                int shownCurrFrameNumber = frameIndex + 1;
                float progress = (float)shownCurrFrameNumber / studio.filming.numOfFrames;

                IsCancelled = EditorUtility.DisplayCancelableProgressBar("Sampling...", "Frame: " + shownCurrFrameNumber + " (" + ((int)(progress * 100f)) + "%)", progress);
                if (IsCancelled)
                    throw new Exception("Cancelled");

                if (!EditorApplication.isPlaying)
                    SimulateModel();

                stateMachine.ChangeState(SamplingState.CaptureFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        protected abstract void SimulateModel();

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

                OnCaptureFrame_();

                stateMachine.ChangeState(SamplingState.EndFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        protected abstract void OnCaptureFrame_();

        public void OnEndFrame()
        {
            try
            {
                frameIndex++;

                if (frameIndex < studio.filming.numOfFrames)
                    stateMachine.ChangeState(SamplingState.BeginFrame);
                else
                    stateMachine.ChangeState(SamplingState.Finalize);
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

                int numOfFrames = studio.filming.numOfFrames;
                studio.sampling = new Sampling(numOfFrames);

                CreateSampling_More(numOfFrames);

                Vector3 pivotScreenPos = Camera.main.WorldToScreenPoint(model.GetPivotPosition());
                PixelVector pivot = new PixelVector(pivotScreenPos);

                PixelBound uniformBound = new PixelBound();

                for (int fi = 0; fi < numOfFrames; ++fi)
                {
                    Texture2D tex = CapturingHelper.ExtractValidPixelsForMain_OneFrame(fi, studio);
                    studio.sampling.rawMainTextures[fi] = tex;
                    ExtractValidPixels_More(fi);

                    PixelBound bound = new PixelBound();
                    if (!TextureHelper.GetPixelBound(tex, pivot, bound))
                    {
                        bound.min.x = pivot.x - 1;
                        bound.max.x = pivot.x + 1;
                        bound.min.y = pivot.y - 1;
                        bound.max.y = pivot.y + 1;
                    }

                    TextureHelper.ExpandBound(bound, uniformBound);
                }

                const int MIN_LENGTH = 200;

                for (int fi = 0; fi < numOfFrames; ++fi)
                {
                    Texture2D rawTex = studio.sampling.rawMainTextures[fi];
                    Texture2D trimTex = TextureHelper.TrimTexture(rawTex, uniformBound, 5, EngineConstants.CLEAR_COLOR32);

                    if (trimTex.width >= trimTex.height && trimTex.width > MIN_LENGTH)
                    {
                        float ratio = (float)trimTex.height / (float)trimTex.width;
                        int newTrimWidth = MIN_LENGTH;
                        int newTrimHeight = Mathf.RoundToInt(newTrimWidth * ratio);
                        trimTex = TextureHelper.ScaleTexture(trimTex, newTrimWidth, newTrimHeight);
                    }
                    else if (trimTex.width < trimTex.height && trimTex.height > MIN_LENGTH)
                    {
                        float ratio = (float)trimTex.width / (float)trimTex.height;
                        int newTrimHeight = MIN_LENGTH;
                        int newTrimWidth = Mathf.RoundToInt(newTrimHeight * ratio);
                        trimTex = TextureHelper.ScaleTexture(trimTex, newTrimWidth, newTrimHeight);
                    }

                    studio.sampling.trimMainTextures[fi] = trimTex;

                    float frameTime = 0;
                    if (EditorApplication.isPlaying)
                        frameTime = frameInterval * fi;
                    else // Editor Mode
                        frameTime = GetFrameTime(fi);
                    studio.sampling.frameTimes[fi] = frameTime;
                    studio.sampling.selectedFrames.Add(new Frame(fi, frameTime));
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

        protected virtual void CreateSampling_More(int numOfFrames) { }

        protected virtual void ExtractValidPixels_More(int frameIndex) { }

        protected virtual float GetFrameTime(int fi) { return 0; }

        public void Finish()
        {
            stateMachine = null;

            EditorApplication.update -= UpdateState;

            EditorUtility.ClearProgressBar();

            studio.extraction.com.Clear();

            Finish_More();

            completion();
        }

        protected virtual void Finish_More() { }
    }
}
