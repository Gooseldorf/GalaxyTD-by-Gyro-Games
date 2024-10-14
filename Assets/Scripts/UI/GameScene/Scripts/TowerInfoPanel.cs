using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using ECSTest.Systems.Roguelike;
using static MusicManager;
using Sounds.Attributes;
using static AllEnums;

namespace UI
{
    public class TowerInfoPanel : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TowerInfoPanel>
        {
        }

        private Label levelLabel_left;
        private Label levelText_left;
        private Label title;
        private DamageModifierWidget damageModifierWidget;
        private VisualElement towerIcon;
        private VisualElement towerStateIcon;
        private Label towerStateLabel;
        private List<DirectiveWidget> directiveWidgets;
        private List<DirectiveWidget> roguelikeDirectiveWidgets;
        private DirectiveTooltip directiveTooltip;
        private TowerStatsWidget towerStatsWidget;
        private AmmoWidget ammoWidget;

        private CommonButton fireModButton;
        private PriceButton reloadButton;
        private PriceButton upgradeButton;
        private PriceButton sellButton;
        private Label levelLabel_right;
        private Label levelText_right;
        private VisualElement rankIcon1;
        private VisualElement rankIcon2;
        private VisualElement upgradeCostContainer;

        private VisualElement infoContainer;
        private VisualElement upgradeContainer;
        private UIHelper uiHelper;

        private Entity tower;

        private TowerState towerState;
        private bool isShown = false;

        public void Init()
        {
            uiHelper = UIHelper.Instance;
            infoContainer = this.Q<VisualElement>("InfoContainer");
            upgradeContainer = this.Q<VisualElement>("UpgradeContainer");

            levelLabel_left = infoContainer.Q<Label>("LevelLabel");
            levelText_left = infoContainer.Q<Label>("LevelText");

            title = this.Q<Label>("Title");
            damageModifierWidget = this.Q<DamageModifierWidget>("DamageModifierWidget");
            damageModifierWidget.Init();
            towerIcon = this.Q<VisualElement>("TowerIcon");
            towerIcon.RegisterCallback<ClickEvent>(ToggleTower);
            towerStateIcon = towerIcon.Q<VisualElement>("TowerStateIcon");
            towerStateLabel = this.Q<Label>("TowerStateLabel");

            directiveWidgets = this.Query<TemplateContainer>("DirectiveWidget").Children<DirectiveWidget>("DirectiveWidget").ToList();
            foreach (DirectiveWidget directiveWidget in directiveWidgets)
            {
                directiveWidget.Init();
                directiveWidget.RegisterCallback<ClickEvent>(OnDirectiveClick);
            }

            directiveTooltip = this.Q<DirectiveTooltip>("DirectiveTooltip");
            directiveTooltip.Init();

            towerStatsWidget = this.Q<TowerStatsWidget>("TowerStatsWidget");
            towerStatsWidget.Init();
            towerStatsWidget.UpdateLocalization();

            ammoWidget = this.Q<AmmoWidget>("AmmoWidget");
            ammoWidget.Init();
            ammoWidget.AddToClassList("AmmoWidget");
            ammoWidget.UpdateLocalization();

            fireModButton = this.Q<CommonButton>("FireModeButton");
            fireModButton.SoundName = SoundConstants.EmptyKey;
            fireModButton.Init();
            fireModButton.RegisterCallback<ClickEvent>(OnChangeFireModeClick);

            reloadButton = this.Q<PriceButton>("ReloadButton");
            reloadButton.SoundName = SoundConstants.EmptyKey;
            reloadButton.Init();
            reloadButton.RegisterCallback<ClickEvent>(OnReloadClick);

            upgradeButton = this.Q<PriceButton>("UpgradeButton");
            upgradeButton.Init();
            upgradeButton.RegisterCallback<ClickEvent>(OnUpgradeClick);

            upgradeCostContainer = this.Q<VisualElement>("CostContainer");

            sellButton = this.Q<PriceButton>("SellButton");
            sellButton.Init();
            sellButton.RegisterCallback<ClickEvent>(OnSellClick);

            levelLabel_right = upgradeContainer.Q<Label>("LevelLabel");
            levelText_right = upgradeContainer.Q<Label>("LevelText");
            rankIcon1 = upgradeContainer.Q<VisualElement>("RankIcon1");
            rankIcon2 = upgradeContainer.Q<VisualElement>("RankIcon2");

            roguelikeDirectiveWidgets = this.Query<TemplateContainer>("RoguelikeDirectiveWidget").Children<DirectiveWidget>().ToList();

            foreach (DirectiveWidget directiveWidget in roguelikeDirectiveWidgets)
            {
                directiveWidget.Init();
                directiveWidget.RegisterCallback<ClickEvent>(OnRoguelikeDirectiveClick);
                directiveWidget.style.display = DisplayStyle.None;
            }

            Messenger<Entity>.AddListener(GameEvents.TowerUpdated, OnTowerUpdated);
            Messenger<int, bool>.AddListener(GameEvents.CashUpdated, UpdateButtonsState);
            Messenger<int, bool>.AddListener(GameEvents.CashUpdated, UpdateUpgradeWidget);

            UpdateLocalization();
            Hide();
        }

