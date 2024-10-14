using CardTD.Utilities;
using Data.Managers;
using DG.Tweening;
using I2.Loc;
using Managers;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class WinWindow : VisualElement
    {
        #region UxmlStaff

        public new class UxmlFactory : UxmlFactory<WinWindow, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlAssetAttributeDescription<Sprite> ticketIcon = new() {name = "TicketIcon", defaultValue = null};
            UxmlAssetAttributeDescription<Sprite> adIcon = new() {name = "AdIcon", defaultValue = null};
            UxmlAssetAttributeDescription<Sprite> adButtonBackground = new() {name = "Ad_Button_Background", defaultValue = null};

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ticketIcon.TryGetValueFromBag(bag, cc, out Sprite value))
                    ((WinWindow)ve).TicketIcon = value;
                if (adIcon.TryGetValueFromBag(bag, cc, out Sprite value1))
                    ((WinWindow)ve).AdIcon = value1;
                if (adButtonBackground.TryGetValueFromBag(bag, cc, out Sprite value2))
                    ((WinWindow)ve).AdButtonBackground = value2;
            }
        }

        public Sprite TicketIcon { get; set; }
        public Sprite AdIcon { get; set; }
        public Sprite AdButtonBackground { get; set; }

        #endregion

        private Label title;
        private VisualElement windowContainer;

        private VisualElement ticketsContainer;
        private Label ticketsLabel;

        private CommonButton restartButton;
        private CommonButton menuButton;
        private CommonButton increaseRewardButton;
        private Label increaseRewardLabel;
        private VisualElement powerCellsAnimationTarget;
        private List<VisualElement> currencyRewards = new();

        private VisualElement progressBarFilling;
        private List<VisualElement> powerCells = new();
        private VisualElement itemReward;

        private List<VisualElement> stars = new();

        private VisualElement questionMark;

        private VisualElement sparks;
        private Tweener sparksTweener;
        private Sprite[] sparkSprites;

        private VisualElement progressBarContainer;
        private VisualElement powerCellDeviationPoint;

        private VisualElement raycastBlocker;

        private LivesWidget livesWidget;
        private UIHelper uiHelper;
        private GameData gameData;
        private IReadOnlyDictionary<int, int> starsDict;
        private int missionIndex;
        private int currencyReward;
        private int hardCurrencyReward;
        private int replayReward;
        private int rewardToIncrease;

        private Reward reward; 

        private Sprite replayRewardSprite;
        private int replayRewardCounter;
        private int starsForWin;
        private int maxPowerCells;
        private Sequence powerCellsSeq;
        private Sequence rewardsSeq;
        private Sequence increaseRewardSeq;
        private WinWindowAnimationData winAnimData;
        private int resolveCounter = 0;
        private VisualElement adsIcon;

        public void Init(LivesWidget livesWidget)
        {
            this.livesWidget = livesWidget;

            title = this.Q<Label>("Title");
            windowContainer = this.Q<VisualElement>("Container");

            ticketsContainer = this.Q<VisualElement>("TicketsContainer");
            ticketsLabel = ticketsContainer.Q<Label>("TicketsLabel");
            ticketsContainer.style.display = GameServices.Instance.IsHard ? DisplayStyle.Flex : DisplayStyle.None;

            restartButton = this.Q<TemplateContainer>("RestartButton").Q<CommonButton>();
            restartButton.Init();
            restartButton.RegisterCallback<ClickEvent>(OnRestartClick);

            menuButton = this.Q<TemplateContainer>("MenuButton").Q<CommonButton>();
            menuButton.Init();
            menuButton.RegisterCallback<ClickEvent>(OnMenuClick);

            increaseRewardButton = this.Q<CommonButton>("IncreaseRewardButton");
            adsIcon = increaseRewardButton.Q<VisualElement>("Icon");
            increaseRewardButton.Init();
            increaseRewardLabel = increaseRewardButton.Q<Label>();
            increaseRewardButton.RegisterCallback<ClickEvent>(OnIncreaseClick);

            currencyRewards = this.Query<VisualElement>("CurrencyContainer").ToList();
            itemReward = this.Q<VisualElement>("DirectiveContainer");
            questionMark = itemReward.Q<VisualElement>("QuestionMark");
            sparks = this.Q<VisualElement>("Sparkles");

            stars = this.Query<VisualElement>("Star").ToList();

            progressBarFilling = this.Q<VisualElement>("ProgressBarFilling");
            powerCellsAnimationTarget = this.Q<VisualElement>("PowerCellsAnimationTarget");
            progressBarContainer = this.Q<VisualElement>("ProgressBarContainer");
            powerCellDeviationPoint = this.Q<VisualElement>("PowerCellsDeviationPoint");

            raycastBlocker = this.parent.Q<VisualElement>("RaycastBlocker");
            raycastBlocker.RegisterCallback<ClickEvent>(SkipAnimation);

            uiHelper = UIHelper.Instance;
            winAnimData = uiHelper.WinWindowData;
            sparkSprites = UIHelper.Instance.SparksSprites;
            gameData = DataManager.Instance.GameData;
            starsDict = GameServices.Instance.IsHard ? gameData.HardStars : gameData.Stars;
            missionIndex = GameServices.Instance.CurrentMission.MissionIndex;
            currencyReward = GameServices.Instance.IsHard ? GameServices.Instance.CurrentMission.Reward.Scrap : GameServices.Instance.CurrentMission.Reward.SoftCurrency;

            //TODO: redo it
            //replayReward = GameServices.Instance.IsHard ? GameServices.Instance.CurrentMission.Reward.ScrapReplay : GameServices.Instance.CurrentMission.Reward.SoftReplay;
            replayRewardSprite = GameServices.Instance.IsHard ? uiHelper.ScrapReward : uiHelper.SoftCurrencyReward;
            style.display = DisplayStyle.None;
        }

        public void Dispose()
        {
            restartButton.Dispose();
            restartButton.UnregisterCallback<ClickEvent>(OnRestartClick);

            menuButton.Dispose();
            menuButton.UnregisterCallback<ClickEvent>(OnMenuClick);

            increaseRewardButton.Dispose();
            increaseRewardButton.UnregisterCallback<ClickEvent>(OnIncreaseClick);

            raycastBlocker.UnregisterCallback<ClickEvent>(SkipAnimation);
        }

        public void Show(bool show, int starsForWin, int maxCells, Action onAnimationComplete)
        {
            this.starsForWin = starsForWin;
            this.maxPowerCells = maxCells;
            EnableRaycastBlocker(true);
            PlayWinSound();

            if (GameServices.Instance.IsHard)
            {
                restartButton.SetIcon(gameData.Tickets > 0 ? TicketIcon.texture : AdIcon.texture);
                if(!DataManager.Instance.GameData.SkipAds)
                    restartButton.SetBackground(gameData.Tickets > 0 ? uiHelper.AvailableCommonButtonBackground : AdButtonBackground);
                ticketsLabel.text = DataManager.Instance.GameData.Tickets < 99 ? DataManager.Instance.GameData.Tickets.ToString() : "99+";
                restartButton.ShowIcon(!DataManager.Instance.GameData.SkipAds);
            }
            else
                restartButton.ShowIcon(false);

            Tween showWindowTween = show ? uiHelper.GetShowWindowTween(windowContainer, this) : uiHelper.GetHideWindowTween(windowContainer, this);
            showWindowTween.SetUpdate(true);
            showWindowTween.OnComplete(() =>
            {
                onAnimationComplete?.Invoke();
                if (show)
                {
                    powerCells = livesWidget.GetActivePowerCellsForAnimation();

                    //FOR CHEATS //TODO: REMOVE???
                    if (InGameCheats.WinWithCheats)
                    {
                        while (powerCells.Count > InGameCheats.PowerCellsForWin)
                        {
                            powerCells.RemoveAt(powerCells.Count - 1);
                        }

                        InGameCheats.WinWithCheats = false;
                        InGameCheats.PowerCellsForWin = 0;
                    }

                    if (CheckResolvedStyle())
                    {
                        PlayPowerCellsAnimation();
                    }
                    //PlaySound(SoundKey.Interface_victory);
                }
            });
            showWindowTween.Play();

            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, showWindowTween.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);

            if (!show) return;


            if (!DataManager.Instance.GameData.SkipAds)
                AdsManager.LoadReward(AdsRewardType.IncreaseReward);

            SetRewards();

            UIHelper.Instance.AnimateQuestionMark(questionMark);
        }

        private bool CheckResolvedStyle()
        {
            bool cellsResolved = true;
            bool animationTargetResolved = true;
            foreach (var cell in powerCells)
            {
                if (float.IsNaN(cell.resolvedStyle.width) || cell.resolvedStyle.width == 0)
                {
                    resolveCounter++;
                    cellsResolved = false;
                    cell.RegisterCallback<GeometryChangedEvent>(PlayAnimationOnResolve);
                    break;
                }
            }

            if (float.IsNaN(powerCellsAnimationTarget.resolvedStyle.width) || powerCellsAnimationTarget.resolvedStyle.width == 0)
            {
                resolveCounter++;
                animationTargetResolved = false;
                powerCellsAnimationTarget.RegisterCallback<GeometryChangedEvent>(PlayAnimationOnResolve);
            }

            return cellsResolved && animationTargetResolved;
        }

        private void PlayAnimationOnResolve(GeometryChangedEvent geom)
        {
            resolveCounter--;
            ((VisualElement)geom.currentTarget).UnregisterCallback<GeometryChangedEvent>(PlayAnimationOnResolve);
            if (resolveCounter <= 0)
                PlayPowerCellsAnimation();
        }

        private void SetRewards()
        {
            Mission mission = DataManager.Instance.Get<MissionList>().GetMissionByIndex(missionIndex);
            replayRewardCounter = 0;
            currencyRewards[0].Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(replayRewardSprite);
            currencyRewards[0].Q<Label>("Label").text = currencyReward.ToStringBigValue();
            currencyRewards[1].Q<Label>("Label").text = mission.Reward.HardCurrency.ToStringBigValue();
            hardCurrencyReward = starsForWin >= 2 ? mission.Reward.HardCurrency : 0;
            rewardToIncrease = currencyReward;
            for (int i = 1; i < currencyRewards.Count; i++)
            {
                currencyRewards[i].Q<VisualElement>("Icon").style.unityBackgroundImageTintColor = Color.gray;
                currencyRewards[i].Q<Label>("Label").style.color = Color.gray;
            }

            itemReward.Q<VisualElement>("Icon").style.unityBackgroundImageTintColor = Color.gray;

            if (starsDict.ContainsKey(missionIndex))
            {
                //change icons
                for (int i = 0; i < starsDict[missionIndex]; i++)
                {
                    currencyRewards[i].Q<VisualElement>("Icon").style.backgroundImage =
                        GameServices.Instance.IsHard ? new StyleBackground(uiHelper.ScrapReward) : new StyleBackground(uiHelper.SoftCurrencyReward);
                    replayRewardCounter++;
                }
                //Calculate reward to display
                rewardToIncrease = 0;
                for (int i = 0; i < replayRewardCounter; i++)
                {
                    int rewardToDisplay = (int)(replayReward * 1f / 6f * (i + 1));
                    currencyRewards[i].Q<Label>("Label").text = rewardToDisplay.ToStringBigValue();
                    if (i < starsForWin) rewardToIncrease += rewardToDisplay;
                }
                // check if we need to display item reward
                if (starsDict[missionIndex] >= 3)
                {
                    currencyRewards[^1].style.display = DisplayStyle.Flex;
                    itemReward.style.display = DisplayStyle.None;
                }

                hardCurrencyReward = replayRewardCounter <= 1 &&  starsForWin >= 2 ? mission.Reward.HardCurrency : 0; //TODO: CHECK
            }

            if (gameData.SelectedRewards.ContainsKey(missionIndex))
            {
                List<WeaponPart> rewardItems = DataManager.Instance.Get<UnlockManager>().GetRewardsForChoose(missionIndex);

                itemReward.Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(rewardItems[1 - gameData.SelectedRewards[missionIndex]].Sprite);
                questionMark.style.display = DisplayStyle.None;
                if (rewardItems[0].PartType == AllEnums.PartType.Ammo)
                    itemReward.Q<VisualElement>("Icon").style.scale = new StyleScale(new Vector2(0.7f, 0.7f));
            }
        }

        private void ChangeSparkSprite(float t)
        {
            int randomIndex = Random.Range(0, sparkSprites.Length);
            sparks.style.backgroundImage = new StyleBackground(sparkSprites[randomIndex]);
        }

        private void PlayPowerCellsAnimation()
        {
            powerCellsSeq = DOTween.Sequence();
            rewardsSeq = DOTween.Sequence();
            float progressBarOneCellPercent = 100f / maxPowerCells;
            float cellsForOneStar = (float)maxPowerCells / 3;
            float2 currentXOffset = float2.zero;
            float stepXBase = powerCellsAnimationTarget.resolvedStyle.width / maxPowerCells;
            float targetOffsetY = -powerCellsAnimationTarget.layout.height / 2;
            int starsCounter = 1;
            float delay = winAnimData.ProgressBarScaleTime;

            float cellsCountModifier = winAnimData.CellsCountModifierBase / powerCells.Count > 1 ? 1 : winAnimData.CellsCountModifierBase / powerCells.Count;
            float currentCellAnimationLength = winAnimData.MaxCellAnimationLength * cellsCountModifier;
            float overlap = winAnimData.MinCellsAnimationOverlap;

            int halfWay = powerCells.Count / 2 > 0 ? powerCells.Count / 2 : 1;
            float lengthStep = (winAnimData.MaxCellAnimationLength - winAnimData.MinCellAnimationLength) / halfWay * cellsCountModifier;
            float overlapStep = (winAnimData.MaxCellsAnimationOverlap - winAnimData.MinCellsAnimationOverlap) / halfWay * cellsCountModifier;

            // DOTween.SetTweensCapacity(500,125);

            for (int i = 0; i < powerCells.Count; i++)
            {
                if (i < halfWay) // First half of the loop
                {
                    currentCellAnimationLength -= lengthStep;
                    overlap += overlapStep;
                }
                else
                {
                    currentCellAnimationLength += lengthStep;
                    overlap -= overlapStep;
                }

                if (i == 0) InsertRewardTween(delay, currentCellAnimationLength, 0);

                if (i > 0) // add delay only after the first animation
                {
                    float actualOverlap = currentCellAnimationLength * overlap;
                    delay += currentCellAnimationLength - actualOverlap;
                    currentXOffset = new float2(stepXBase * (i - 1), targetOffsetY);
                }

                //CellMoveTween
                VisualElement cell = powerCells[i];
                float2 middlePosition = (float2)powerCellDeviationPoint.LocalToWorld(powerCellDeviationPoint.layout.center) +
                                        new float2(Random.Range(-winAnimData.HorizontalDeviation, winAnimData.HorizontalDeviation));
                float2 endPosition = (float2)sparks.LocalToWorld(sparks.layout.position) + currentXOffset;
                Tween cellMoveTween = uiHelper.MoveVisualElementByCurve(cell, cell.LocalToWorld(cell.layout.center), middlePosition, endPosition, 10, currentCellAnimationLength)
                    .OnComplete(() => cell.style.display = DisplayStyle.None);
                powerCellsSeq.Insert(delay, cellMoveTween);
                powerCellsSeq.InsertCallback(delay, () => PlaySound2D(SoundKey.Cell_win_count));
                powerCellsSeq.Insert(delay, uiHelper.InOutScaleTween(cell, winAnimData.PowerCellStartScale, winAnimData.PowerCellMidScale, currentCellAnimationLength));

                //ProgressBarTween
                Tween increaseProgressBarTween = uiHelper.ChangeWidthByPercent(progressBarFilling, progressBarOneCellPercent * i, progressBarOneCellPercent * (i + 1), currentCellAnimationLength);
                powerCellsSeq.Insert(delay + currentCellAnimationLength, increaseProgressBarTween);

                //SparksTween
                InsertSparksTween(i, currentCellAnimationLength, delay);

                //RewardTween
                if (i >= (cellsForOneStar * starsCounter) - 1 && starsCounter < 3)
                {
                    InsertRewardTween(delay, currentCellAnimationLength, starsCounter);
                    starsCounter++;
                }
            }

            float totalLength = delay + currentCellAnimationLength;
            powerCellsSeq.Insert(0, uiHelper.ScaleTween(progressBarContainer, 1, winAnimData.ProgressBarScale, winAnimData.ProgressBarScaleTime));
            powerCellsSeq.Insert(totalLength, DOVirtual.DelayedCall(0, () =>
            {
                if (powerCells.Count >= maxPowerCells)
                    PlaySound2D(SoundKey.Cell_win_full);
            }));
            powerCellsSeq.Insert(totalLength + 1, uiHelper.ScaleTween(progressBarContainer, winAnimData.ProgressBarScale, 1, winAnimData.ProgressBarScaleTime));
            powerCellsSeq.Insert(totalLength, uiHelper.ScaleTween(sparks, winAnimData.SparksEndScale, 0.01f, 1).OnComplete(() => sparks.style.display = DisplayStyle.None));
            powerCellsSeq.SetUpdate(true);
            powerCellsSeq.OnComplete(() =>
            {
                EnableRaycastBlocker(false);
            });
            powerCellsSeq.Play();
            rewardsSeq.SetUpdate(true);
            rewardsSeq.Play();

            sparksTweener = DOTween.To(ChangeSparkSprite, 0f, 1, 0.1f).SetLoops(-1, LoopType.Restart).SetUpdate(true);
            sparksTweener.Play();
        }

        private void InsertSparksTween(int i, float currentCellAnimationLength, float delay)
        {
            Tween increaseSparksTween = null;
            if (i == 0)
                increaseSparksTween = uiHelper.ScaleTween(sparks, 0, winAnimData.SparksMidScale, currentCellAnimationLength);
            else
                increaseSparksTween = uiHelper.ScaleTween(sparks, winAnimData.SparksMidScale, winAnimData.SparksEndScale, currentCellAnimationLength);

            powerCellsSeq.Insert(delay + currentCellAnimationLength, increaseSparksTween);
        }

        private void InsertRewardTween(float delay, float currentCellAnimationLength, int counter)
        {
            float startTime = delay + currentCellAnimationLength;
            rewardsSeq.Insert(startTime, DOVirtual.DelayedCall(0, () =>
            {
                int index = counter;
                stars[index].style.backgroundImage = new StyleBackground(winAnimData.ActiveStarSprite);
                stars[index].parent.Q<VisualElement>("Background").style.unityBackgroundImageTintColor = new StyleColor(uiHelper.StarsBackgroundColor);
                currencyRewards[index].Q<VisualElement>("Icon").style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                currencyRewards[index].Q<Label>("Label").style.color = new StyleColor(Color.white);
                currencyRewards[index].Q<VisualElement>("Background").style.unityBackgroundImageTintColor = new StyleColor(uiHelper.StarsBackgroundColor);
                if (index >= 2)
                {
                    itemReward.Q<VisualElement>("Background").style.unityBackgroundImageTintColor = new StyleColor(uiHelper.StarsBackgroundColor);
                    itemReward.Q<VisualElement>("Icon").style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                }
            }));

            rewardsSeq.Insert(startTime, uiHelper.InOutScaleTween(stars[counter], 1, winAnimData.RewardScale, winAnimData.RewardScaleTime));
            rewardsSeq.Insert(startTime, uiHelper.InOutScaleTween(currencyRewards[counter].Q<VisualElement>("Icon"), 1, winAnimData.RewardScale, winAnimData.RewardScaleTime));
            rewardsSeq.Insert(startTime, uiHelper.InOutScaleTween(currencyRewards[counter].Q<Label>("Label"), 1, winAnimData.RewardScale, winAnimData.RewardScaleTime));
            if (counter >= 3)
                rewardsSeq.Insert(startTime, uiHelper.InOutScaleTween(itemReward.Q<VisualElement>("Icon"), 1, winAnimData.RewardScale, winAnimData.RewardScaleTime));
        }

        private Sequence IncreaseRewardAnimation()
        {
            Sequence seq = DOTween.Sequence();
            int counter = 0;
            
            for (int i = 0; i < starsForWin /*currencyRewards.Count*/; i++)
            {
                /*if ((currencyRewards[i].Q<VisualElement>("Icon").style.backgroundImage.value.sprite == replayRewardSprite && i <= starsForWin - 1) || i == 1 && hardCurrencyReward > 0)
                {*/
                    seq.Insert(0 + winAnimData.IncreaseRewardDelay * i,
                        uiHelper.InOutScaleTween(currencyRewards[i].Q<VisualElement>("Icon"), 1, winAnimData.RewardScale, winAnimData.IncreaseRewardDuration));
                    seq.Insert(0 + winAnimData.IncreaseRewardDelay * i,
                        uiHelper.InOutScaleTween(currencyRewards[i].Q<VisualElement>("Label"), 1, winAnimData.RewardScale, winAnimData.IncreaseRewardDuration));
                    counter++;
                /*}*/
            }

            for (int j = 0; j < counter; j++)
            {
                if(!int.TryParse(currencyRewards[j].Q<Label>("Label").text, out int start))
                    continue;
                if (j <= starsForWin - 1)
                    seq.Insert(winAnimData.IncreaseRewardDelay + winAnimData.IncreaseRewardDelay * j,
                        uiHelper.ChangeNumberInLabelTween(currencyRewards[j].Q<Label>("Label"), start, start * 2, winAnimData.IncreaseRewardDuration));
            }

            seq.SetUpdate(true);
            return seq;
        }

        private void SkipAnimation(ClickEvent clk)
        {
            if (powerCellsSeq != null && powerCellsSeq.IsActive() && !powerCellsSeq.IsComplete())
                powerCellsSeq.Complete(true);

            if (increaseRewardSeq != null && increaseRewardSeq.IsActive() && !increaseRewardSeq.IsComplete())
                increaseRewardSeq.Complete(true);

            if (rewardsSeq != null && rewardsSeq.IsActive() && !rewardsSeq.IsComplete())
                rewardsSeq.Complete(true);
            
            raycastBlocker.style.display = DisplayStyle.None;
        }

        public void Reset()
        {
            increaseRewardButton.visible = true;
            adsIcon.visible = !DataManager.Instance.GameData.SkipAds;
            powerCellsSeq.Kill(true);
            rewardsSeq.Kill(true);
            increaseRewardSeq.Kill(true);

            sparks.style.scale = new StyleScale(new Vector2(0.01f, 0.01f));
            sparks.style.display = DisplayStyle.Flex;
            for (int i = 0; i < stars.Count; i++)
            {
                stars[i].style.backgroundImage = new StyleBackground(winAnimData.InactiveStarSprite);
                stars[i].parent.Q<VisualElement>("Background").style.unityBackgroundImageTintColor = new StyleColor(uiHelper.StarsBackgroundColorGray);
                currencyRewards[i].style.unityBackgroundImageTintColor = new StyleColor(uiHelper.StarsBackgroundColorGray);
                currencyRewards[i].Q<VisualElement>("Background").style.unityBackgroundImageTintColor = new StyleColor(uiHelper.StarsBackgroundColorGray);
            }

            foreach (VisualElement powerCell in powerCells)
                powerCell.RemoveFromHierarchy();

            itemReward.Q<VisualElement>("Background").style.unityBackgroundImageTintColor = new StyleColor(uiHelper.StarsBackgroundColorGray);
            progressBarFilling.style.width = new StyleLength(Length.Percent(0));
        }

        private void OnRestartClick(ClickEvent clk)
        {
            PauseWindow.OnRestartButtonClick(restartButton, Restart);
        }

        private void Restart()
        {
            GameServices.Instance.SetPause(false);
            Show(false, 0, 0, () =>
            {
                Reset();
                GameServices.Instance.Restart();
            });
        }

        private void OnMenuClick(ClickEvent clk)
        {
            GameServices.Instance.SetPause(false);
            GameServices.Instance.ReturnToMenu();
        }

        private void OnIncreaseClick(ClickEvent clk)
        {
            AdsManager.TryShowReward(
                () =>
                {
                    increaseRewardButton.visible = false;
                    adsIcon.visible = false;
                },
                () =>
                {
                    EnableRaycastBlocker(true);
                    increaseRewardSeq = IncreaseRewardAnimation();
                    increaseRewardSeq.OnComplete(() =>
                    {
                        /*int hardReward*/
                        GameServices.Instance.IncreaseReward();
                        EnableRaycastBlocker(false);
                    });

                    if (GameServices.Instance.IsHard && DataManager.Instance.GameData.Tickets <= 0)
                        AdsManager.LoadReward(AdsRewardType.GetTicket);
                });
        }

        private void EnableRaycastBlocker(bool enable) => raycastBlocker.style.display = enable ? DisplayStyle.Flex : DisplayStyle.None;

        public void UpdateLocalization()
        {
            title.text = LocalizationManager.GetTranslation("Victory");
            uiHelper.SetLocalizationFont(title);
            restartButton.SetText(LocalizationManager.GetTranslation("Restart"));
            menuButton.SetText(LocalizationManager.GetTranslation("Menu"));
            increaseRewardLabel.text = LocalizationManager.GetTranslation("IncreaseReward");
            uiHelper.SetLocalizationFont(increaseRewardLabel);
        }
    }
}