using DG.Tweening;
using I2.Loc;
using System;
using System.Collections.Generic;
using Tags;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class FactoryWidget : Selector
    {
        public new class UxmlFactory : UxmlFactory<FactoryWidget> { }

        private Label towerTitle;
        private SelectableElement towerIcon;
        private TowerStatsWidget towerStatsWidget;
        private List<SlotWidget> slotWidgets;
        private List<DirectiveWidget> directiveWidgets;
        private AmmoWidget ammoWidget;

        private VisualElement buildCostContainer;
        private Label buildCostLabel;
        private Label buildCostValueLabel;
        private int currentBuildCost = 0;
        private VisualElement canUpgradeIcon;

        private TowerFactory factory;
        private UIHelper uiHelper;

        public event Action<SelectableElement> OnPartSelected;

        private VisualElement upgradeEffect;
        private Sequence upgradeSeq;

        public void Init()
        {
            uiHelper = UIHelper.Instance;
            towerTitle = this.parent.Q<Label>("TowerTitle");
            towerIcon = this.Q<SelectableElement>("TowerIcon");
            towerIcon.Init();

            towerStatsWidget = this.Q<TowerStatsWidget>("TowerStatsWidget");
            towerStatsWidget.Init();

            slotWidgets = this.Query<SlotWidget>("SlotWidget").ToList();
            foreach (SlotWidget slotWidget in slotWidgets)
                slotWidget.Init();

            directiveWidgets = this.Query<TemplateContainer>("DirectiveWidget")
                .Children<DirectiveWidget>("DirectiveWidget").ToList();

            foreach (DirectiveWidget directiveWidget in directiveWidgets)
                directiveWidget.Init();

            ammoWidget = this.Q<AmmoWidget>("AmmoWidget");
            ammoWidget.Init();

            canUpgradeIcon = this.Q<VisualElement>("CanUpgradeIcon");

            upgradeEffect = this.Q<VisualElement>("UpgradeEffect");

            List<VisualElement> slotsLockers = this.Query<VisualElement>("LockedSlotWidget").ToList();
            VisualElement ammoLocker = this.Q<VisualElement>("LockedAmmoWidget");
            VisualElement directivesLocker = this.Q<VisualElement>("LockedDirectives");
            VisualElement directivesContainer = this.Q<VisualElement>("Directives");

            int passedMissionsCount = DataManager.Instance.GameData.Stars.Count;
            UnlockManager unlocker = DataManager.Instance.Get<UnlockManager>();

            for (int i = 0; i < slotsLockers.Count; i++)
            {
                if (passedMissionsCount >= unlocker.PartsUnlockMission)
                    slotsLockers[i].style.display = DisplayStyle.None;
                else
                    slotWidgets[i].style.display = DisplayStyle.None;
            }

            if (passedMissionsCount >= unlocker.AmmoUnlockMission)
                ammoLocker.style.display = DisplayStyle.None;
            else
                ammoWidget.style.display = DisplayStyle.None;

            if (passedMissionsCount >= unlocker.DirectivesUnlockMission)
                directivesLocker.style.display = DisplayStyle.None;
            else
                directivesContainer.style.display = DisplayStyle.None;

        }

        public void InitWithCallbacks()
        {
            Init();
            towerIcon?.RegisterCallback<ClickEvent>(OnPartClick);

            foreach (SlotWidget slotWidget in slotWidgets)
            {
                slotWidget.RegisterCallback<ClickEvent>(OnPartClick);
            }

            foreach (DirectiveWidget directiveWidget in directiveWidgets)
                directiveWidget.RegisterCallback<ClickEvent>(OnPartClick);

            ammoWidget.RegisterCallback<ClickEvent>(OnPartClick);
            buildCostContainer = this.Q<VisualElement>("BuildCost");
            buildCostLabel = this.Q<Label>("BuildCostLabel");
            buildCostValueLabel = this.Q<Label>("BuildCostValueLabel");
            Select(towerIcon);
        }

        public void Dispose()
        {
            towerIcon.Dispose();
            towerIcon.UnregisterCallback<ClickEvent>(OnPartClick);

            foreach (SlotWidget slotWidget in slotWidgets)
            {
                slotWidget.Dispose();
                slotWidget.UnregisterCallback<ClickEvent>(OnPartClick);
            }


            foreach (DirectiveWidget directiveWidget in directiveWidgets)
            {
                directiveWidget.Dispose();
                directiveWidget.UnregisterCallback<ClickEvent>(OnPartClick);
            }

            ammoWidget.Dispose();
            ammoWidget.UnregisterCallback<ClickEvent>(OnPartClick);
        }

        public void ShowUpgradeEffect()
        {
            upgradeSeq = uiHelper.AnimateTowerUpgrade(upgradeEffect);
            //upgradeSeq.Insert(0,)
            upgradeSeq.OnComplete(() =>
            {
                upgradeSeq = null;
                upgradeEffect.style.backgroundImage = new StyleBackground();
            });

            towerIcon.PlayOnClickAnimation(new ClickEvent());
        }

        public void SetTower(TowerFactory factory)
        {
            if (upgradeSeq != null)
            {
                upgradeSeq.Kill();
                upgradeSeq = null;
                upgradeEffect.style.backgroundImage = new StyleBackground();
            }

            this.factory = factory;
            towerIcon.Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(factory.TowerPrototype.Sprite);

            for (int i = 0; i < factory.Parts.Count; i++)
            {
                slotWidgets[i].Init();
                slotWidgets[i].SetSlot((Slot)factory.Parts[i]);
            }

            for (int i = 0; i < factory.Directives.Count; i++)
            {
                directiveWidgets[i].SetEmptySlot();
                directiveWidgets[i].Init();
                directiveWidgets[i].SetSlot(factory.Directives[i]);
            }

            ammoWidget.SetSlot(factory.Ammo);
            ammoWidget.SetAmmo(factory.Ammo.WeaponPart, factory.GetAssembledTower().AttackStats.ReloadStats.MagazineSize);

            UpdateStats(factory);
            UpdateLocalization();
        }

        public void UpdateStats(TowerFactory factory)
        {
            if (buildCostValueLabel != null)
            {
                int cost = factory.GetAssembledTower().BuildCost;
                if (int.Parse(buildCostValueLabel.text) != cost)
                    uiHelper.InOutScaleTween(buildCostContainer, 1, 1.1f, 0.5f);
                uiHelper.ChangeNumberInLabelTween(buildCostValueLabel, currentBuildCost, cost, 0.5f);
                currentBuildCost = cost;
            }
            towerStatsWidget.UpdateStats(factory.GetAssembledTower(), factory.GetBaseTower());
            //ammoWidget.SetAmmo(factory.Ammo.WeaponPart, factory.GetAssembledTower().AttackStats.ReloadStats.MagazineSize);

            MenuUpgrade nextUpgrade = DataManager.Instance.Get<UpgradeProvider>().GetNextUpgrade(factory.TowerId, factory.Level);
            canUpgradeIcon.style.display = DataManager.Instance.GameData.SoftCurrency > nextUpgrade.Cost ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnPartClick(ClickEvent clk)
        {
            SelectableElement target = (SelectableElement)clk.currentTarget;
            Select(target);
            OnPartSelected?.Invoke(target);
        }

        public void UpdateIsNewNotifications()
        {
            if (factory == null) return;
            GameData gameData = DataManager.Instance.GameData;

            bool isNewItemExist;

            foreach (var slotWidget in slotWidgets)
            {
                isNewItemExist = gameData.NewItems.Exists(x =>
                    x.PartType == slotWidget.Slot.PartType && x.TowerId == slotWidget.Slot.WeaponPart.TowerId);

                slotWidget.SetIsNewNotification(isNewItemExist);
            }

            isNewItemExist = gameData.NewItems.Exists(x =>
                x.PartType == AllEnums.PartType.Ammo && x.TowerId == ammoWidget.Slot.WeaponPart.TowerId);

            ammoWidget.SetIsNewNotification(isNewItemExist);
        }

        public void UpdateLocalization()
        {
            foreach (SlotWidget slotWidget in slotWidgets) slotWidget.UpdateLocalization();
            ammoWidget.UpdateLocalization();

            towerStatsWidget.UpdateLocalization();
            uiHelper.SetLocalizationFont(towerStatsWidget);
            if (buildCostLabel != null)
            {
                buildCostLabel.text = LocalizationManager.GetTranslation("TowerStats/BuildCost");
            }
            if (factory != null)
                towerTitle.text = factory.TowerPrototype.GetTitle();
        }

        private void AnimateTitle()
        {
            if (factory == null) return;
            DOTween.Kill(towerTitle);
            uiHelper.PlayTypewriter(towerTitle, factory.TowerPrototype.GetTitle());
        }

        public void SelectTowerIcon() => Select(towerIcon);
    }
}