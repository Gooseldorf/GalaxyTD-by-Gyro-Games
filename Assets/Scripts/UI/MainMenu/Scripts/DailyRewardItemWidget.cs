using CardTD.Utilities;
using Data.Managers;
using DG.Tweening;
using I2.Loc;
using Sounds.Attributes;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class DailyRewardItemWidget : ClickableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<DailyRewardItemWidget> {}

        private VisualElement container;
        private VisualElement content;
        private VisualElement glow;
        
        private Label dayLabel;
        private int cachedDay;
        public VisualElement RewardIcon;
        private VisualElement questionMark;
        private VisualElement mark;
        private Label rewardLabel;
        private VisualElement frame;
        private VisualElement locker;
        
        private Sequence takeSeq;
        private int cachedRewardAmount = 0;
        private UIHelper uiHelper;
        private RewardWindowAnimationData animData;
        public DailyReward Reward;
        
        public override void Init()
        {
            base.Init();
            SoundName = SoundConstants.EmptyKey;
            container = this.Q<VisualElement>("Container");
            content = this.Q<VisualElement>("ContentContainer");
            glow = this.Q<VisualElement>("Glow");
            dayLabel = content.Q<Label>("DayLabel");
            RewardIcon = content.Q<VisualElement>("RewardIcon");
            rewardLabel = content.Q<Label>("RewardLabel");
            questionMark = RewardIcon.Q<VisualElement>("QuestionMark");
            mark = RewardIcon.Q<VisualElement>("Mark");
            frame = this.Q<VisualElement>("Frame");
            locker = this.Q<VisualElement>("Lock");
            
            uiHelper = UIHelper.Instance;
            animData = uiHelper.RewardWindowAnimationData;
            SetState(AllEnums.UIState.Locked);
        }

        public override void SetState(AllEnums.UIState state)
        {
            base.SetState(state);
            switch (state)
            {
                case AllEnums.UIState.Active:
                    content.style.opacity = 1;
                    glow.style.unityBackgroundImageTintColor = uiHelper.StarsBackgroundColor;
                    frame.style.unityBackgroundImageTintColor = Color.white;
                    mark.style.opacity = 1;
                    locker.style.opacity = 0;
                    break;
                case AllEnums.UIState.Available:
                    content.style.opacity = 1;
                    rewardLabel.style.display = DisplayStyle.Flex;
                    frame.style.unityBackgroundImageTintColor = Color.white;
                    uiHelper.InOutScaleTween(container, animData.PulseScaleInterval.x, animData.PulseScaleInterval.y, animData.PulseScaleDuration).SetLoops(-1).SetTarget(container).Play();
                    locker.style.opacity = 0;
                    break;
                default:
                    return;
            }
        }

        public void SetDay(int day)
        {
            this.cachedDay = day;
            dayLabel.text = LocalizationManager.GetTranslation("Menu/Day") + $" {day}";
        }

        public void SetReward(DailyReward reward)
        {
            this.Reward = reward;
            cachedRewardAmount = reward.GetRewardAmount;
            
            RewardIcon.style.backgroundImage = MainMenuDaily.GetSprite(reward.DailyRewardType);
            rewardLabel.text = cachedRewardAmount.ToStringBigValue();
            
            switch (reward.DailyRewardType)
            {
                case DailyRewardType.Ticket:
                    rewardLabel.style.visibility = Visibility.Hidden;
                    break;
                
                case DailyRewardType.RandomDirective:
                    rewardLabel.style.visibility = Visibility.Hidden;
                    SetDirectiveReward();
                    break;
                default:
                    break;
            }
        }

        private void SetDirectiveReward()
        {
            RewardIcon.transform.scale = new Vector3(1, 1);
            if (State == AllEnums.UIState.Locked)
            {
                questionMark.style.display = DisplayStyle.Flex;
                uiHelper.AnimateQuestionMark(questionMark);
            }
            else
                questionMark.style.display = DisplayStyle.None;
        }

        public void PlayTakeAnimation()
        {
            State = AllEnums.UIState.Active;
            DOTween.Kill(container);
            takeSeq = DOTween.Sequence();
            takeSeq.Append(uiHelper.ScaleTween(container, container.transform.scale.x, animData.ScaleOnClickValue, animData.ScaleOnClickDuration));
            if (Reward.Value > 0)
            {
                takeSeq.Join(uiHelper.ChangeBigNumberInLabelTween(rewardLabel, cachedRewardAmount, 0, animData.ChangeNumberDuration));
                takeSeq.Append(uiHelper.FadeTween(rewardLabel, 1, 0, animData.NumberFadeDuration));
                takeSeq.Append(uiHelper.FadeTween(mark, 0, 1, animData.ButtonFadeMarkDuration));
            }

            takeSeq.Append(DOVirtual.DelayedCall(0, () =>
            {
                container.style.height = container.resolvedStyle.maxHeight.value;
                rewardLabel.style.display = DisplayStyle.None;
            }));
            takeSeq.Append(uiHelper.ChangeHeight(container, container.resolvedStyle.maxHeight.value, container.resolvedStyle.minHeight.value, animData.ChangeHeightDuration));
            takeSeq.Join(uiHelper.ChangeColorTween(glow, uiHelper.StarsBackgroundColor, animData.ChangeColorDuration));
            //takeSeq.Join(uiHelper.ChangeColorTween(frame, Color.gray, animData.ChangeColorDuration));
            takeSeq.Append(uiHelper.ScaleTween(container, animData.ScaleOnClickValue, 1, animData.ScaleOutDuration));
        }

        public void UpdateLocalization()
        {
            dayLabel.text = LocalizationManager.GetTranslation("Menu/Day") + $" {cachedDay}";
            uiHelper.SetLocalizationFont(dayLabel);
        }
    }
}
