using DefaultNamespace;
using ECSTest.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ECSTest.Systems
{
    [BurstCompile]
    public partial class TextVisualizationSystem : SystemBase
    {
        private const uint UMinus = 45;
        private const uint UPlus = 43;
        private const uint UDollar = 36;
        private Material fontMaterial;
        private NativeHashMap<uint, GlyphData> glyphHashMap;

        private EntityQuery textQuery;
        private BatchRendererGroup mBRG;
        private TextBatchData textBatchData;
        private TextAnimationDataHolder textAnimationData;
        private float referenceGlyphWidth;
        private int fontPointSize;
        private int2 textureSize;

        private const int kSizeOfPackedMatrix = sizeof(float) * 4 * 3;
        private const int kBytesPerInstance = (kSizeOfPackedMatrix * 2) + (kSizeOfFloat4 * 2);
        private const int kSizeOfFloat4 = sizeof(float) * 4;

        private uint cullingMask;

        protected override void OnCreate()
        {
            base.OnCreate();

            textQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AnimatedTextComponent>()
                .Build(this);
        }

        protected override void OnDestroy()
        {
            Clear();
            base.OnDestroy();
        }

        public void Init()
        {
            var renderDataHolder = GameServices.Instance.RenderDataHolder;
            fontMaterial = renderDataHolder.FontMaterial;
            textAnimationData = renderDataHolder.TextAnimationData;
            glyphHashMap = renderDataHolder.GetGlyphData();
            referenceGlyphWidth = renderDataHolder.ReferenceGlyphWidth;
            fontPointSize = renderDataHolder.FontPointSize;

            textureSize = new int2(fontMaterial.mainTexture.width, fontMaterial.mainTexture.height);

            mBRG = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

            textBatchData = new TextBatchData(fontMaterial, renderDataHolder.Quad, mBRG, 5000);
        }

        protected override void OnUpdate()
        {
            if (textQuery.IsEmpty) return;
            var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();

            new AnimateTextJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                TextAnimationData = textAnimationData.GetTextAnimationData,
                ECB = singleton.CreateCommandBuffer(World.DefaultGameObjectInjectionWorld.Unmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

        private unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            if (cullingContext.viewType != BatchCullingViewType.Camera || textQuery.IsEmpty) return new JobHandle();

            NativeArray<AnimatedTextComponent> textComponents = textQuery.ToComponentDataArray<AnimatedTextComponent>(Allocator.TempJob);

            if (textComponents.Length == 0) return new JobHandle();
            textComponents.Sort();
            NativeArray<int> numberOfLettersList = new(textComponents.Length, Allocator.TempJob);
            int totalLettersCount = 0;

            for (int i = 0; i < textComponents.Length; i++)
            {
                if (textComponents[i].NonCashValue > 0)
                {
                    totalLettersCount += (int)math.floor(math.log10(textComponents[i].NonCashValue) + 1);
                }
                else if (textComponents[i].CashValue != 0)
                {
                    totalLettersCount += (int)math.floor(math.log10(math.abs(textComponents[i].CashValue)) + 1 + 2); // +2 for $ and +- symbols
                }
                numberOfLettersList[i] = totalLettersCount;
            }

            int drawCommandsCount = textBatchData.GetCommandsCount(totalLettersCount);
            int visibleOffset = 0;
            int drawCommandsIndex = 0;

            BatchCullingOutputDrawCommands* drawCommands = BRGSystem.AllocateMemory(cullingOutput, totalLettersCount, drawCommandsCount);

            drawCommands->drawRanges[0].drawCommandsBegin = 0;
            drawCommands->drawRanges[0].drawCommandsCount = (uint)drawCommandsCount;

            drawCommands->drawRanges[0].filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff, layer = 5 };

            NativeArray<float> bufferArray = textBatchData.LockGPUArray(totalLettersCount);

            TextRenderJob textRenderJob = new()
            {
                TextComponents = textComponents,
                Indexes = numberOfLettersList,
                DataBuffer = bufferArray,
                GlyphHashMap = glyphHashMap,
                FontPointSize = fontPointSize,
                ReferenceGlyphWidth = referenceGlyphWidth,
                IndexAddressObjectToWorld = textBatchData.ByteAddressObjectToWorld / sizeof(float),
                IndexAddressWorldToObject = textBatchData.ByteAddressWorldToObject / sizeof(float),
                IndexAddressColor = textBatchData.ByteAddressColor / sizeof(float),
                IndexAddressUV = textBatchData.ByteAddressUV / sizeof(float),
                TextureSize = textureSize
            };

            JobHandle jobHandle = textRenderJob.Schedule(textComponents.Length, 64);
            jobHandle.Complete();

            textBatchData.CreateDrawCommands(drawCommands, ref drawCommandsIndex, totalLettersCount, ref visibleOffset);
            textBatchData.UnlockGPUArray();
            return new JobHandle();
        }

        [BurstCompile]
        private struct TextRenderJob : IJobParallelFor
        {
            [ReadOnly] public NativeHashMap<uint, GlyphData> GlyphHashMap;
            [ReadOnly, DeallocateOnJobCompletion] public NativeArray<int> Indexes;
            [DeallocateOnJobCompletion] public NativeArray<AnimatedTextComponent> TextComponents;
            public float ReferenceGlyphWidth;
            public int FontPointSize;

            [ReadOnly] public int IndexAddressObjectToWorld;
            [ReadOnly] public int IndexAddressWorldToObject;
            [ReadOnly] public int IndexAddressColor;
            [ReadOnly] public int IndexAddressUV;
            [ReadOnly] public int2 TextureSize;

            [WriteOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<float> DataBuffer;

            public void Execute(int i)
            {
                int processedCharactersCount = i > 0 ? Indexes[i - 1] : 0;
                int textLength = i > 0 ? Indexes[i] - Indexes[i - 1] : Indexes[i];

                //bool isShowDamage = TextComponents[i].TextType != AllEnums.TextType.Cash;
                bool isShowDamage = TextComponents[i].NonCashValue != 0;//change to isShowCashIcon
                int numberToShow = math.abs(isShowDamage ? TextComponents[i].NonCashValue : TextComponents[i].CashValue);

                NativeArray<uint> charCodes = new(textLength, Allocator.Temp);

                float offsetX = TextComponents[i].Position.x;

                float totalWidth = 0;
                for (int j = 0; j < charCodes.Length; j++)
                {
                    if (!GlyphHashMap.ContainsKey(charCodes[j]))
                        continue;

                    GlyphData glyphData = GlyphHashMap[charCodes[j]];
                    totalWidth += (glyphData.HorizontalAdvance / FontPointSize) * TextComponents[i].Scale;
                }
                offsetX -= totalWidth / 2;

                for (int j = 0; j < textLength; j++)
                {
                    if (!isShowDamage && j == 0)
                    {
                        charCodes[j] = TextComponents[i].CashValue > 0 ? UPlus : UMinus;
                        continue;
                    }
                    else if (!isShowDamage && j == textLength - 1)
                    {
                        charCodes[j] = UDollar;
                        continue;
                    }

                    charCodes[textLength - j - 1] = (uint)(numberToShow % 10 + 48);
                    numberToShow /= 10;
                }

                for (int j = 0; j < charCodes.Length; j++)
                {
                    GlyphData glyphData = GlyphHashMap[charCodes[j]];

                    float offsetY = TextComponents[i].Position.y - (glyphData.Height / 2 - glyphData.BearingY) / FontPointSize;

                    float4x4 matr = float4x4.TRS(new float3(offsetX, offsetY, 0),
                        quaternion.identity,
                        new float3(glyphData.Width * TextComponents[i].Scale / FontPointSize, glyphData.Height * TextComponents[i].Scale / FontPointSize, 1));

                    BRGSystem.FillDataBuffer(ref DataBuffer, IndexAddressObjectToWorld, processedCharactersCount + j, matr);

                    float4x4 inverse = math.inverse(matr);
                    BRGSystem.FillDataBuffer(ref DataBuffer, IndexAddressWorldToObject, processedCharactersCount + j, inverse);

                    BRGSystem.FillDataBuffer(ref DataBuffer, IndexAddressColor, processedCharactersCount + j, TextComponents[i].Color);

                    float texScaleX = glyphData.RectWidth / TextureSize.x;
                    float texScaleY = glyphData.RectHeight / TextureSize.y;
                    float texOffsetX = glyphData.X / TextureSize.x;
                    float texOffsetY = glyphData.Y / TextureSize.y;
                    BRGSystem.FillDataBuffer
                    (
                        ref DataBuffer,
                        IndexAddressUV,
                        processedCharactersCount + j,
                        new float4(texScaleX, texScaleY, texOffsetX, texOffsetY)
                    );

                    float nextCharOffset = 0;
                    if (j + 1 < charCodes.Length)
                    {
                        GlyphData nextCharData = GlyphHashMap[charCodes[j + 1]];

                        nextCharOffset = nextCharData.HorizontalAdvance / FontPointSize / 2 * TextComponents[i].Scale;
                    }

                    offsetX += glyphData.HorizontalAdvance / FontPointSize / 2 * TextComponents[i].Scale + nextCharOffset * TextComponents[i].Scale;
                }
            }
        }

        [BurstCompile]
        private partial struct AnimateTextJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public TextAnimationData TextAnimationData;

            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, ref AnimatedTextComponent animatedText)
            {
                animatedText.Timer += DeltaTime;
                int sortKey = chunkIndex;

                switch (animatedText.TextType)
                {
                    case AllEnums.TextType.Damage:
                        AnimateDamageText(ref animatedText, entity, sortKey);
                        break;
                    case AllEnums.TextType.Cash:
                        AnimateCashText(ref animatedText, entity, sortKey);
                        break;
                    case AllEnums.TextType.DropZone:
                        AnimateDropZoneText(ref animatedText, entity, sortKey);
                        break;
                    case AllEnums.TextType.SpawnZone:
                        break;
                }
            }

            private void AnimateDamageText(ref AnimatedTextComponent animatedText, Entity entity, int sortKey)
            {
                if (animatedText.Timer >= TextAnimationData.TotalDamageAnimationTime)
                {
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                }

                if (animatedText.Timer < TextAnimationData.DamagePopUpTime)
                {
                    animatedText.Scale += TextAnimationData.DamageScaleSpeed * DeltaTime;
                }
                else
                {
                    float fadingTime = animatedText.Timer - (TextAnimationData.TotalDamageAnimationTime - TextAnimationData.DamageFadingTime);
                    if (fadingTime > 0)
                    {
                        animatedText.Color.w = math.clamp((TextAnimationData.DamageFadingTime - fadingTime) / (TextAnimationData.DamageFadingTime), 0, 1.0f);
                    }
                }

                animatedText.Position += new float2(0, TextAnimationData.DamagePopUpSpeed * DeltaTime);

            }

            private void AnimateCashText(ref AnimatedTextComponent animatedText, Entity entity, int sortKey)
            {
                if (animatedText.Timer >= TextAnimationData.TotalCashAnimationTime)
                {
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                }

                if (animatedText.Timer < TextAnimationData.CashPopUpTime)
                {
                    animatedText.Scale += TextAnimationData.CashScaleSpeed * DeltaTime;
                    animatedText.Position += new float2(0, TextAnimationData.CashPopUpSpeed * DeltaTime);
                }
                else
                {
                    float fadingTime = animatedText.Timer - (TextAnimationData.TotalCashAnimationTime - TextAnimationData.CashFadingTime);
                    if (fadingTime > 0)
                    {
                        animatedText.Color.w = math.clamp((TextAnimationData.CashFadingTime - fadingTime) / (TextAnimationData.CashFadingTime), 0, 1.0f);
                    }
                }

            }

            private void AnimateDropZoneText(ref AnimatedTextComponent animatedText, Entity entity, int sortKey)
            {
                if (animatedText.Timer >= 1)
                {
                    if (animatedText.NonCashValue > 0)
                    {
                        ECB.AddComponent(sortKey, entity,
                        new AnimatedTextComponent()
                        {
                            CashValue = 0,
                            NonCashValue = animatedText.NonCashValue - 1,
                            Position = (animatedText.NonCashValue >= 10 && (animatedText.NonCashValue - 1) < 10) ? new float2(animatedText.Position.x - TextAnimationData.DropZoneXStartOffset, animatedText.Position.y) : animatedText.Position,
                            Timer = 0,
                            Color = TextAnimationData.DropZoneTextColor,
                            TextType = AllEnums.TextType.DropZone,
                            Scale = TextAnimationData.DropZoneScale
                        });
                    }
                    else
                        ECB.DestroyEntity(sortKey, entity);
                }

                float fadingTime = animatedText.Timer - TextAnimationData.DropZoneTextIdleTime;
                if (fadingTime > 0)//check here - we should use only fade time , but clamp totalTime
                {
                    animatedText.Color.w = math.clamp((TextAnimationData.DropZoneTextFadingTime - fadingTime) / TextAnimationData.DropZoneTextFadingTime, 0, 1.0f);
                }
            }
        }

        public void Clear()
        {
            mBRG?.Dispose();
            try
            {
                if (glyphHashMap.IsCreated)
                    glyphHashMap.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"glyphHashMap {e}");
            }
            textBatchData?.Dispose();
        }
    }
}