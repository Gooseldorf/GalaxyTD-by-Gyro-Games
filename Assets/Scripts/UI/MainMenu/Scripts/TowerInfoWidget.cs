using DG.Tweening;
using I2.Loc;
using UnityEngine.UIElements;

namespace UI
{
    public class TowerInfoWidget : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TowerInfoWidget> { }

        private Label titleLabel;
        private Label levelLabel;
        private Label desc;
        private ScrollView descriptionScrollView;
        private Label nextLevelLabel;
        private Label upgradeDescription;

        private TowerFactory factory;
        private UIHelper uiHelper;
        private UpgradeProvider upgradeProvider;

        public void Init()
        {
            titleLabel = this.Q<Label>("TitleLabel");
            levelLabel = this.Q<Label>("LevelLabel");
            desc = this.Q<Label>("Desc");
            descriptionScrollView = this.Q<ScrollView>("DescriptionScroll");
            nextLevelLabel = this.Q<Label>("NextLevelLabel");
            upgradeDescription = this.Q<Label>("UpgradeText");

            uiHelper = UIHelper.Instance;
            upgradeProvider = DataManager.Instance.Get<UpgradeProvider>();
        }

        public void Show(TowerFactory factory)
        {
            this.factory = factory;
            style.display = DisplayStyle.Flex;
            AnimateText();
        }
        
        public void Hide() => style.display = DisplayStyle.None;

        private void AnimateText(float delay = 0)
        {
            titleLabel.text = string.Empty;
            desc.text = string.Empty;
            upgradeDescription.text = string.Empty;
            nextLevelLabel.text = string.Empty;
            levelLabel.text = string.Empty;
            DOVirtual.DelayedCall(delay, () =>
            {
                AnimateTitle();
                AnimateDesc();
                UpdateUpgradeDescription();
            });
        }

        private void AnimateTitle()
        {
            DOTween.Kill(titleLabel);
            string title = factory.TowerPrototype.GetTitle();
            uiHelper.PlayTypewriter(titleLabel, title);
        }

        private void AnimateDesc()
        {
            DOTween.Kill(desc);
            string description = factory.TowerPrototype.GetDescription();
            uiHelper.PlayTypewriter(desc, description, true, descriptionScrollView,1.5f, true);
        }

        private void AnimateUpgradeText()
        {
            DOTween.Kill(upgradeDescription);
            string upgradeDesc = upgradeProvider.GetNextUpgradeDesc(factory.TowerId, factory.Level);
            uiHelper.PlayTypewriter(upgradeDescription, upgradeDesc, useHeight: true);
            

            DOTween.Kill(nextLevelLabel);
            string nextLevelText = $"{LocalizationManager.GetTranslation("Menu/Level")} {factory.Level + 1 + uiHelper.TowerLevelAdjustDict[factory.TowerId]} -> {LocalizationManager.GetTranslation("Menu/Level")} {factory.Level + 2 + uiHelper.TowerLevelAdjustDict[factory.TowerId]}";
            uiHelper.PlayTypewriter(nextLevelLabel, nextLevelText);
            
            DOTween.Kill(levelLabel);
            string levelText = $"{LocalizationManager.GetTranslation("Menu/Level")} {factory.Level + 1 + uiHelper.TowerLevelAdjustDict[factory.TowerId]}";
            uiHelper.PlayTypewriter(levelLabel, levelText);
        }

        public void UpdateUpgradeDescription()
        {
            AnimateUpgradeText();
        }
    }
}