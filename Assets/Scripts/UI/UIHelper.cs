using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;
using static MusicManager;
using Random = UnityEngine.Random;

namespace UI
{
    public class UIHelper : ScriptableObjSingleton<UIHelper>
    {
        public const float WaveAnnouncementOffset = 3;
        [FoldoutGroup("UIAnimations")][SerializeField] private float menuPanelFadeTime;
        [Space]
        [FoldoutGroup("UIAnimations")][SerializeField] private float flashDuration;
        [FoldoutGroup("UIAnimations")][SerializeField] private float flashStartOpacity;
        [FoldoutGroup("UIAnimations")][SerializeField] private float flashEndOpacity;
        [FoldoutGroup("UIAnimations")][SerializeField] private int flashCount;
        [FoldoutGroup("UIAnimations")][SerializeField] private float typewriterEffectSpeed;
        [FoldoutGroup("UIAnimations")][SerializeField] private float typewriterDelayBetweenChars;
        [FoldoutGroup("UIAnimations")][SerializeField] private float typewriterCarriageBlinkInterval;
        [FoldoutGroup("UIAnimations")] public WinWindowAnimationData WinWindowData;
        [FoldoutGroup("UIAnimations")] public NextWaveAnnouncementAnimationData NextWaveAnnouncementData;
        [FoldoutGroup("UIAnimations")] public CashWidgetAnimationData CashWidgetAnimationData;
        [FoldoutGroup("UIAnimations")] public PopUpTextAnimationData PopUpTextAnimationData;
        [FoldoutGroup("UIAnimations")] public RewardWindowAnimationData RewardWindowAnimationData;

        [Space]
        [FoldoutGroup("Atlases")][SerializeField] private SpriteAtlas towerStatIcons;
        [FoldoutGroup("Atlases")][SerializeField] private SpriteAtlas fireModeIcons;
        [FoldoutGroup("Atlases")][SerializeField] private SpriteAtlas energyCellIcons;
        [FoldoutGroup("Atlases")][SerializeField] private SpriteAtlas towerSprites;
        [FoldoutGroup("Atlases")][SerializeField] private SpriteAtlas waveIcons;
        [Space]
        [FoldoutGroup("ShopItems")][SerializeField] private List<GoodsItem> softItems;
        [FoldoutGroup("ShopItems")][SerializeField] private List<GoodsItem> hardItems;
        [FoldoutGroup("ShopItems")][SerializeField] private List<GoodsItem> scrapItems;
        [FoldoutGroup("ShopItems")][SerializeField] private List<GoodsItem> ticketItems;
        [FoldoutGroup("ShopItems")][SerializeField] private List<GoodsItem> offersItems;
        [FoldoutGroup("ShopItems")][SerializeField] private List<GoodsItem> bundlesItems;
        [FoldoutGroup("ShopItems")][SerializeField] private List<GoodsItem> directiveBundles;
        [FoldoutGroup("Sprites")][SerializeField] private List<Sprite> dialogCharacterSprites;

        [FoldoutGroup("Sprites")] public Sprite[] QuestionMarkSprites;
        [FoldoutGroup("Sprites")] public Sprite[] SparksSprites;
        [FoldoutGroup("Sprites")] public Sprite[] TowerUpgrateSprites;

        [FoldoutGroup("Sprites")] public Sprite LockedCommonButtonBackground;
        [FoldoutGroup("Sprites")] public Sprite AvailableCommonButtonBackground;
        [FoldoutGroup("Sprites")] public Sprite SquareButtonYellow;
        [FoldoutGroup("Sprites")] public Sprite AvailableTowerBuildWidget;
        [FoldoutGroup("Sprites")] public Sprite LockedTowerBuildWidget;
        [FoldoutGroup("Sprites")] public Sprite SoftCurrencyReward;
        [FoldoutGroup("Sprites")] public Sprite HardReward;
        [FoldoutGroup("Sprites")] public Sprite ScrapReward;
        [FoldoutGroup("Sprites")] public Sprite TicketReward;
        [FoldoutGroup("Sprites"), SerializeField] private List<AmmoBgData> ammoBg;
        [FoldoutGroup("Sprites")] public Sprite EmptyDirective;
        [FoldoutGroup("Sprites")] public Texture2D RoundButtonRedFrame;
        [FoldoutGroup("Sprites")] public Texture2D RoundButtonRedBackground;
        [FoldoutGroup("Sprites")] public Texture2D RoundButtonBlueFrame;
        [FoldoutGroup("Sprites")] public Texture2D RoundButtonBlueBackground;
        [FoldoutGroup("Sprites")] public Texture2D PowerOffIcon;
        [FoldoutGroup("Sprites")] public Texture2D NoCashIcon;
        [FoldoutGroup("Sprites")] public Texture2D ReloadIcon;
        [FoldoutGroup("Sprites")] public Sprite ActiveMissionLine;
        [FoldoutGroup("Sprites")] public Sprite PassedMissionLine;
        [FoldoutGroup("Sprites")] public Texture2D ActiveMissionPointer;


