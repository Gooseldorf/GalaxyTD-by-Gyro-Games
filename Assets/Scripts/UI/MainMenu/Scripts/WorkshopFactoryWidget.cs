using CardTD.Utilities;
using I2.Loc;
using System.Collections.Generic;
using Tags;
using UnityEngine.UIElements;

namespace UI
{
    public class WorkshopFactoryWidget : ClickableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<WorkshopFactoryWidget> { }

        private Label towerTitle;
        private VisualElement towerIcon;
        private List<DirectiveWidget> directiveWidgets;

        private AllEnums.UIState state;
        private VisualElement towerContainer;
        private ClickableVisualElement lockedVisuals;
        private VisualElement questionMarkIcon;
        private Label buildCostLabel;
        private Label buildCostValueLabel;
        private VisualElement isNewNotification;

        private VisualElement newPartsContainer;
        private VisualElement newPartIcon;
        private Label newPartLabel;

        private DamageModifierWidget damageModifierWidget;

        private Label towerLevelLabel;
        private Label damageTitle;
        private Label damageLabel;
        private Label projectilesCountLabel;

        private VisualElement canLevelUp1;
        private VisualElement canLevelUp2;

        private VisualElement ammoIcon;
        private VisualElement ammoBg;
        private VisualElement isNewAmmoNotification;

        private TowerFactory factory;
        public TowerFactory Factory => factory;
        private UIHelper uiHelper;

        public AllEnums.UIState State => state;

        public void Init()
        {
            base.Init();
            uiHelper = UIHelper.Instance;
            towerTitle = this.parent.Q<Label>("TowerTitle");
            towerIcon = this.Q<VisualElement>("Icon");

            directiveWidgets = this.Query<TemplateContainer>("DirectiveWidget")
                .Children<DirectiveWidget>("DirectiveWidget").ToList();

            foreach (DirectiveWidget directiveWidget in directiveWidgets)
                directiveWidget.Init();

            isNewNotification = this.Q<VisualElement>("FactoryIsNewNotification");
            lockedVisuals = this.Q<ClickableVisualElement>("LockedVisuals");
            towerContainer = this.Q<VisualElement>("TowerContainer");
            questionMarkIcon = this.Q<VisualElement>("QuestionMark");

            buildCostLabel = this.Q<Label>("BuildCostLabel");
            buildCostValueLabel = this.Q<Label>("BuildCostValueLabel");

            newPartsContainer = this.Q<VisualElement>("NewPartsContainer");
            newPartIcon = this.Q<VisualElement>("NewPartsIcon");
            newPartLabel = this.Q<Label>("NewPartsLabel");

            damageModifierWidget = this.Q<DamageModifierWidget>("DamageModifierWidget");
            damageModifierWidget.Init();

            towerLevelLabel = this.Q<Label>("TowerLevel");
            damageTitle = this.Q<Label>("DamageLabel");
            damageLabel = this.Q<Label>("Damage");
            projectilesCountLabel = this.Q<Label>("ProjectilesCountLabel");

            canLevelUp1 = this.Q<VisualElement>("CanLevelUp1");
            canLevelUp2 = this.Q<VisualElement>("CanLevelUp2");

            ammoIcon = this.Q<VisualElement>("AmmoType");
            ammoBg = this.Q<VisualElement>("AmmoBg");
            isNewAmmoNotification = this.Q<VisualElement>("AmmoIsNewNotification");
        }

        public void Dispose()
        {
            foreach (DirectiveWidget directiveWidget in directiveWidgets)
                directiveWidget.Dispose();
        }

        public void SetTower(TowerFactory factory)
        {
            this.factory = factory;
            towerIcon.style.backgroundImage = new StyleBackground(factory.TowerPrototype.Sprite);

            Tower tower = factory.GetAssembledTower();
            buildCostValueLabel.text = tower.BuildCost.ToString();
            damageLabel.text = ((int)tower.AttackStats.DamagePerBullet).ToStringBigValue();
            if (tower.TowerId is AllEnums.TowerId.Shotgun or AllEnums.TowerId.TwinGun)
            {
                projectilesCountLabel.style.display = DisplayStyle.Flex;
                projectilesCountLabel.text = $"{tower.AttackStats.ShootingStats.ProjectilesPerShot}x";
            }
            else
            {
                projectilesCountLabel.style.display = DisplayStyle.None;
            }

            for (int i = 0; i < factory.Directives.Count; i++)
            {
                directiveWidgets[i].SetEmptySlot();
                directiveWidgets[i].Init();
                directiveWidgets[i].SetSlot(factory.Directives[i]);
            }

            damageModifierWidget.SetPart(factory.Ammo.WeaponPart);

            MenuUpgrade nextUpgrade = DataManager.Instance.Get<UpgradeProvider>().GetNextUpgrade(factory.TowerId, factory.Level);
            canLevelUp1.style.display =
                canLevelUp2.style.display = DataManager.Instance.GameData.SoftCurrency > nextUpgrade.Cost ? DisplayStyle.Flex : DisplayStyle.None;


            ammoIcon.style.backgroundImage = new StyleBackground(tower.Ammo.WeaponPart.Sprite);
            ammoBg.style.backgroundImage = new StyleBackground(uiHelper.GetAmmoBg(tower.Ammo.WeaponPart));
            UpdateLocalization();
        }

        public void SetLocked()
        {
            towerContainer.style.display = DisplayStyle.None;
            lockedVisuals.style.display = DisplayStyle.Flex;
            uiHelper.AnimateQuestionMark(questionMarkIcon);
        }


        public void UpdateIsNewNotifications()
        {
            GameData gameData = DataManager.Instance.GameData;

            isNewNotification.style.display = gameData.NewFactories.Contains(factory.TowerId) ? DisplayStyle.Flex : DisplayStyle.None;

            bool isNewItemExist = gameData.NewItems.Exists(x => (x.TowerId == factory.TowerId || x.TowerId.HasFlag(factory.TowerId)) && x.PartType != AllEnums.PartType.Ammo && x.PartType != AllEnums.PartType.Directive);
            newPartsContainer.style.display = isNewItemExist ? DisplayStyle.Flex : DisplayStyle.None;

            bool isNewAmmoExist = gameData.NewItems.Exists(x => (x.TowerId == factory.TowerId || x.TowerId.HasFlag(factory.TowerId)) && x.PartType == AllEnums.PartType.Ammo);
            isNewAmmoNotification.style.display = isNewAmmoExist ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void UpdateLocalization()
        {
            if (factory != null)
            {
                towerTitle.text = factory.TowerPrototype.GetTitle();
                towerLevelLabel.text = LocalizationManager.GetTranslation("Menu/Level").ToUpper() + " " + (factory.Level + 1 + uiHelper.TowerLevelAdjustDict[factory.TowerId]);
            }

            buildCostLabel.text = LocalizationManager.GetTranslation("TowerStats/BuildCost");
            newPartLabel.text = LocalizationManager.GetTranslation("Menu/NewParts");
            damageTitle.text = LocalizationManager.GetTranslation("TowerStats/Damage").ToUpper() + ": ";
            uiHelper.SetLocalizationFont(damageTitle);
            uiHelper.SetLocalizationFont(damageLabel);
            uiHelper.SetLocalizationFont(projectilesCountLabel);
            
        }
    }

}
