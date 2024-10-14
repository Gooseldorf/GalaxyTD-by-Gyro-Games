using CardTD.Utilities;
using Data.Managers;
using DG.Tweening;
using I2.Loc;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class DailyRewardButton : ClickableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<DailyRewardButton>
        {
        }

        private VisualElement image;
        private Label label;
        private VisualElement mark;
        private float cachedWidth;
        private Sequence seq;
        private Tween inOutScaleTween;
        private UIHelper uiHelper;
        private RewardWindowAnimationData animData;

        public override void Init()
        {
            uiHelper = UIHelper.Instance;
            SoundName = SoundKey.Menu_mission_dailyButton;

            animData = uiHelper.RewardWindowAnimationData;
            label = this.Q<Label>();
            image = this.Q<VisualElement>("image");
            mark = this.Q<VisualElement>("Mark");

            DailyRewardType rewardType = MainMenuDaily.GetLastReward.DailyRewardType;
            image.style.backgroundImage = MainMenuDaily.GetSprite(rewardType);
            if (rewardType == DailyRewardType.RandomDirective)
            {
                UpdateDirectiveRewardSprite();
            }

            Messenger.AddListener(UIEvents.LoadingCompleted, Resolve);
        }

        public override void Dispose()
        {
            Messenger.RemoveListener(UIEvents.DailyRewardsReceived, ShowMark);
            Messenger.RemoveListener(UIEvents.LoadingCompleted, Resolve);
        }

        private void Resolve()
        {
            bool isRewardReadyToTake = false;
            MainMenuDaily.GetRewards().ForEach(r => 
            {
                if (r.StatusType == DailyRewardStatusType.ReadyToTake)
                    isRewardReadyToTake = true;
            });

            if (isRewardReadyToTake)
            {
                label.style.opacity = 0;
                label.style.display = DisplayStyle.Flex;
                label.text = LocalizationManager.GetTranslation("Menu/NewRewards");
                this.RegisterCallback<GeometryChangedEvent>(CacheWidth);
                Messenger.AddListener(UIEvents.DailyRewardsReceived, ShowMark);
            }
            else
            {
                mark.style.opacity = 1;
            }
        }

        private void CacheWidth(GeometryChangedEvent geom)
        {
            if(float.IsNaN(this.resolvedStyle.width) || this.resolvedStyle.width == 0 || this.resolvedStyle.width == this.resolvedStyle.minWidth) 
                return;
            
            this.UnregisterCallback<GeometryChangedEvent>(CacheWidth);
            cachedWidth = this.resolvedStyle.width;
            label.text = String.Empty;
            label.style.display = DisplayStyle.None;
            DOVirtual.DelayedCall(3, AnimateShow);
        }

        private Sequence GetShowSeq()
        {
            Sequence result = DOTween.Sequence();
            
            result.Append(uiHelper.ChangeColorTween(this, Color.cyan, animData.ButtonChangeColorDuration));
            result.Append(uiHelper.ChangeWidth(this, this.resolvedStyle.width, cachedWidth, animData.ButtonChangeWidthDuration));
            result.Append(DOVirtual.DelayedCall(0, () => label.style.display = DisplayStyle.Flex));
            result.Append(uiHelper.GetTypewriterTween(label, LocalizationManager.GetTranslation("Menu/NewRewards"), null,0.7f).OnStart(() => label.style.opacity = 1));
            result.Append(DOVirtual.DelayedCall(4, AnimateHide));
            return result;
        }

        private void AnimateShow()
        {
            seq.Kill();
            inOutScaleTween.Kill();
            inOutScaleTween = uiHelper.InOutScaleTween(this, animData.ButtonPulseScaleInterval.x, animData.ButtonPulseScaleInterval.y, animData.ButtonPulseScaleDuration).SetLoops(-1, LoopType.Restart).Play()
                .OnComplete(() =>
                {
                    this.transform.scale = Vector3.one;
                });
            seq = GetShowSeq();
        }

        private void AnimateHide()
        {
            seq.Kill();
            inOutScaleTween.Kill(true);
            seq = HideTween();
        }

        private Sequence HideTween()
        {
            Sequence result = DOTween.Sequence();

            result.Append(uiHelper.FadeTween(label, 1, 0, animData.ButtonFadeLabelDuration));
            result.Append(DOVirtual.DelayedCall(0, () => label.style.display = DisplayStyle.None));
            result.Append(uiHelper.ChangeWidth(this, this.resolvedStyle.width, 0, animData.ButtonChangeWidthDuration));
            result.Append(uiHelper.ChangeColorTween(this, Color.white, animData.ButtonChangeColorDuration));
            result.OnComplete(() =>
            {
                
                DOTween.Kill(this);
                DOTween.Kill(label);
            });
            return result;
        }
        
        private void ShowMark() => uiHelper.FadeTween(mark, 0, 1, animData.ButtonFadeMarkDuration);
        

        public void OnClick()
        {
            PlaySound2D(SoundName);
            uiHelper.InOutScaleTween(this, transform.scale.x, 1.05f, 0.5f).Play();
        }

        public void UpdateDirectiveRewardSprite()
        {
            if (MainMenuDaily.DirectiveRewardReceivedToday(out string directiveId))
            {
                image.style.backgroundImage = new StyleBackground(DataManager.Instance.Get<PartsHolder>().Directives.Find(x => x.SerializedID == directiveId).Sprite);
            }
        }
    }
}