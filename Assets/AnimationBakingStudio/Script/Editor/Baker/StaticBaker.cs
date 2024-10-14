using System;
using UnityEngine;
using UnityEditor;

namespace ABS
{
	public class StaticBaker : Baker
    {
        private readonly MeshModel meshModel = null;

        private class BakingData
        {
            public string name;
            public PixelVector pivot;
            public Texture2D mainTex;
            public Texture2D normalTex;

            public BakingData(string name, PixelVector pivot, Texture2D mainTex, Texture2D normalTex = null)
            {
                this.name = name;
                this.pivot = pivot;
                this.mainTex = mainTex;
                this.normalTex = normalTex;
            }
        }

        private BakingData[] bakingDataList = null;

        public StaticBaker(Model model, Studio studio, string sIndex, string parentFolderPath)
            : base(model, studio, sIndex, parentFolderPath)
        {
            meshModel = model as MeshModel;

            stateMachine = new StateMachine<BakingState>();
            stateMachine.AddState(BakingState.Initialize, OnInitialize);
            stateMachine.AddState(BakingState.BeginView, OnBeginView);
            stateMachine.AddState(BakingState.CaptureFrame, OnCaptureFrame);
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

                ShadowHelper.LocateShadowToModel(model, studio);

                if (studio.shadow.type == ShadowType.Simple)
                {
                    ShadowHelper.ScaleSimpleShadow(model, studio);
                }
                else if (studio.shadow.type == ShadowType.TopDown)
                {
                    Camera camera;
                    GameObject fieldObj;
                    ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);

                    CameraHelper.LookAtModel(camera.transform, model);
                    ShadowHelper.ScaleShadowField(camera, fieldObj);
                }
                else if (studio.shadow.type == ShadowType.Matte)
                {
                    ShadowHelper.ScaleMatteField(meshModel, studio.shadow.obj, studio.lit);
                }

                if (studio.packing.on || studio.triming.IsOnUniformSize())
                    bakingDataList = new BakingData[studio.view.checkedSubViews.Count];

                if (studio.triming.IsOnUniformSize())
                    uniformBound = new PixelBound();

                fileBaseName = BuildFileBaseName();
                BuildFolderPathAndCreate(modelName);

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
                int viewIndexForProgress = viewIndex + 1;
                float progress = (float)viewIndexForProgress / (float)studio.view.checkedSubViews.Count;

                if (studio.view.checkedSubViews.Count == 0)
                    IsCancelled = EditorUtility.DisplayCancelableProgressBar("Progress...", " (" + ((int)(progress * 100f)) + "%)", progress);
                else
                    IsCancelled = EditorUtility.DisplayCancelableProgressBar("Progress...", "View: " + viewIndexForProgress + " (" + ((int)(progress * 100f)) + "%)", progress);

                if (IsCancelled)
                    throw new Exception("Cancelled");

                studio.view.checkedSubViews[viewIndex].func(model);
                viewName = studio.view.checkedSubViews[viewIndex].name;

                Vector3 screenPivotPos = Camera.main.WorldToScreenPoint(model.GetPivotPosition());
                currFramePivot = new PixelVector(screenPivotPos);

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
                if (!EditorApplication.isPlaying)
                {
                    double deltaTime = EditorApplication.timeSinceStartup - prevTime;
                    if (deltaTime < studio.filming.delay)
                        return;
                    prevTime = EditorApplication.timeSinceStartup;
                }

                Texture2D mainTex = CapturingHelper.CapturePixelsManagingShadow(model, studio);

                Texture2D normalTex = null;
                if (studio.output.toMakeNormalMap)
                {
                    float rotX = studio.view.slopeAngle;
                    float rotY = studio.view.rotationType == RotationType.Camera ? studio.view.checkedSubViews[viewIndex].angle : 0;
                    normalTex = CapturingHelper.CapturePixelsForNormal(model, rotX, rotY, studio.shadow.obj);
                }

                PixelBound bound = new PixelBound();
                if (!TextureHelper.GetPixelBound(mainTex, currFramePivot, bound))
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
                            normalTex = TextureHelper.TrimTexture(normalTex, bound, studio.triming.margin, EngineConstants.NORMALMAP_COLOR32, true);
                    }
                }

                string viewName = studio.view.checkedSubViews[viewIndex].name;
                if (studio.packing.on || studio.triming.IsOnUniformSize())
                {
                    bakingDataList[viewIndex] = new BakingData(viewName, pivot, mainTex, normalTex);
                }
                else // !studio.packing.on && !studio.trim.IsOnUniformSize()
                {
                    string mtrlFullName = BakeIndividuallyReal(ref mainTex, pivot, viewName, "");
                    if (studio.output.toMakeNormalMap)
                        BakeIndividuallyReal(ref normalTex, pivot, viewName, "", true);
                    CreateMaterial(mtrlFullName, mainTex, normalAtlasTexture);
                }

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

                if (studio.triming.IsOnUniformSize())
                    TrimToUniformSizeAll();

                if (!studio.packing.on && studio.triming.IsOnUniformSize())
                {
                    foreach (BakingData data in bakingDataList)
                    {
                        string mtrlFullName = BakeIndividuallyReal(ref data.mainTex, data.pivot, data.name, "");
                        if (studio.output.toMakeNormalMap)
                            BakeIndividuallyReal(ref data.normalTex, data.pivot, data.name, "", true);
                        CreateMaterial(mtrlFullName, data.mainTex, data.normalTex);
                    }
                }
                else if (studio.packing.on)
                {
                    PixelVector[] pivots = new PixelVector[bakingDataList.Length];
                    string[] spriteNames = new string[bakingDataList.Length];
                    Texture2D[] mainTextures = new Texture2D[bakingDataList.Length];

                    Texture2D[] normalTextures = null;
                    if (studio.output.toMakeNormalMap)
                        normalTextures = new Texture2D[bakingDataList.Length];

                    for (int i = 0; i < bakingDataList.Length; ++i)
                    {
                        BakingData bakingData = bakingDataList[i];
                        pivots[i] = bakingData.pivot;
                        spriteNames[i] = bakingData.name;
                        mainTextures[i] = bakingData.mainTex;
                        if (studio.output.toMakeNormalMap)
                            normalTextures[i] = bakingData.normalTex;
                    }

                    BakeWithPacking(pivots, "", spriteNames, mainTextures, normalTextures);
                }

                if (IsToMakePrefab())
                {
                    GameObject obj = PrefabUtility.InstantiatePrefab(model.spritePrefab) as GameObject;
                    if (obj != null)
                    {
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

        private void TrimToUniformSizeAll()
        {
            try
            {
                for (int i = 0; i < bakingDataList.Length; ++i)
                {
                    BakingData bakingData = bakingDataList[i];

                    bakingData.pivot.SubtractWithMargin(uniformBound.min, studio.triming.margin);

                    bakingData.mainTex = TextureHelper.TrimTexture(bakingData.mainTex,
                        uniformBound, studio.triming.margin, EngineConstants.CLEAR_COLOR32);
                    if (studio.output.toMakeNormalMap)
                    {
                        bakingData.normalTex = TextureHelper.TrimTexture(bakingData.normalTex,
                            uniformBound, studio.triming.margin, EngineConstants.NORMALMAP_COLOR32, true);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
