using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ABS
{
    public abstract class Baker
    {
        protected enum BakingState
        {
            Initialize,
            BeginAnimation,
            BeginView,
            BeginFrame,
            CaptureFrame,
            EndView,
            EndFrame,
            EndAnimation,
            Finalize
        }
        protected StateMachine<BakingState> stateMachine = null;

        protected readonly Model model;
        protected readonly Studio studio;

        protected readonly string sIndex = "";
        protected readonly bool isMultiModel = false;

        protected string parentFolderPath = "";

        public readonly string modelName;

        protected string fileBaseName = "";
        protected string folderPath = "";

        protected int viewIndex = -1;
        protected string viewName;

        protected Frame[] framesForView = null;
        protected int frameIndex = -1;

        protected PixelVector currFramePivot;
        protected PixelBound uniformBound;

        protected PixelVector[] framePivots = null;
        protected Texture2D[] frameMainTextures = null;

        protected List<AnimatorState> animatorStates = null;

        protected Sprite firstSprite;
        protected Material firstMaterial;

        private Texture2D mainAtlasTexture;
        protected Texture2D normalAtlasTexture;
        private Material atlasMaterial;

        protected float frameInterval;
        protected double prevTime = 0.0;

        public bool IsCancelled { get; set; }

        public Baker(Model model, Studio studio, string sIndex, string parentFolderPath)
        {
            this.model = model;
            this.studio = studio;
            this.sIndex = sIndex;
            isMultiModel = sIndex.Length > 0;
            this.parentFolderPath = parentFolderPath;

            modelName = model.nameSuffix.Length > 0 ? model.name + model.nameSuffix : model.name;

            Camera mainCam = Camera.main;
            if (model.toUseMyCamSize)
            {
                if (mainCam.orthographic)
                    mainCam.orthographicSize = model.cameraOrthoSize;
                else
                    mainCam.fieldOfView = model.cameraFOV;
            }
            else
            {
                if (mainCam.orthographic)
                    mainCam.orthographicSize = studio.cam.orthographicSize;
                else
                    mainCam.fieldOfView = studio.cam.fieldOfView;
            }
        }

        public bool IsInProgress()
        {
            return stateMachine != null;
        }

        public void UpdateState()
        {
            stateMachine.Update();
        }

        public virtual void Finish()
        {
            stateMachine = null;
        }

        protected void BuildFolderPathAndCreate(string fileName)
        {
            string folderName = "";

            if (parentFolderPath.Length > 0)
            {
                if (sIndex.Length > 0)
                    folderName += sIndex + "_";
                folderName += fileName;
            }
            else
            {
                int assetRootIndex = studio.dir.exportPath.IndexOf("Assets");
                parentFolderPath = studio.dir.exportPath.Substring(assetRootIndex);

                folderName += fileName + "_" + PathHelper.MakeDateTimeString();
            }

            folderPath = Path.Combine(parentFolderPath, folderName);
            Directory.CreateDirectory(folderPath);
        }

        protected string BuildFileBaseName()
        {
            string fileName = "";
            if (studio.naming.fileNamePrefix.Length > 0)
                fileName += studio.naming.fileNamePrefix;
            fileName += modelName;
            return fileName;
        }

        protected void TrimToUniformSize(PixelVector[] pivots, Texture2D[] mainTextures, Texture2D[] normalTextures = null)
        {
            try
            {
                Debug.Assert(studio.triming.isUniformSize);

                for (int i = 0; i < mainTextures.Length; ++i)
                {
                    pivots[i].SubtractWithMargin(uniformBound.min, studio.triming.margin);

                    mainTextures[i] = TextureHelper.TrimTexture(mainTextures[i], uniformBound, studio.triming.margin, EngineConstants.CLEAR_COLOR32);
                    if (studio.output.toMakeNormalMap && normalTextures != null)
                    {
                        normalTextures[i] = TextureHelper.TrimTexture(normalTextures[i], uniformBound, studio.triming.margin,
                            EngineConstants.NORMALMAP_COLOR32, true);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected void BakeIndividually_Group(PixelVector[] pivots, string subName, Texture2D[] mainTextures, Texture2D[] normalTextures = null)
        {
            try
            {
                for (int i = 0; i < mainTextures.Length; i++)
                {
                    string mtrlFullName = BakeIndividually(ref mainTextures[i], pivots[i], subName, i);

                    Texture2D normalTex = null;
                    if (studio.output.toMakeNormalMap && normalTextures != null)
                    {
                        BakeIndividually(ref normalTextures[i], pivots[i], subName, i, true);
                        normalTex = normalTextures[i];
                    }
                    
                    CreateMaterial(mtrlFullName, mainTextures[i], normalTex);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected string BakeIndividually(ref Texture2D tex, PixelVector pivot, string subName, int frame, bool isNormalMap = false)
        {
            try
            {
                string detailName = frame.ToString().PadLeft((framesForView.Length - 1).ToString().Length, '0');
                return BakeIndividuallyReal(ref tex, pivot, subName, detailName, isNormalMap);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected string BakeIndividuallyReal(ref Texture2D tex, PixelVector pivot, string subName, string detailName, bool isNormalMap = false)
        {
            try
            {
                string fileFullName = fileBaseName;
                if (subName.Length > 0)
                    fileFullName += "_" + subName;
                if (detailName.Length > 0)
                    fileFullName += "_" + detailName;
                if (isNormalMap)
                    fileFullName += "_normal";

                string filePath = TextureHelper.SaveTexture(folderPath, fileFullName, tex);

                AssetDatabase.ImportAsset(filePath);

                TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
                if (texImporter != null)
                {
                    texImporter.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Sprite;
                    texImporter.spriteImportMode = SpriteImportMode.Multiple;

                    SpriteMetaData[] metaData = new SpriteMetaData[1];

                    string sprtName = "";
                    if (studio.naming.isModelPrefixSprite)
                        sprtName += modelName + "_";
                    sprtName += "0";
                    metaData[0].name = sprtName;

                    metaData[0].rect = new Rect(0.0f, 0.0f, (float)tex.width, (float)tex.height);
                    metaData[0].alignment = (int)SpriteAlignment.Custom;
                    metaData[0].pivot = new Vector2((float)pivot.x / (float)tex.width,
                                                    (float)pivot.y / (float)tex.height);

                    texImporter.spritesheet = metaData;

                    AssetDatabase.ImportAsset(filePath);

                    if (studio.output.toMakeMaterial)
                        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
                }

                if (firstSprite == null)
                    firstSprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);

                return fileFullName;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected Sprite[] BakeWithPacking(PixelVector[] pivots, string subName, Texture2D[] mainTextures, Texture2D[] normalTextures = null)
        {
            try
            {
                string[] spriteNames = new string[mainTextures.Length];
                for (int i = 0; i < mainTextures.Length; ++i)
                    spriteNames[i] = i.ToString();
                return BakeWithPacking(pivots, subName, spriteNames, mainTextures, normalTextures);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected Sprite[] BakeWithPacking(PixelVector[] pivots, string subName, string[] spriteNames, Texture2D[] mainTextures,
            Texture2D[] normalTextures = null)
        {
            Debug.Assert(mainTextures.Length == pivots.Length);
            Debug.Assert(mainTextures.Length == spriteNames.Length);

            try
            {
                int atlasLength = 64;
                if (studio.packing.method == PackingMethod.Optimized)
                {
                    if (!int.TryParse(studio.atlasSizes[studio.packing.maxAtlasSizeIndex], out atlasLength))
                        atlasLength = 4096;
                }
                else if (studio.packing.method == PackingMethod.InOrder)
                {
                    if (!int.TryParse(studio.atlasSizes[studio.packing.minAtlasSizeIndex], out atlasLength))
                        atlasLength = 256;
                }

                Rect[] atlasRects = null;

                if (studio.packing.method == PackingMethod.Optimized)
                {
                    mainAtlasTexture = new Texture2D(atlasLength, atlasLength, TextureFormat.ARGB32, false);
                    atlasRects = mainAtlasTexture.PackTextures(mainTextures, studio.packing.padding, atlasLength);
                    for (int i = 0; i < atlasRects.Length; i++)
                    {
                        Texture2D tex = mainTextures[i];
                        float newX = atlasRects[i].x * mainAtlasTexture.width;
                        float newY = atlasRects[i].y * mainAtlasTexture.height;
                        atlasRects[i] = new Rect(newX, newY, (float)tex.width, (float)tex.height);
                    }

                    if (studio.output.toMakeNormalMap && normalTextures != null)
                    {
                        normalAtlasTexture = new Texture2D(atlasLength, atlasLength, TextureFormat.ARGB32, false);
                        normalAtlasTexture.PackTextures(normalTextures, studio.packing.padding, atlasLength);

                        Color32[] normapMapPixels = normalAtlasTexture.GetPixels32();
                        Color32[] basePixels = Enumerable.Repeat(EngineConstants.NORMALMAP_COLOR32, normapMapPixels.Length).ToArray();
                        for (int i = 0; i < basePixels.Length; i++)
                        {
                            Color32 pixel = normapMapPixels[i];
                            if (pixel.a > 0)
                                basePixels[i] = pixel;
                        }

                        normalAtlasTexture = new Texture2D(normalAtlasTexture.width, normalAtlasTexture.height, TextureFormat.ARGB32, false);
                        normalAtlasTexture.SetPixels32(basePixels);
                        normalAtlasTexture.Apply();
                    }
                }
                else if (studio.packing.method == PackingMethod.InOrder)
                {
                    MakeAtlasInOrder(mainTextures, ref mainAtlasTexture, ref atlasLength, ref atlasRects, EngineConstants.CLEAR_COLOR32);
                    if (studio.output.toMakeNormalMap && normalTextures != null)
                        MakeAtlasInOrder(normalTextures, ref normalAtlasTexture, ref atlasLength, ref atlasRects, EngineConstants.NORMALMAP_COLOR32);
                }

                string fileFullName = fileBaseName;
                if (subName.Length > 0)
                    fileFullName += "_" + subName;

                string modelAtlasFilePath = SaveAtlasAndSetMetaData(fileFullName, ref mainAtlasTexture, atlasLength, atlasRects,
                    mainTextures, pivots, spriteNames);

                if (studio.output.toMakeNormalMap && normalAtlasTexture != null)
                {
                    SaveAtlasAndSetMetaData(fileFullName, ref normalAtlasTexture, atlasLength, atlasRects, normalTextures, pivots,
                        spriteNames, true);
                }

                Sprite[] modelSprites = AssetDatabase.LoadAllAssetsAtPath(modelAtlasFilePath).OfType<Sprite>().ToArray();
                if (firstSprite == null)
                    firstSprite = modelSprites[0];

                atlasMaterial = CreateMaterial(fileFullName, mainAtlasTexture, normalAtlasTexture);

                return modelSprites;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected Material CreateMaterial(string fileName, Texture2D mainTex, Texture2D normalTex)
        {
            Material material = null;

            if (studio.output.toMakeMaterial)
            {
                if (studio.output.spriteMaterial != null)
                {
                    material = new Material(studio.output.spriteMaterial);

                    if (studio.output.materialBuilder != null)
                        studio.output.materialBuilder.BindTextures(material, mainTex, normalTex);
                }
                else
                {
                    Shader shader;
                    if (normalTex != null)
                        shader = Shader.Find("Legacy Shaders/Transparent/Bumped Diffuse");
                    else
                        shader = Shader.Find("Standard");

                    if (shader != null)
                    {
                        material = new Material(shader);
                        material.SetTexture("_MainTex", mainTex);
                        if (normalTex != null)
                            material.SetTexture("_BumpMap", normalTex);
                    }
                }

                if (material != null)
                {
                    AssetDatabase.CreateAsset(material, folderPath + "/" + fileName + ".mat");

                    if (firstMaterial == null)
                        firstMaterial = material;
                }
            }

            return material;
        }

        private void MakeAtlasInOrder(Texture2D[] textures, ref Texture2D atlasTexture, ref int atlasLength, ref Rect[] atlasRects, Color32 defaultColor)
        {
            int maxSpriteWidth = int.MinValue;
            int maxSpriteHeight = int.MinValue;
            foreach (Texture2D tex in textures)
            {
                maxSpriteWidth = Mathf.Max(tex.width, maxSpriteWidth);
                maxSpriteHeight = Mathf.Max(tex.height, maxSpriteHeight);
            }

            while (atlasLength < maxSpriteWidth || atlasLength < maxSpriteHeight)
                atlasLength *= 2;

            int atlasWidth = atlasLength;
            int atlasHeight = atlasLength;

            while (true)
            {
                atlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.ARGB32, false);
                Color32[] atlasPixels = Enumerable.Repeat(defaultColor, atlasWidth * atlasHeight).ToArray();

                atlasRects = new Rect[textures.Length];
                int originY = atlasHeight - maxSpriteHeight;

                bool toMultiply = false;

                int atlasRectIndex = 0;
                int currX = 0, currY = originY;
                foreach (Texture2D tex in textures)
                {
                    if (currX + tex.width > atlasWidth)
                    {
                        if (currY - maxSpriteHeight < 0)
                        {
                            toMultiply = true;
                            break;
                        }
                        currX = 0;
                        currY -= (maxSpriteHeight + studio.packing.padding);
                    }
                    WriteSpriteToAtlas(tex, atlasPixels, currX, currY, atlasTexture.width);
                    atlasRects[atlasRectIndex++] = new Rect(currX, currY, tex.width, tex.height);
                    currX += tex.width + studio.packing.padding;
                }

                if (toMultiply)
                {
                    if (atlasWidth == atlasHeight)
                        atlasWidth *= 2;
                    else // atlasWidth > atlasHeight
                        atlasHeight *= 2;

                    if (atlasWidth > 8192)
                    {
                        Debug.Log("Output sprite sheet size is bigger than 8192 X 8192");
                        return;
                    }
                }
                else
                {
                    atlasLength = atlasWidth;
                    atlasTexture.SetPixels32(atlasPixels);
                    atlasTexture.Apply();
                    break;
                }
            }
        }

        private void WriteSpriteToAtlas(Texture2D spriteTex, Color32[] atlasPixels, int atlasStartX, int atlasStartY, int atlasWidth)
        {
            Color32[] spritePixels = spriteTex.GetPixels32();

            for (int i = 0; i < spriteTex.width * spriteTex.height; ++i)
            {
                int x = i % spriteTex.width;
                int y = i / spriteTex.width;
                int atlasIndex = (atlasStartY + y) * atlasWidth + (atlasStartX + x);
                if (atlasIndex < atlasPixels.Length)
                    atlasPixels[atlasIndex] = spritePixels[i];
            }
        }

        private string SaveAtlasAndSetMetaData(string fileName, ref Texture2D atlasTexture, int atlasLength, Rect[] atlasRects,
            Texture2D[] textures, PixelVector[] pivots, string[] spriteNames, bool isNormalMap = false)
        {
            if (isNormalMap)
                fileName += "_normal";

            string filePath = TextureHelper.SaveTexture(folderPath, fileName, atlasTexture);
            AssetDatabase.ImportAsset(filePath);

            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
            if (textureImporter != null)
            {
                textureImporter.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Multiple;
                textureImporter.maxTextureSize = atlasLength;

                SpriteMetaData[] metaData = new SpriteMetaData[textures.Length];
                for (int i = 0; i < textures.Length; i++)
                {
                    string sprtName = "";
                    if (studio.naming.isModelPrefixSprite)
                        sprtName += modelName + "_";
                    string numberStr = spriteNames[i].PadLeft((spriteNames.Length - 1).ToString().Length, '0');
                    sprtName += numberStr;
                    metaData[i].name = sprtName;

                    metaData[i].rect = atlasRects[i];
                    metaData[i].alignment = (int)SpriteAlignment.Custom;
                    metaData[i].pivot = new Vector2((float)pivots[i].x / (float)textures[i].width,
                                                    (float)pivots[i].y / (float)textures[i].height);
                }
                textureImporter.spritesheet = metaData;

                AssetDatabase.ImportAsset(filePath);

                atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            }

            return filePath;
        }

        protected const string ANGLE_PARAM_NAME = "angle";

        protected void AddParameterIfNotExist(AnimatorController controller, string paramName, AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Trigger)
        {
            if (!HasParameter(controller, paramName, paramType))
                controller.AddParameter(paramName, paramType);
        }

        protected bool HasParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Trigger)
        {
            foreach (AnimatorControllerParameter param in controller.parameters)
            {
                if (param.name == paramName && param.type == paramType)
                    return true;
            }
            return false;
        }

        protected AnimatorState GetOrCreateState(AnimatorStateMachine stateMachine, string stateName)
        {
            AnimatorState state = FindState(stateMachine, stateName);
            if (state == null)
                state = stateMachine.AddState(stateName);
            return state;
        }

        protected AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (childState.state.name == stateName)
                    return childState.state;
            }
            return null;
        }

        protected void AddDirectionTransitionA2BIfNotExist(AnimatorState stateA, AnimatorState stateB, int angle)
        {
            AnimatorStateTransition transition = FindTransitionA2B(stateA, stateB);
            if (transition == null)
                transition = stateA.AddTransition(stateB);
            RemoveAllAndAddCondition(transition, ANGLE_PARAM_NAME, AnimatorConditionMode.Equals, angle);
        }

        protected AnimatorStateTransition FindTransitionA2B(AnimatorState stateA, AnimatorState stateB)
        {
            foreach (AnimatorStateTransition transition in stateA.transitions)
            {
                if (transition.destinationState == stateB)
                    return transition;
            }
            return null;
        }

        protected void RemoveAllAndAddCondition(AnimatorTransitionBase transition, string paramName, AnimatorConditionMode mode, float threshold)
        {
            foreach (AnimatorCondition condition in transition.conditions)
            {
                if (condition.parameter == paramName)
                    transition.RemoveCondition(condition);
            }
            transition.AddCondition(mode, threshold, paramName);
        }

        protected AnimationClip MakeAnimationClipsForView(bool isLooping, Sprite[] sprites, string viewName)
        {
            AnimationClip animClip = new AnimationClip
            {
                frameRate = studio.output.frameRate
            };

            if (isLooping)
            {
                AnimationClipSettings animClipSettings = AnimationUtility.GetAnimationClipSettings(animClip);
                animClipSettings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(animClip, animClipSettings);
            }

            //----- sprite curve binding -----
            EditorCurveBinding spriteCurveBinding = PrefabBuilder.GetSpriteCurveBinding();

            ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                spriteKeyFrames[i] = new ObjectReferenceKeyframe();
                float unitTime = 1f / animClip.frameRate;
                spriteKeyFrames[i].time = studio.output.frameInterval * i * unitTime;
                spriteKeyFrames[i].value = sprites[i];
            }

            AnimationUtility.SetObjectReferenceCurve(animClip, spriteCurveBinding, spriteKeyFrames);
            //-----------------------------------

            //----- material curve binding ------
            if (atlasMaterial != null)
            {
                EditorCurveBinding mtrlCurveBinding = PrefabBuilder.GetMaterialCurveBinding();

                ObjectReferenceKeyframe[] mtrlKeyFrames = new ObjectReferenceKeyframe[1];
                mtrlKeyFrames[0] = new ObjectReferenceKeyframe
                {
                    time = 0,
                    value = atlasMaterial
                };

                AnimationUtility.SetObjectReferenceCurve(animClip, mtrlCurveBinding, mtrlKeyFrames);
            }

            string clipFilePath = Path.Combine(folderPath, fileBaseName);
            if (viewName.Length > 0)
                clipFilePath += "_" + viewName;
            clipFilePath += ".anim";

            AssetDatabase.CreateAsset(animClip, clipFilePath);

            if (animatorStates != null && studio.view.checkedSubViews.Count == animatorStates.Count)
            {
                for (int i = 0; i < studio.view.checkedSubViews.Count; ++i)
                {
                    if (studio.view.checkedSubViews[i].name == viewName)
                    {
                        animatorStates[i].motion = animClip;
                        break;
                    }
                }
            }

            return animClip;
        }

        protected bool IsToMakePrefab()
        {
            return studio.output.toMakePrefab && model.spritePrefab != null && model.prefabBuilder != null;
        }

        protected void SaveAsPrefab(GameObject obj, string prefabName)
        {
            obj.SetActive(true);

            string prefabPath = folderPath + "/" + prefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
