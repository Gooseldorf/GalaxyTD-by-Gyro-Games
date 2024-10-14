using DG.Tweening;
using I2.Loc;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace UI
{
    public class MissionWidget : SelectableElement
    {
        #region UxmlStaff

        public new class UxmlFactory : UxmlFactory<MissionWidget, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlAssetAttributeDescription<Texture2D> availableFrame = new() { name = "AvailableFrame", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> activeFrame = new() { name = "ActiveFrame", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> activeStar = new() { name = "ActiveStar", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> activeHardStar = new() { name = "ActiveHardStar", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> redFrame = new() { name = "AvailableRedFrame", defaultValue = null };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (availableFrame.TryGetValueFromBag(bag, cc, out Texture2D value))
                    ((MissionWidget)ve).AvailableFrame = value;
                if (activeFrame.TryGetValueFromBag(bag, cc, out Texture2D value2))
                    ((MissionWidget)ve).ActiveFrame = value2;
                if (activeStar.TryGetValueFromBag(bag, cc, out Texture2D value3))
                    ((MissionWidget)ve).ActiveStar = value3;
                if (activeHardStar.TryGetValueFromBag(bag, cc, out Texture2D value4))
                    ((MissionWidget)ve).ActiveHardStar = value4;
                if (redFrame.TryGetValueFromBag(bag, cc, out Texture2D value5))
                    ((MissionWidget)ve).RedFrame = value5;
            }
        }

        public Texture2D AvailableFrame { get; set; }
        public Texture2D ActiveFrame { get; set; }
        public Texture2D ActiveStar { get; set; }
        public Texture2D ActiveHardStar { get; set; }
        public Texture2D RedFrame { get; set; }

        #endregion
        
        //Circle container (left):
        private VisualElement outerCircle;
        private VisualElement lockIcon;
        private VisualElement starsContainer;
        private UQueryBuilder<VisualElement> starsQueryBuilder;
        
        //Label container (right):
        private VisualElement labelContainer;
        private VisualElement labelContainerBackground;
        private Label missionLabel;

        //Cached data:
        private IReadOnlyDictionary<int, int> normMissions;
        private IReadOnlyDictionary<int, int> hardMissions;
        
        //Fields:
        private Mission mission;
        private Mission missionHard;
        private Sequence showSeq;
        private bool isShown;
        private string localazedTitle;
        
        //Dependencies
        private UIHelper uiHelper;

        public AllEnums.UIState HardState;
        public int MissionIndex => mission.MissionIndex;
        public event Action<Mission, bool> OnWidgetClick;
        
        public override void Init()
        {
            base.Init();
            CacheData();
            CacheUIElements();

            RegisterCallback<ClickEvent>(OnClick);
        }
        
        public override void Dispose()
        {
            base.Dispose();
            UnregisterCallback<ClickEvent>(OnClick);
        }

        public void SetMission(Mission mission, Mission missionHard = null)
        {
            this.mission = mission;
            if (missionHard != null)
                this.missionHard = missionHard;

            SetStars();
        }

        public override void SetState(AllEnums.UIState state)
        {
            base.SetState(state);
            
            if (state != AllEnums.UIState.Locked)
            {
                starsContainer.style.display = DisplayStyle.Flex;
                lockIcon.style.display = DisplayStyle.None;
                outerCircle.style.backgroundImage = new StyleBackground(state == AllEnums.UIState.Active ? ActiveFrame : AvailableFrame);
                outerCircle.transform.rotation = Quaternion.Euler(0,0,Random.Range(0,360));
                PlayIdleAnimation();
            }
            else
            {
                style.opacity = 0.7f;
                outerCircle.style.unityBackgroundImageTintColor = Color.gray;
                pickingMode = PickingMode.Ignore;
            }
        }

        public override void SetSelected(bool selected)
        {
            base.SetSelected(selected);
            Animate(selected);
        }

        public void UpdateLocalization()
        {
            localazedTitle = $"{LocalizationManager.GetTranslation("Mission")} {(mission.MissionIndex + 1).ToString()}";
            
            missionLabel.text = State == AllEnums.UIState.Locked ? LocalizationManager.GetTranslation("Locked") : localazedTitle;
            UIHelper.Instance.SetLocalizationFont(this);
        }

        private void CacheUIElements()
        {
            //Circle container:
            outerCircle = this.Q<VisualElement>("OuterCircle");
            lockIcon = this.Q<VisualElement>("Lock");
            starsContainer = this.Q<VisualElement>("Stars");
            starsQueryBuilder = starsContainer.Query<VisualElement>("Star");
            //Label container:
            labelContainer = this.Q<VisualElement>("LabelContainer");
            missionLabel = labelContainer.Q<Label>();
            labelContainerBackground = labelContainer.Q<VisualElement>("Background");
        }

        private void CacheData()
        {
            uiHelper = UIHelper.Instance;
            normMissions = DataManager.Instance.GameData.Stars;
            hardMissions = DataManager.Instance.GameData.HardStars;
        }

        private void SetStars()
        {
            if (!normMissions.ContainsKey(mission.MissionIndex)) return;

            int i = 1;
            starsQueryBuilder.Build().ForEach((star) =>
            {
                if (i <= normMissions[mission.MissionIndex])
                {
                    star.style.backgroundImage = new StyleBackground(ActiveStar);
                    star.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                }
                i++;
            });
        }

        private void OnClick(ClickEvent clk)
        {
            OnWidgetClick?.Invoke(mission, false);
        }

        private void PlayIdleAnimation()
        {
            uiHelper.RotateElement(outerCircle, 360, true, 6).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetTarget(this).Play();
            uiHelper.ScaleTween(this, this.transform.scale.x, this.transform.scale.x + 0.05f, 1).SetLoops(-1,LoopType.Yoyo).SetEase(Ease.Linear).SetTarget(this).Play();
        }

        private void Animate(bool show)
        {
            
            if (show)
            {
                if (!isShown)
                {
                    showSeq?.Kill();
                    showSeq = null;
                    showSeq = DOTween.Sequence();
                    AnimateShow();
                }
            }
            else
            {
                showSeq?.Kill();
                showSeq = null;
                showSeq = DOTween.Sequence();
                AnimateHide();
            }
        }

        private void AnimateShow()
        {
            isShown = true;
            MusicManager.PlaySound2D("missionWidget_selected");
            showSeq.OnStart(()=>
            {
                DOTween.Kill(this);
                labelContainer.style.display = DisplayStyle.Flex;
            });
            showSeq.Append(uiHelper.RotateElement(outerCircle, 720, false, 0.8f));
            showSeq.Insert(0, uiHelper.ScaleTween(this, 1, 1.1f, 0.4f));
            showSeq.Insert(0.2f, uiHelper.FadeTween(labelContainer, 0, 1, 0.4f));
            showSeq.Insert(0.2f, uiHelper.ChangeWidth(labelContainer, 0, 540, 0.4f));
            showSeq.OnComplete(PlayIdleAnimation);
            
            DOVirtual.DelayedCall(0.3f, ()=> uiHelper.PlayTypewriter(missionLabel, localazedTitle, false));
        }

        private void AnimateHide()
        {
            isShown = false;
            
            showSeq.Append(uiHelper.ScaleTween(this, this.transform.scale.x, 1, 0.3f));
            showSeq.Append(uiHelper.ChangeWidth(labelContainer, labelContainer.resolvedStyle.width, 0, 0.6f));
            showSeq.Insert(0f, uiHelper.FadeTween(missionLabel, 1, 0, 0.4f));
            showSeq.Insert(0.2f, uiHelper.FadeTween(labelContainer, 1, 0, 0.6f));
            showSeq.OnComplete(() =>
            {
                labelContainer.style.display = DisplayStyle.None;
                PlayIdleAnimation();
            });
        }

        /*
        private void ActivateOutline(AllEnums.UIState state)
        {
            if (state == AllEnums.UIState.Active)
            {
                activeOutline.style.display = DisplayStyle.Flex;
                UIHelper.Instance.FadeTween(activeOutline, 0.2f, 0.6f, 5).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).Play();
                UIHelper.Instance.ScaleTween(activeOutline, 2.1f, 2.2f, 5).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).Play();
                UIHelper.Instance.ScaleTween(normContainer, 1,1.1f, 1).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).Play();
            }
        }
        */

        /*public void SetHardState(AllEnums.UIState state)
        {
            HardState = state;
            switch (state)
            {
                case AllEnums.UIState.Unavailable:
                    break;
                case AllEnums.UIState.Locked:
                    break;
                case AllEnums.UIState.Available:
                case AllEnums.UIState.Active:
                    //hardStars[0].parent.style.opacity = 1;
                    rewards[1].style.opacity = 1;
                    hardLockIcon.style.opacity = 0;
                    hardRewardOuterCircle.style.backgroundImage = new StyleBackground(state == AllEnums.UIState.Active ? ActiveFrame : RedFrame);
                    break;
            }
        }*/
    }
}