        [FoldoutGroup("Color")] public Color LockedGrayTint;
        [FoldoutGroup("Color")] public Color Green;
        [FoldoutGroup("Color")] public Color LineStatGreen;
        [FoldoutGroup("Color")] public Color Red;
        [FoldoutGroup("Color/StarsBackground")] public Color StarsBackgroundColor;
        [FoldoutGroup("Color/StarsBackground")] public Color StarsBackgroundColorGray;
        [FoldoutGroup("Color/StarsBackground")] public Color StarsBackgroundColorRed;
        [FoldoutGroup("Color/CostWidget")] public Color CostWidgetGreen;
        [FoldoutGroup("Color/CostWidget")] public Color CostWidgetRed;
        [FoldoutGroup("Color/CostWidget")] public Color CostWidgetBlue;
        [FoldoutGroup("Color/CashWidget")] public Color CashWidgetBlue;
        [FoldoutGroup("Color/CashWidget")] public Color CashWidgetYellow;
        [FoldoutGroup("Color/CashWidget")] public Color CashWidgetRed;

        [FoldoutGroup("Color")] public DirectivesColorData DirectivesColorData;

        [FoldoutGroup("Background")] public List<Sprite> BackgroundSprites;
        [FoldoutGroup("Background")] public List<Sprite> ForegroundStarsLayerOneSprites;
        [FoldoutGroup("Background")] public List<Sprite> ForegroundStarsLayerTwoSprites;

        [FoldoutGroup("Sprites/Ranks")] public Sprite Rank1;
        [FoldoutGroup("Sprites/Ranks")] public Sprite Rank2;
        [FoldoutGroup("Sprites/Ranks")] public Sprite Rank3;
        [Space] public StatsWidgetData StatsWidgetData;
        [SerializeField] private FallbackFontsHolder fallbackFontsHolder;

        public IReadOnlyList<GoodsItem> SoftItems => softItems;
        public IReadOnlyList<GoodsItem> HardItems => hardItems;
        public IReadOnlyList<GoodsItem> ScrapItems => scrapItems;
        public IReadOnlyList<GoodsItem> TicketItems => ticketItems;
        public IReadOnlyList<GoodsItem> OffersItems => offersItems;
        public IReadOnlyList<GoodsItem> BundlesItems => bundlesItems;
        public IReadOnlyList<GoodsItem> DirectiveBundlesItems => directiveBundles;
        [SerializeField] public PanelSettings UIToolkitPanelSettings;

        public Dictionary<AllEnums.TowerId, int> TowerLevelAdjustDict = new()
        {
            { AllEnums.TowerId.Light , 0}, { AllEnums.TowerId.Shotgun, 0}, { AllEnums.TowerId.Heavy , 1}, {AllEnums.TowerId.Cannon,3},
            { AllEnums.TowerId.Plasma , 5}, {AllEnums.TowerId.TwinGun, 6}, { AllEnums.TowerId.Gatling , 7}, { AllEnums.TowerId.Mortar , 8},
            { AllEnums.TowerId.Gauss , 9}, { AllEnums.TowerId.Rocket , 10}
        };

        public Sprite GetCharacterSprite(string characterKey)
        {
            if (dialogCharacterSprites.Exists(x => x.name == characterKey))
            {
                return dialogCharacterSprites.Find(x => x.name == characterKey);
            }
            else
            {
                Debug.LogError($"Character sprite {characterKey} not found");
                return null;
            }
        }
        public Sprite GetTowerStatSprite(string spriteName) => towerStatIcons.GetSprite(spriteName);
        public Sprite GetWaveIcon(string name) => waveIcons.GetSprite(name);
        public Sprite GetFireModeSprite(string spriteName) => fireModeIcons.GetSprite(spriteName);
        public Sprite GetEnergyCellSprite(string spriteName) => energyCellIcons.GetSprite(spriteName);
        public Sprite GetTowerSprite(string spriteName) => towerSprites.GetSprite(spriteName);