        public void Dispose()
        {
            towerIcon.UnregisterCallback<ClickEvent>(ToggleTower);
            foreach (var directiveWidget in directiveWidgets)
            {
                directiveWidget.Dispose();
                directiveWidget.UnregisterCallback<ClickEvent>(OnDirectiveClick);
            }

            foreach (var directiveWidget in roguelikeDirectiveWidgets)
            {
                directiveWidget.Dispose();
                directiveWidget.UnregisterCallback<ClickEvent>(OnRoguelikeDirectiveClick);
            }

            fireModButton.Dispose();
            fireModButton.UnregisterCallback<ClickEvent>(OnChangeFireModeClick);

            reloadButton.Dispose();
            reloadButton.UnregisterCallback<ClickEvent>(OnReloadClick);

            upgradeButton.Dispose();
            upgradeButton.UnregisterCallback<ClickEvent>(OnUpgradeClick);

            sellButton.Dispose();
            sellButton.UnregisterCallback<ClickEvent>(OnSellClick);

            Messenger<Entity>.RemoveListener(GameEvents.TowerUpdated, OnTowerUpdated);
            Messenger<int, bool>.RemoveListener(GameEvents.CashUpdated, UpdateButtonsState);
            Messenger<int, bool>.RemoveListener(GameEvents.CashUpdated, UpdateUpgradeWidget);
        }

        public void Show(Entity towerEntity)
        {
            isShown = true;

            if (towerEntity == tower) ToggleTower();
            
            tower = towerEntity;

            AttackerComponent attackerComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AttackerComponent>(tower);

            title.text = LocalizationManager.GetTranslation($"Tower/{attackerComponent.TowerType}_title");
            towerIcon.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetTowerSprite(attackerComponent.TowerType.ToString()));

            Tower currentTower = GameServices.Instance.GetTower(attackerComponent.TowerType, towerEntity);

            if (currentTower == null) return;

            for (int i = 0; i < currentTower.Directives.Count; i++)
            {
                if (currentTower.Directives[i].WeaponPart != null)
                    directiveWidgets[i].SetSlot(currentTower.Directives[i]);
                else
                    directiveWidgets[i].SetEmptySlot();
            }

            damageModifierWidget.SetPart(currentTower.Ammo.WeaponPart);

            ammoWidget.SetAmmo(currentTower.Ammo.WeaponPart, attackerComponent.AttackStats.ReloadStats.MagazineSize);
            ammoWidget.SetFireModeButtonState(attackerComponent.AttackPattern);
            UpdateAmmoWidget();

            UpdateTowerStats();

            style.display = DisplayStyle.Flex;

            CostComponent costComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<CostComponent>(tower);
            sellButton.SetPrice(costComponent.SellCost);

            CashComponent cashComponent = GameServices.Instance.GetCashComponent();
            levelLabel_left.text =
                levelLabel_right.text = (attackerComponent.Level + 1).ToString();

            CheckRankSprites(attackerComponent.Level);
            UpdateUpgradeWidget(cashComponent.Cash, false);
            UpdateButtonsState(cashComponent.Cash, false);
            UpdateTowerState();

            ShowRoguelikeDirectives();

