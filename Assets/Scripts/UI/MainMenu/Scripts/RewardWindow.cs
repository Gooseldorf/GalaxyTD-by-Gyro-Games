using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class RewardWindow : Selector
    {
        public new class UxmlFactory : UxmlFactory<RewardWindow> { }

        private Label titleLabel;
        private VisualElement background;
        private CommonButton confirmButton;
        private Label partTitleLabel;
        private Label descLabel;

        private CostDescriptionWidget costDescriptionWidget;

        private VisualElement directiveRewardsContainer;
        private List<RewardDirectiveWidget> directiveRewards = new();

        private VisualElement ammoRewardsContainer;
        private List<RewardAmmoWidget> ammoRewards = new();

        private DamageModifierWidget damageModifierWidget;

        private Mission mission;
        private List<WeaponPart> rewardItems;
        private int targetIndex;

        public event Action<Mission> OnRewardSelected;

        public bool IsShowing { get; private set; }

        public void Init()
        {
            background = this.parent;
            titleLabel = this.Q<Label>("TitleLabel");
            partTitleLabel = this.Q<Label>("PartTitle");
            descLabel = this.Q<Label>("Desc");
            costDescriptionWidget = this.Q<TemplateContainer>("CostDescription").Q<CostDescriptionWidget>();
            costDescriptionWidget.Init();

            directiveRewardsContainer = this.Q<VisualElement>("Directives");
            directiveRewards = this.Query<RewardDirectiveWidget>("RewardDirective").ToList();

            ammoRewardsContainer = this.Q<VisualElement>("Ammo");
            ammoRewards = this.Query<RewardAmmoWidget>("RewardAmmo").ToList();

            damageModifierWidget = this.Q<TemplateContainer>("DamageModifier").Q<DamageModifierWidget>();
            damageModifierWidget.Init();

            confirmButton = this.Q<TemplateContainer>("ConfirmButton").Q<CommonButton>();
            confirmButton.Init();
            confirmButton.RegisterCallback<ClickEvent>(OnConfirmButtonClick);
            confirmButton.SetState(AllEnums.UIState.Available);

            background.style.display = DisplayStyle.None;
            this.style.display = DisplayStyle.None;
            IsShowing = false;
        }

        public void Dispose()
        {
            foreach (var reward in directiveRewards)
            {
                reward.Dispose();
                reward.UnregisterCallback<ClickEvent>(OnRewardClick);
            }

            foreach (var ammo in ammoRewards)
            {
                ammo.Dispose();
                ammo.UnregisterCallback<ClickEvent>(OnRewardClick);
            }

            confirmButton.Dispose();
            confirmButton.UnregisterCallback<ClickEvent>(OnConfirmButtonClick);
        }

        public void Show(Mission mission)
        {
            IsShowing = true;
            this.mission = mission;
            rewardItems = DataManager.Instance.Get<UnlockManager>().GetRewardsForChoose(mission.MissionIndex);
            if (rewardItems[0].PartType == AllEnums.PartType.Directive)
            {
                directiveRewardsContainer.style.display = DisplayStyle.Flex;
                ammoRewardsContainer.style.display = DisplayStyle.None;
                for (int i = 0; i < directiveRewards.Count; i++)
                {
                    directiveRewards[i].RegisterCallback<ClickEvent>(OnRewardClick);
                    directiveRewards[i].Init(rewardItems[i]);
                }
                Select(directiveRewards[0]);
            }
            else
            {
                ammoRewardsContainer.style.display = DisplayStyle.Flex;
                directiveRewardsContainer.style.display = DisplayStyle.None;
                for (int i = 0; i < ammoRewards.Count; i++)
                {
                    ammoRewards[i].RegisterCallback<ClickEvent>(OnRewardClick);
                    ammoRewards[i].Init(rewardItems[i]);
                }
                Select(ammoRewards[0]);
            }

            background.style.display = DisplayStyle.Flex;
            this.style.display = DisplayStyle.Flex;
            UpdateLocalization();
        }

        private void SetDirectiveDescription(WeaponPart directive)
        {
            DOTween.Kill(partTitleLabel);
            UIHelper.Instance.PlayTypewriter(partTitleLabel, directive.GetTitle());
            DOTween.Kill(descLabel);
            UIHelper.Instance.PlayTypewriter(descLabel, directive.GetDescription(), true, null, 1.5f, true);
            costDescriptionWidget.SetPart(directive);

            if (damageModifierWidget.resolvedStyle.display == DisplayStyle.Flex)
                damageModifierWidget.style.display = DisplayStyle.None;
        }

        private void SetAmmoDescription(WeaponPart ammo)
        {
            DOTween.Kill(partTitleLabel);
            UIHelper.Instance.PlayTypewriter(partTitleLabel, ammo.GetTitle());
            DOTween.Kill(descLabel);
            UIHelper.Instance.PlayTypewriter(descLabel, ammo.GetDescription(), true, null, 1.5f, true);
            costDescriptionWidget.SetPart(ammo);
            damageModifierWidget.SetPart(ammo);
        }

        private void Hide()
        {
            Tween animation = UIHelper.Instance.GetHideWindowTween(this, background);
            animation.OnComplete(() => IsShowing = false);
            animation.Play();
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void OnRewardClick(ClickEvent clk)
        {
            SelectableElement widget = (SelectableElement)clk.currentTarget;
            Select(widget);
        }

        protected override void Select(SelectableElement widget)
        {
            base.Select(widget);

            targetIndex = directiveRewards.Contains(widget as RewardDirectiveWidget) ? directiveRewards.IndexOf(widget as RewardDirectiveWidget) : ammoRewards.IndexOf(widget as RewardAmmoWidget);
            widget.Q<VisualElement>("Icon").style.unityBackgroundImageTintColor = new StyleColor(Color.white);

            WeaponPart selectedPart = rewardItems[targetIndex];

            DOTween.Kill(partTitleLabel);
            DOTween.Kill(descLabel);
            if (selectedPart.PartType == AllEnums.PartType.Directive)
            {
                SetDirectiveDescription(selectedPart);
            }
            else
            {
                SetAmmoDescription(selectedPart);
            }
        }

        private void OnConfirmButtonClick(ClickEvent clk)
        {
            if (LastSelected == null) return;

            DataManager.Instance.GameData.ChooseRewardForMission(mission, targetIndex);
            OnRewardSelected?.Invoke(mission);
            Hide();
        }

        public void UpdateLocalization()
        {
            titleLabel.text = LocalizationManager.GetTranslation("Menu/ChooseReward");
            confirmButton.SetText(LocalizationManager.GetTranslation("Menu/ChooseButton"));
        }
    }
}