        public Sprite GetAmmoBg(WeaponPart ammo)
        {
            foreach (var data in ammoBg)
            {
                if (ammo.TowerId == data.Towers)
                    return data.Bg;
            }
            return null;
        }

        public Sprite GetRankSprite(int level) => level switch
        {
            { } when level >= 14 => Rank3,
            { } when level >= 9 => Rank2,
            { } when level >= 4 => Rank1,
            _ => null
        };

        public Tween GetMenuPanelFadeTween(VisualElement element, bool show, bool transition = false)
        {
            if (show)
            {
                return FadeTween(element, 0, 1, transition ? flashDuration : menuPanelFadeTime).OnStart(() =>
                {
                    element.style.opacity = 0;
                    element.style.display = DisplayStyle.Flex;
                });
            }
            else
            {
                Tween hideTween = FadeTween(element, 1, 0, transition ? flashDuration : menuPanelFadeTime);
                hideTween.OnComplete((() =>
                {
                    element.style.display = DisplayStyle.None;
                    element.style.opacity = 1;
                }));
                return hideTween;
            }
        }

        public Tween FadeTween(VisualElement element, float startValue, float endValue, float time)
        {
            element.style.display = DisplayStyle.Flex;
            element.style.opacity = startValue;
            float panelOpacity = startValue;
            Tween fadeTween = DOTween.To(() => panelOpacity, x => panelOpacity = x, endValue, time)
                .OnUpdate(() => element.style.opacity = panelOpacity);
            return fadeTween;
        }

        public Tween InOutFadeTween(VisualElement element, float duration, Action callbackInTheMiddle = null)
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(FadeTween(element, element.style.opacity.value, 0, duration / 2));
            seq.Append(DOVirtual.DelayedCall(0, () => callbackInTheMiddle?.Invoke()));
            seq.Append(FadeTween(element, 0, 1, duration / 2));
            return seq;
        }

        private Sequence FlashingEffectSeq(VisualElement element, float duration, int flashes)
        {
            float panelOpacity = 1;
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < flashes - 1; i++)
            {
                sequence.Append(
                    DOTween.To(
                            () => panelOpacity,
                            x => panelOpacity = x,
                            Random.Range(flashStartOpacity, flashEndOpacity),
                            duration / flashes)
                        .OnUpdate(() => element.style.opacity = panelOpacity)
                );
            }

            sequence.Append(DOTween.To(() => panelOpacity, x => panelOpacity = x, 1, duration / flashes)
                .OnUpdate(() => element.style.opacity = panelOpacity));