            infoContainer.AddToClassList("ShowInfo");
            infoContainer.RemoveFromClassList("HideInfo");
            upgradeContainer.AddToClassList("ShowUpgrade");
            upgradeContainer.RemoveFromClassList("HideUpgrade");
        }

        public void Hide()
        {
            isShown = false;
            tower = default;
            infoContainer.AddToClassList("HideInfo");
            infoContainer.RemoveFromClassList("ShowInfo");
            upgradeContainer.AddToClassList("HideUpgrade");
            upgradeContainer.RemoveFromClassList("ShowUpgrade");
        }

        private void ShowRoguelikeDirectives()
        {
            if (!GameServices.Instance.IsRoguelike)
                return;

            var directives = RoguelikeMainController.Link.Directives;

            for (int i = 0; i < roguelikeDirectiveWidgets.Count; i++)
            {
                if (directives.Count > i)
                {
                    roguelikeDirectiveWidgets[i].style.display = DisplayStyle.Flex;
                    roguelikeDirectiveWidgets[i].SetSlot(new Slot(AllEnums.PartType.Directive, directives[i]));
                }
                else
                {
                    //TODO: также если директива не доступна на данную вышку должно быть но тогда надо ш пропускать
                    roguelikeDirectiveWidgets[i].style.display = DisplayStyle.None;
                }
            }
        }

        private void OnTowerUpdated(Entity updatedTower)
        {
            if (!isShown || updatedTower != tower)
                return;

            ShowRoguelikeDirectives();
            UpdateTowerStats();
            UpdateAmmoWidget();
            UpdateTowerState();
        }

        private void UpdateButtonsState(int currentCash, bool cashForWave)
        {
            if (!isShown)
                return;

            var manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (!manager.Exists(tower) || !manager.HasComponent(tower, typeof(AttackerComponent)))
            {
                Debug.LogError("tower is not exist, should be already fixed, if you saw it tell to Ivan");
                return;
            }

            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
            if (attackerComponent.ReloadTimer <= 0)
            {
                int reloadCost = attackerComponent.AttackStats.ReloadStats.ManualReloadCost(attackerComponent.Bullets);
                ammoWidget.SetReloadButtonState(reloadCost <= currentCash ? AllEnums.UIState.Available : AllEnums.UIState.Locked);
            }
        }

        private void UpdateUpgradeWidget(int currentCash, bool cashForWave)
        {
            if (!isShown)
                return;

            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
            CostComponent costComponent = manager.GetComponentData<CostComponent>(tower);
            
            if (GameServices.Instance.CanTowerUpgrade(attackerComponent, out CompoundUpgrade nextGameUpgrade))
            {
                int upgradeCost = Mathf.RoundToInt(nextGameUpgrade.Cost * costComponent.CostMultiplier);
                SetUpgradeButtonState(currentCash >= upgradeCost ? AllEnums.UIState.Available : AllEnums.UIState.Unavailable);
                upgradeButton.SetPrice(upgradeCost);
                if (upgradeCostContainer.style.display != DisplayStyle.Flex)
                    upgradeCostContainer.style.display = DisplayStyle.Flex;
                UpdateUpgradeButton(attackerComponent.Level);
            }
            else
            {
                upgradeButton.SetText(LocalizationManager.GetTranslation("GameScene/MaxLevel"));
                SetUpgradeButtonState(AllEnums.UIState.Unavailable);
                if (upgradeCostContainer.style.display != DisplayStyle.None)
                    upgradeCostContainer.style.display = DisplayStyle.None;
            }
        }

        private void UpdateAmmoWidget()
        {
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
            ammoWidget.SetAmmoCount(attackerComponent.Bullets, attackerComponent.AttackStats.ReloadStats.MagazineSize);
            ammoWidget.ShowReload(attackerComponent.ReloadTimer, attackerComponent.AttackStats.ReloadStats.ReloadTime);
        }

        private void SetUpgradeButtonState(AllEnums.UIState state) => upgradeButton.SetState(state);

        private void UpdateTowerStats()
        {
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
            IComponentData additionalStats = null;

            if (manager.HasComponent<GunStatsComponent>(tower))
                additionalStats = manager.GetComponentData<GunStatsComponent>(tower);
            else if (manager.HasComponent<RocketStatsComponent>(tower))
                additionalStats = manager.GetComponentData<RocketStatsComponent>(tower);
            else if (manager.HasComponent<MortarStatsComponent>(tower))
                additionalStats = manager.GetComponentData<MortarStatsComponent>(tower);

            towerStatsWidget.UpdateStats(attackerComponent, additionalStats);
        }

        private void OnRoguelikeDirectiveClick(ClickEvent clk)
        {
            RoguelikeMainController.Link.AddDirectiveToTower(tower, ((DirectiveWidget)clk.currentTarget).Directive);
        }

        private void OnDirectiveClick(ClickEvent clk)
        {
            DirectiveWidget selectedDirective = (DirectiveWidget)clk.currentTarget;

            directiveTooltip.Show(selectedDirective.Directive != null
                ? selectedDirective.Directive.GetDescription()
                : LocalizationManager.GetTranslation("GameScene/DirectiveNotFound"), selectedDirective);
        }

        private void OnReloadClick(ClickEvent clk)
        {
            AttackerComponent attackerComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AttackerComponent>(tower);
            if (attackerComponent.Bullets < attackerComponent.AttackStats.ReloadStats.MagazineSize)
            {
                GameServices.Instance.ManualReload(tower);
                UpdateAmmoWidget();
                PlaySound2D(SoundKey.Tower_manual_reload);
            }
        }

        private void OnUpgradeClick(ClickEvent clk)
        {
            if (!GameServices.Instance.UpgradeTower(tower))
                return;

            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            CostComponent costComponent = manager.GetComponentData<CostComponent>(tower);
            sellButton.SetPrice(costComponent.SellCost);
            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
            levelLabel_left.text = levelLabel_right.text = (attackerComponent.Level + 1).ToString();

            Messenger<Entity, int>.Broadcast(GameEvents.TowerUpgraded, tower, attackerComponent.Level, MessengerMode.DONT_REQUIRE_LISTENER);
            CheckRankSprites(attackerComponent.Level);
            UpdateUpgradeButton(attackerComponent.Level);
            PlayUpgradeSound(attackerComponent.Level + 1);
        }

        private void PlayUpgradeSound(int level)
        {
            switch (level)
            {
                case 5:
                    PlaySound2D(SoundKey.Tower_upgrade5);
                    break;
                case 10:
                    PlaySound2D(SoundKey.Tower_upgrade10);
                    break;
                case 15:
                    PlaySound2D(SoundKey.Tower_upgrade15);
                    break;
                default:
                    PlaySound2D(SoundKey.Tower_upgrade);
                    break;
            }
        }

        private bool CheckRankSprites(int level)
        {
            if (UIHelper.Instance.GetRankSprite(level) is Sprite rankSprite && rankSprite != null)
            {
                rankIcon1.style.backgroundImage = rankIcon2.style.backgroundImage = new StyleBackground(rankSprite);
                rankIcon1.style.visibility = rankIcon2.style.visibility = Visibility.Visible;
                return true;
            }
            else
            {
                rankIcon1.style.visibility = rankIcon2.style.visibility = Visibility.Hidden;
                return false;
            }
        }

        private void UpdateUpgradeButton(int level)
        {
            /*VisualElement icon = upgradeButton.Q<VisualElement>("ButtonRankIcon");*/
            if (level > 0 && (level + 2) % 5 == 0)
            {
                upgradeButton.SetBackground(UIHelper.Instance.SquareButtonYellow);
                /*icon.style.display = DisplayStyle.Flex;
                icon.style.backgroundImage = new StyleBackground(rankSprite);*/
                upgradeButton.SetText(LocalizationManager.GetTranslation("GameScene/Promote"));
            }
            else
            {
                /*icon.style.display = DisplayStyle.None;*/
                upgradeButton.SetBackground(UIHelper.Instance.AvailableCommonButtonBackground);
                upgradeButton.SetText(LocalizationManager.GetTranslation("GameScene/Upgrade"));
            }
        }

        private void OnSellClick(ClickEvent clk)
        {
            GameServices.Instance.SellTower(tower);
            PlaySound2D(SoundKey.Tower_sell);
        }

        private void OnChangeFireModeClick(ClickEvent clk)
        {
            GameServices.Instance.ChangeFiringModel(tower);

            AttackerComponent attackerComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AttackerComponent>(tower);
            ammoWidget.SetFireModeButtonState(attackerComponent.AttackPattern);
            ammoWidget.PlayOnOffSound(attackerComponent.AttackPattern == AllEnums.AttackPattern.Off);
        }

        private void ToggleTower(ClickEvent clk = null)
        {
            GameServices.Instance.ToggleTower(tower);
            
            AttackerComponent attackerComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AttackerComponent>(tower);
            ammoWidget.PlayOnOffSound(attackerComponent.AttackPattern == AllEnums.AttackPattern.Off);
            ammoWidget.SetFireModeButtonState(attackerComponent.AttackPattern);
        }

        private void UpdateTowerState()
        {
            AttackerComponent attackerComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AttackerComponent>(tower);
            
            if(attackerComponent.AttackPattern == AllEnums.AttackPattern.Off)
                ShowTowerState(AllEnums.TowerState.TurnedOff);
            else if(attackerComponent.ReloadTimer > 0)
                ShowTowerState(AllEnums.TowerState.Reloading);
            else if(attackerComponent.Bullets <= 0 && attackerComponent.AttackPattern != AllEnums.AttackPattern.Off)
                ShowTowerState(AllEnums.TowerState.NoCashForReload);
            else
                ShowTowerState(AllEnums.TowerState.Active);
        }
        
        private void ShowTowerState(AllEnums.TowerState state)
        {
            DOTween.Kill(towerStateIcon, true);
            towerStateIcon.transform.rotation = Quaternion.Euler(new Vector3(0,0,-90));
            towerStateIcon.style.width = 150;
            towerStateIcon.style.height = 150;
            switch (state)
            {
                case AllEnums.TowerState.Active:
                    towerStateIcon.style.visibility = Visibility.Hidden;
                    towerIcon.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                    towerStateLabel.parent.style.visibility = Visibility.Hidden;
                    this.towerState = state;
                    break;
                case AllEnums.TowerState.TurnedOff:
                    towerStateIcon.style.visibility = Visibility.Visible;
                    towerStateIcon.style.backgroundImage = new StyleBackground(UIHelper.Instance.PowerOffIcon);
                    towerIcon.style.unityBackgroundImageTintColor = new StyleColor(Color.gray);
                    towerStateLabel.parent.style.visibility = Visibility.Visible;
                    towerStateLabel.text = LocalizationManager.GetTranslation("TowerStats/Off");
                    this.towerState = state;
                    break;
                case AllEnums.TowerState.Reloading:
                    towerStateIcon.style.visibility = Visibility.Visible;
                    towerStateIcon.style.width = 75;
                    towerStateIcon.style.height = 75;
                    towerStateIcon.style.backgroundImage = new StyleBackground(UIHelper.Instance.ReloadIcon);
                    towerIcon.style.unityBackgroundImageTintColor = new StyleColor(Color.gray);
                    UIHelper.Instance.RotateElement(towerStateIcon, 360, true, 1).SetLoops(-1).SetEase(Ease.Linear).SetTarget(towerStateIcon).Play();
                    towerStateLabel.parent.style.visibility = Visibility.Visible;
                    towerStateLabel.text = LocalizationManager.GetTranslation("TowerStats/Reloading");
                    break;
                case AllEnums.TowerState.NoCashForReload:
                    towerStateIcon.style.visibility = Visibility.Visible;
                    towerStateIcon.style.backgroundImage = new StyleBackground(UIHelper.Instance.NoCashIcon);
                    towerIcon.style.unityBackgroundImageTintColor = new StyleColor(Color.gray);
                    towerStateLabel.parent.style.visibility = Visibility.Visible;
                    towerStateLabel.text = LocalizationManager.GetTranslation("TowerStats/NoAmmo");
                    break;
            }
        }

        public void UpdateLocalization()
        {
            sellButton.SetText(LocalizationManager.GetTranslation("GameScene/Sell"));
            upgradeButton.SetText(LocalizationManager.GetTranslation("GameScene/Upgrade"));
            levelText_right.text = levelText_left.text = LocalizationManager.GetTranslation("Menu/Level");
            ammoWidget.UpdateLocalization();
            uiHelper.SetLocalizationFont(levelText_right);
            uiHelper.SetLocalizationFont(levelText_left);
            uiHelper.SetLocalizationFont(ammoWidget);
            uiHelper.SetLocalizationFont(towerStatsWidget);
        }
    }
}