            return sequence;
        }

        public Tween ScaleByYTween(VisualElement element, bool scaleUp, float duration)
        {
            Vector2 endScale = scaleUp ? new Vector2(1, 1) : new Vector2(1, 0);

            return DOTween.To(
                () => (Vector2)element.style.scale.value.value,
                x => { element.style.scale = new StyleScale(x); },
                endScale,
                duration
            );
        }

        public Tween GetShowWindowTween(VisualElement window, VisualElement background)
        {
            window.style.scale = new StyleScale(new Vector2(1f, 0.01f));
            window.style.display = DisplayStyle.Flex;
            Sequence showSeq = DOTween.Sequence();
            showSeq.AppendInterval(0.15f); //buttonTransitionAnimation duration

            showSeq.Append(GetMenuPanelFadeTween(background, true));
            showSeq.Append(ScaleByYTween(window, true, 0.1f)
                .OnComplete(() => window.style.scale = new StyleScale(new Vector2(1, 1))));

            return showSeq;
        }

        public Tween GetHideWindowTween(VisualElement window, VisualElement background)
        {
            Sequence hideSeq = DOTween.Sequence();
            hideSeq.AppendInterval(0.15f); //buttonTransitionAnimation duration

            hideSeq.Append(ScaleByYTween(window, false, 0.1f).OnComplete((() =>
            {
                window.style.display = DisplayStyle.None;
                window.style.scale = new StyleScale(new Vector2(1, 1));
            })));

            hideSeq.Append(GetMenuPanelFadeTween(background, false).OnComplete(() =>
            {
                background.style.display = DisplayStyle.None;
                background.style.opacity = 1;
            }));

            return hideSeq;
        }

        public Tween GetInGameAnnouncementTween(VisualElement parent, VisualElement content, float resolvedWidth, Tween contentChangeTween)
        {
            Sequence result = DOTween.Sequence();

            result.Append(FadeTween(parent, 0, 1, NextWaveAnnouncementData.FadeTime));
            result.Append(ChangeWidth(parent, 0, resolvedWidth, NextWaveAnnouncementData.OpeningTime).OnComplete(() =>
            {
                content.style.visibility = Visibility.Visible;
                content.style.opacity = 0;
            }));
            result.Append(FadeTween(content, 0, 1, NextWaveAnnouncementData.FadeTime));
            result.Append(contentChangeTween);
            result.Append(FadeTween(content, 1, 0, NextWaveAnnouncementData.FadeTime));
            result.Append(ChangeWidth(parent, resolvedWidth, 0, NextWaveAnnouncementData.OpeningTime));
            result.Append(FadeTween(parent, 1, 0, NextWaveAnnouncementData.FadeTime));
            return result;
        }

        #region Move Tweens
        public Tween TranslateXTween(VisualElement element, float start, float end, float duration)
        {
            float positionX = start;
            Tween moveX = DOTween.To(() => positionX, x => positionX = x, end, duration)
                .OnUpdate(() => element.style.translate = new StyleTranslate(new Translate(positionX, element.style.translate.value.y)));
            return moveX;
        }

        public Tween TranslateYTween(VisualElement element, float start, float end, float duration)
        {
            float positionY = start;
            Tween moveY = DOTween.To(() => positionY, y => positionY = y, end, duration)
                .OnUpdate(() => element.style.translate = new StyleTranslate(new Translate(element.style.translate.value.x, positionY)));
            return moveY;
        }

        private Tween TranslateVisualElement(VisualElement element, float2 start, float2 end, float duration)
        {
            Sequence moveSeq = DOTween.Sequence();
            moveSeq.Insert(0, TranslateXTween(element, start.x, end.x, duration));
            moveSeq.Insert(0, TranslateYTween(element, start.y, end.y, duration));
            return moveSeq;
        }

        public Tween TranslateVisualElementToGlobalPosition(VisualElement element, float2 globalTargetPos, float duration)
        {
            float2 newLocalPosition = element.WorldToLocal(globalTargetPos);
            return TranslateVisualElement(element, new float2(element.style.translate.value.x.value, element.style.translate.value.y.value), newLocalPosition, duration);
        }

        public Tween TranslateVisualElementByCurve(VisualElement element, float2 globalMiddle, float2 globalEnd, float resolution, float duration)
        {
            Sequence moveSeq = DOTween.Sequence();
            float2 localStart = element.WorldToLocal(element.layout.center);
            float2 localMiddle = element.WorldToLocal(globalMiddle);
            float2 localEnd = element.WorldToLocal(globalEnd);
            float2 currentPosition = localStart;
            for (float t = 0; t <= 1; t += 1 / (resolution + 1))
            {
                float2 point = CalculateBezierPoint(t, localStart, localMiddle, localEnd);

                moveSeq.Append(TranslateVisualElement(element, currentPosition, point, duration / (resolution + 1)));
                currentPosition = point;
            }
            return moveSeq;
        }

        public Tween MoveVisualElementToGlobalPosition(VisualElement element, float2 startPosition, float2 globalTargetPos, float duration)
        {
            Vector2 newLocalPosition = element.WorldToLocal(globalTargetPos);
            startPosition = element.WorldToLocal(startPosition);
            Vector2 currentPosition = startPosition;
            return DOTween.To(() => currentPosition,
                    x => currentPosition = x,
                    newLocalPosition, duration)
                .OnUpdate(() => element.transform.position = currentPosition);
        }

        public Tween MoveVisualElementByCurve(VisualElement element, float2 globalStart, float2 globalMiddle, float2 globalEnd, float resolution, float duration)
        {
            Sequence moveSeq = DOTween.Sequence();
            float2 currentPosition = globalStart;
            for (float t = 0; t <= 1; t += 1 / (resolution + 1))
            {
                float2 point = CalculateBezierPoint(t, globalStart, globalMiddle, globalEnd);

                moveSeq.Append(MoveVisualElementToGlobalPosition(element, currentPosition, point, duration / (resolution + 1)));
                currentPosition = point;
            }
            return moveSeq;
        }

        private float2 CalculateBezierPoint(float t, float2 p0, float2 p1, float2 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float2 p = uu * p0;
            p += 2 * u * t * p1;
            p += tt * p2;
            return p;
        }

        public Tween MoveWithLeftTopOffsets(VisualElement element, float2 targetLeftTopOffsets, float duration)
        {
            Vector2 start = new Vector2(element.resolvedStyle.left, element.resolvedStyle.top);

            Tween tween = DOTween.To(() => start, x => start = x, targetLeftTopOffsets, duration)
                .OnUpdate(() => UpdatePosition(element, start));

            return tween;
        }

        private void UpdatePosition(VisualElement element, Vector2 offset)
        {
            element.style.left = offset.x;
            element.style.top = offset.y;
        }

        #endregion

        public void AnimateQuestionMark(VisualElement questionMark)
        {
            Tweener tweener = DOTween.To((t) => ChangeQuestionMarkSprite(t, questionMark), 0f, 1f, 0.1f)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
            tweener.Play();
        }

        private void ChangeQuestionMarkSprite(float t, VisualElement questionMark)
        {
            int randomIndex = Random.Range(0, QuestionMarkSprites.Length);
            questionMark.style.backgroundImage = new StyleBackground(QuestionMarkSprites[randomIndex]);
        }

        public Sequence AnimateTowerUpgrade(VisualElement effect)
        {
            Sequence result = DOTween.Sequence();
            for (int i = 0; i < TowerUpgrateSprites.Length; i++)
            {
                int index = i;
                result.InsertCallback(i * .01f, () => SetSprite(index, effect));
            }
            return result;
            void SetSprite(int index, VisualElement element) => element.style.backgroundImage = new StyleBackground(TowerUpgrateSprites[index]);
        }

        public Tween ChangeWidthByPercent(VisualElement element, float start, float end, float duration)
        {
            float percent = start;
            Tween tween = DOTween.To(() => percent, w => percent = w, end, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(() => element.style.width = new StyleLength(Length.Percent(percent)))
                .SetTarget(element);
            return tween;
        }

        public Tween ChangeWidth(VisualElement element, float start, float end, float duration)
        {
            float current = start;
            Tween tween = DOTween.To(() => current, w => current = w, end, duration)
                .OnUpdate(() => element.style.width = new StyleLength(current))
                .SetTarget(element);
            return tween;
        }

        public Tween ChangeHeight(VisualElement element, float start, float end, float duration)
        {
            float current = start;
            Tween tween = DOTween.To(() => current, w => current = w, end, duration)
                .OnUpdate(() => element.style.height = new StyleLength(current))
                .SetTarget(element);
            return tween;
        }

        public Tween ScaleTween(VisualElement element, float start, float end, float duration)
        {
            float scale = start;
            Tween tween = DOTween.To(() => scale, w => scale = w, end, duration)
                .OnUpdate(() => element.style.scale = new StyleScale(new Vector2(scale, scale)));
            return tween;
        }

        public Tween ScaleToSize(VisualElement element, float2 size, float duration)
        {
            Vector2 start = new Vector2(element.style.width.value.value, element.style.height.value.value);

            Tween tween = DOTween.To(() => start, x => start = x, size, duration)
                .OnUpdate(() => ScaleElementToSize(element, start));

            return tween;
        }

        private void ScaleElementToSize(VisualElement element, Vector2 size)
        {
            element.style.width = size.x;
            element.style.height = size.y;
        }

        public Tween InOutScaleTween(VisualElement element, float start, float end, float duration)
        {
            Sequence scaleSeq = DOTween.Sequence();
            scaleSeq.Append(ScaleTween(element, start, end, duration / 2));
            scaleSeq.Append(ScaleTween(element, end, start, duration / 2));
            return scaleSeq;
        }

        public Tween ChangeNumberInLabelTween(Label label, int start, int end, float duration)
        {
            int current = start;
            return DOTween.To(() => current, x => current = x, end, duration).OnUpdate(() =>
            {
                label.text = current.ToString();
            }).SetTarget(label);
        }

        public Tween ChangeBigNumberInLabelTween(Label label, int start, int end, float duration)
        {
            int current = start;
            return DOTween.To(() => current, x => current = x, end, duration).OnUpdate(() =>
            {
                label.text = current.ToStringBigValue();
            }).SetTarget(label);
        }

        public Tween ChangeColorTween(VisualElement element, Color targetColor, float duration)
        {
            element.style.unityBackgroundImageTintColor = new StyleColor(element.resolvedStyle.unityBackgroundImageTintColor);

            return DOTween.To(() => element.style.unityBackgroundImageTintColor.value,
                x => element.style.unityBackgroundImageTintColor = x,
                targetColor,
                duration);
        }

        public void PlayTypewriter(Label label, string text, bool withCarriage = true, ScrollView scrollView = null, float speedMultiplier = 1, bool addBlinkingCarriageOnComplete = false, bool useHeight = false)
        {
            DOTween.Kill(label, true);
            label.style.opacity = 0;
            label.schedule.Execute(() =>
            {
                Vector2 size;
                if (useHeight)
                {
                    size = label.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, label.resolvedStyle.height, VisualElement.MeasureMode.Exactly);
                }
                else
                {
                    size = label.MeasureTextSize(text, label.resolvedStyle.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                }
                label.style.minWidth = size.x;
                label.style.minHeight = size.y;
                label.style.maxWidth = size.x;
                label.style.maxHeight = size.y;
                label.style.opacity = 1;
                PlayTypewriterOnResolve(label, text, withCarriage, scrollView, speedMultiplier, addBlinkingCarriageOnComplete);
            }).StartingIn(100);
        }

        private void PlayTypewriterOnResolve(Label label, string text, bool withCarriage = true, ScrollView scrollView = null, float speedMultiplier = 1, bool addBlinkingCarriageOnComplete = false)
        {
            //MusicManager.Pla
            Tween typewriter = withCarriage ? GetTypewriterTweenWithCarriage(label, text, scrollView, speedMultiplier) : GetTypewriterTween(label, text, null, speedMultiplier);
            typewriter.OnComplete(() => OnTypeWriterComplete(label, addBlinkingCarriageOnComplete));

            typewriter.Play();
        }

        private void OnTypeWriterComplete(Label label, bool addBlinkingCarriageOnComplete)
        {
            StopSound3D(SoundKey.Interface_dialog, Camera.main.transform);
            if (addBlinkingCarriageOnComplete)
                GetBlinkingCarriageTween(label).Play();
        }

        public Tween GetTypewriterTween(Label label, string text, ScrollView scrollView = null, float speedMultiplier = 1)
        {
            label.text = "";
            if (string.IsNullOrEmpty(text)) return DOTween.Sequence(); //Dummy empty Tween

            bool hasScroll = scrollView != null;
            float previousBlinkTime = Time.unscaledTime;
            bool isBlinkOn = true;

            text = ReplaceRichTextTags(text, out string[] foundTags);
            //text = text.Replace("\n", "~");
            int tagsCounter = 0;

            string[] words = text.Split(' ');
            for (int j = 0; j < words.Length - 1; j++)
                words[j] += " ";
            int wordsCounter = 0;

            StringBuilder sb = hasScroll ? new(ReplaceWithEmptySymbol(words[0])) : new(ReplaceWithEmptySymbol(text));

            int i = 0;
            return DOVirtual.DelayedCall(typewriterDelayBetweenChars / speedMultiplier, () =>
                {
                    if (Time.unscaledTime > previousBlinkTime + typewriterCarriageBlinkInterval)
                    {
                        isBlinkOn = !isBlinkOn;
                        previousBlinkTime = Time.unscaledTime;
                    }

                    sb[i] = text[i];
                    if (hasScroll && sb[i] == ' ' && wordsCounter < words.Length - 1)
                    {
                        wordsCounter++;
                        sb.Append(ReplaceWithEmptySymbol(words[wordsCounter]));
                    }

                    if (sb[i] == '&')
                    {
                        sb.Insert(i, foundTags[tagsCounter]);
                        text = text.Insert(i, foundTags[tagsCounter]);
                        i += foundTags[tagsCounter].Length - 1;
                        tagsCounter++;
                        sb.Replace("&", "");
                        text = text.Remove(i, 1);
                    }

                    i++;
                    label.text = sb.ToString();
                })
                .SetLoops(text.Length, LoopType.Restart)
                .SetUpdate(true)
                .SetTarget(label);
        }

        private Tween GetTypewriterTweenWithCarriage(Label label, string text, ScrollView scrollView = null, float speedMultiplier = 1)
        {
            label.text = "";
            if (string.IsNullOrEmpty(text)) return DOTween.Sequence(); //Dummy empty Tween

            bool hasScroll = scrollView != null;
            float previousBlinkTime = Time.unscaledTime;
            bool isBlinkOn = true;

            text = ReplaceRichTextTags(text, out string[] foundTags);
            int tagsCounter = 0;

            string[] words = text.Split(' ');
            for (int j = 0; j < words.Length - 1; j++)
                words[j] += " ";
            int wordsCounter = 0;

            StringBuilder sb = hasScroll ? new(ReplaceWithEmptySymbol(words[0])) : new(ReplaceWithEmptySymbol(text));

            int i = 0;
            return DOVirtual.DelayedCall(typewriterDelayBetweenChars / speedMultiplier, () =>
                {
                    if (Time.unscaledTime > previousBlinkTime + typewriterCarriageBlinkInterval)
                    {
                        isBlinkOn = !isBlinkOn;
                        previousBlinkTime = Time.unscaledTime;
                    }

                    sb[i] = text[i];
                    if (hasScroll && sb[i] == ' ' && wordsCounter < words.Length - 1)
                    {
                        wordsCounter++;
                        sb.Append(ReplaceWithEmptySymbol(words[wordsCounter]));
                    }

                    if (sb[i] == '&')
                    {
                        sb.Insert(i, foundTags[tagsCounter]);
                        text = text.Insert(i, foundTags[tagsCounter]);
                        i += foundTags[tagsCounter].Length - 1;
                        tagsCounter++;
                        sb.Replace("&", "");
                        text = text.Remove(i, 1);
                    }

                    if (sb[i] == '\\')
                    {
                        text = text.Insert(i, "\n");
                        i++;
                    }

                    if (i + 1 < text.Length - 1)
                    {
                        if (sb[i + 1] != ' ')
                            sb[i + 1] = isBlinkOn ? '^' : '@';
                    }

                    i++;
                    label.text = sb.ToString();
                })
                .SetLoops(text.Length, LoopType.Restart)
                .SetUpdate(true)
                .SetTarget(label);
        }

        public string ReplaceWithEmptySymbol(string input)
        {
            char emptySymbol = '@';
            var sb = new StringBuilder(input);

            for (int i = 0; i < sb.Length; i++)
            {
                if (!Char.IsWhiteSpace(sb[i]))
                {
                    sb[i] = emptySymbol;
                }
            }

            return sb.ToString();
        }

        private string ReplaceRichTextTags(string input, out string[] foundTags)
        {
            string pattern = "<.*?>";
            string replacement = "&";
            Regex rgx = new Regex(pattern);

            MatchCollection matches = rgx.Matches(input);
            foundTags = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                foundTags[i] = matches[i].Value;
            }

            string result = rgx.Replace(input, replacement);
            return result;
        }

        public Tween GetBlinkingCarriageTween(Label label)
        {
            bool isBlinkOn = false;
            string carriage = "^";
            float blinkInterval = typewriterCarriageBlinkInterval;
            label.text += "&";
            return DOVirtual.DelayedCall(blinkInterval, () =>
            {
                if (label.text.Length > 0)
                    label.text = label.text.Remove(label.text.Length - 1, 1);
                label.text += (isBlinkOn ? carriage : "&");
                isBlinkOn = !isBlinkOn;
            }, true).SetLoops(-1)
            .SetTarget(label);
        }

        public Tween RotateElement(VisualElement element, float deg, bool clockwise, float duration)
        {
            float startRotation = element.transform.rotation.eulerAngles.z;
            float endRotation = clockwise ? startRotation + deg : startRotation - deg;

            return DOVirtual.Float(startRotation, endRotation, duration, rotation =>
            {
                element.transform.rotation = Quaternion.Euler(0, 0, rotation);
            });
        }

        public void SetLocalizationFont(VisualElement element)
        {
            fallbackFontsHolder.SetFallbackFont(element);
        }

        public bool ElementIsVisible(ScrollView scroll, VisualElement element)
        {
            return element.worldBound.x >= scroll.worldBound.x && element.worldBound.x < scroll.worldBound.x + scroll.layout.width && element.worldBound.y >= scroll.worldBound.y && element.worldBound.y < scroll.worldBound.y + scroll.layout.height;
        }

        public bool ElementIsBehind(ScrollView scroll, VisualElement element)
        {
            return element.worldBound.x <= scroll.worldBound.x;
        }

        [Serializable]
        private class AmmoBgData
        {
            public AllEnums.TowerId Towers;
            public Sprite Bg;
        }
    }

    [Serializable]
    public class WinWindowAnimationData
    {
        [FoldoutGroup("Sprites")] public Sprite ActiveStarSprite;
        [FoldoutGroup("Sprites")] public Sprite InactiveStarSprite;
        [FoldoutGroup("PowerCells")] public float MinCellAnimationLength;
        [FoldoutGroup("PowerCells")] public float MaxCellAnimationLength;
        [FoldoutGroup("PowerCells")] public float CellsCountModifierBase;

        [FoldoutGroup("PowerCells")][Range(0, 2000)] public float HorizontalDeviation;
        [FoldoutGroup("PowerCells")][Range(0, 1)] public float MinCellsAnimationOverlap;
        [FoldoutGroup("PowerCells")][Range(0, 1)] public float MaxCellsAnimationOverlap;
        [FoldoutGroup("PowerCells")] public float PowerCellStartScale;
        [FoldoutGroup("PowerCells")] public float PowerCellMidScale;

        [FoldoutGroup("Sparks")] public float SparksMidScale;
        [FoldoutGroup("Sparks")] public float SparksEndScale;

        [FoldoutGroup("ProgressBar")] public float ProgressBarScale;
        [FoldoutGroup("ProgressBar")] public float ProgressBarScaleTime;

        [FoldoutGroup("Rewards")] public float RewardScale;
        [FoldoutGroup("Rewards")] public float RewardScaleTime;

        [FoldoutGroup("IncreaseReward")] public float IncreaseRewardDuration;
        [FoldoutGroup("IncreaseReward")] public float IncreaseRewardDelay;
    }

    [Serializable]
    public class DirectivesColorData
    {
        [SerializeField] private Color red;
        [SerializeField] private Color green;
        [SerializeField] private Color purple;
        [SerializeField] private Color yellow;
        [SerializeField] private Color blue;

        [SerializeField] private List<WeaponPart> redDirectives;
        [SerializeField] private List<WeaponPart> greenDirectives;
        [SerializeField] private List<WeaponPart> purpleDirectives;
        [SerializeField] private List<WeaponPart> yellowDirectives;
        [SerializeField] private List<WeaponPart> blueDirectives;

        private Dictionary<WeaponPart, Color> dict;

        public Color GetDirectiveColor(WeaponPart directive)
        {
            if (dict == null)
            {
                dict = new();
                foreach (var dir in redDirectives)
                    dict.TryAdd(dir, red);
                foreach (var dir in greenDirectives)
                    dict.TryAdd(dir, green);
                foreach (var dir in purpleDirectives)
                    dict.TryAdd(dir, purple);
                foreach (var dir in yellowDirectives)
                    dict.TryAdd(dir, yellow);
                foreach (var dir in blueDirectives)
                    dict.TryAdd(dir, blue);
            }

            if (dict.TryGetValue(directive, out var color))
                return color;
            else
            {
                Debug.LogError($"{directive.name} not present in color dictionary");
                return Color.white;
            }
        }
    }

    [Serializable]
    public class NextWaveAnnouncementAnimationData
    {
        public float FadeTime;
        public float OpeningTime;
        public float IdleTime;
        public float TypewriterTime;
    }

    [Serializable]
    public class CashWidgetAnimationData
    {
        public float ProgressBarChangeTime;
        public float CashLabelChangeTime;
        public float GainLabelFadeTime;
        public float GainLabelIdleTime;
        public int GainLabelTranslateYValue;
        public float GainLabelTranslateTime;
    }

    [Serializable]
    public class StatsWidgetData
    {
        public float2 FirerateRange;
        public float2 ForceRange;
        public float2 PenetrationRange;
        public float2 RicochetRange;
        public float2 ReloadSpeedRange;
        public float2 ReloadCostRange;
        public float2 AOERange;
        public float2 AccuracyRange;
        [Space] public float LineStatChangeTime;
    }

    [Serializable]
    public class PopUpTextAnimationData
    {
        public float ScaleTime;
        public float FadeInTime;
        public float MoveYTime;
        public float MoveYDistance;
        public float IdleTime;
        public float FadeOutTime;
    }

    [Serializable]
    public class RewardWindowAnimationData
    {
        [FoldoutGroup("Button")] public float ButtonChangeColorDuration;
        [FoldoutGroup("Button")] public float ButtonChangeWidthDuration;
        [FoldoutGroup("Button")] public float2 ButtonPulseScaleInterval;
        [FoldoutGroup("Button")] public float ButtonPulseScaleDuration;
        [FoldoutGroup("Button")] public float ButtonFadeLabelDuration;
        [FoldoutGroup("Button")] public float ButtonFadeMarkDuration;
        [FoldoutGroup("Window")] public float2 PulseScaleInterval;
        [FoldoutGroup("Window")] public float PulseScaleDuration;
        [FoldoutGroup("Window")] public float ScaleOnClickValue;
        [FoldoutGroup("Window")] public float ScaleOnClickDuration;
        [FoldoutGroup("Window")] public float ChangeNumberDuration;
        [FoldoutGroup("Window")] public float NumberFadeDuration;
        [FoldoutGroup("Window")] public float ChangeHeightDuration;
        [FoldoutGroup("Window")] public float ChangeColorDuration;
        [FoldoutGroup("Window")] public float ScaleOutDuration;
    }
}