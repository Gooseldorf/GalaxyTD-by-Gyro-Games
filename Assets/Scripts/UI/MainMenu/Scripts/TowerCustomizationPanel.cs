using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using Sounds.Attributes;
using System;
using System.Collections.Generic;
using Tags;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class TowerCustomizationPanel : VisualElement, IMenuPanel
    {
        public new class UxmlFactory : UxmlFactory<TowerCustomizationPanel> { }

        private Label titleLabel;
        public FactoryWidget FactoryWidget;
        private TowerInfoWidget towerInfoWidget;
        private DirectiveSelector directiveSelector;
        private PartSelector partSelector;
        private AmmoPartSelector ammoPartSelector;
        private PriceButton upgradeButton;
        private ConfirmWindow confirmWindow;
        private ClickableVisualElement leftScrollButton;
        private ClickableVisualElement rightScrollButton;
        private VisualElement factoryWidgetContainer;
        private VisualElement infoContainer;

        private TowerFactory currentFactory;
        private List<TowerFactory> availableFactories;
        private int currentFactoryIndex;
        private Sequence factoryTransitionSeq;
        private Sequence infoTransitionSeq;
        private Tween scrollButtonTween;

        private DataManager dataManager;
        private UIHelper uiHelper;
        public event Action OnDirectiveShopButton;

        public void Init(VisualTreeAsset directiveWidgetPrefab, VisualTreeAsset partWidgetPrefab, VisualTreeAsset ammoPartWidgetPrefab, ShopPanel shopPanel, ConfirmWindow confirmWindow)
        {
            titleLabel = this.Q<Label>("TitleLabel");
            factoryWidgetContainer = this.Q<VisualElement>("FactoryWidgetContainer");
            infoContainer = this.Q<VisualElement>("InfoContainer");
            FactoryWidget = this.Q<FactoryWidget>("FactoryWidget");
            FactoryWidget.InitWithCallbacks();
            FactoryWidget.OnPartSelected += PlayInfoPanelTransition;

            towerInfoWidget = this.Q<TowerInfoWidget>("TowerInfoWidget");
            towerInfoWidget.Init();

            directiveSelector = this.Q<DirectiveSelector>("DirectiveSelector");
            directiveSelector.Init(directiveWidgetPrefab);
            directiveSelector.OnApplyDirective += ApplyPart;
            directiveSelector.OnRemoveDirective += RemoveDirective;
            directiveSelector.DirectiveShopButton.RegisterCallback<ClickEvent>(OnDirectiveShopButtonClick);

            partSelector = this.Q<PartSelector>("PartSelector");
            partSelector.Init(partWidgetPrefab, shopPanel, confirmWindow);
            partSelector.OnApplyPart += ApplyPart;

            ammoPartSelector = this.Q<AmmoPartSelector>("AmmoPartSelector");
            ammoPartSelector.Init(ammoPartWidgetPrefab, shopPanel, confirmWindow);
            ammoPartSelector.OnApplyPart += ApplyPart;

            upgradeButton = towerInfoWidget.Q<PriceButton>("UpgradeButton");
            upgradeButton.Init();
            upgradeButton.RegisterCallback<ClickEvent>(OnUpgradeButtonClick);

            leftScrollButton = this.Q<ClickableVisualElement>("LeftArrowIcon");
            leftScrollButton.Init();
            leftScrollButton.RegisterCallback<ClickEvent>(OnScrollButtonClick);
            rightScrollButton = this.Q<ClickableVisualElement>("RightArrowIcon");
            rightScrollButton.Init();
            rightScrollButton.RegisterCallback<ClickEvent>(OnScrollButtonClick);

            this.confirmWindow = confirmWindow;

            dataManager = DataManager.Instance;
            uiHelper = UIHelper.Instance;

            availableFactories = GetAvailableFactories();

            Messenger.AddListener(UIEvents.OnNewItemsUpdated, FactoryWidget.UpdateIsNewNotifications);

            style.display = DisplayStyle.None;
        }

        public void Dispose()
        {
            FactoryWidget.Dispose();
            FactoryWidget.OnPartSelected -= PlayInfoPanelTransition;

            directiveSelector.Dispose();
            directiveSelector.OnApplyDirective -= ApplyPart;
            directiveSelector.OnRemoveDirective -= RemoveDirective;

            partSelector.Dispose();
            partSelector.OnApplyPart -= ApplyPart;

            ammoPartSelector.Dispose();
            ammoPartSelector.OnApplyPart -= ApplyPart;

            upgradeButton.Dispose();
            upgradeButton.UnregisterCallback<ClickEvent>(OnUpgradeButtonClick);

            Messenger.RemoveListener(UIEvents.OnNewItemsUpdated, FactoryWidget.UpdateIsNewNotifications);
        }

        private List<TowerFactory> GetAvailableFactories()
        {
            UnlockManager unlockManager = dataManager.Get<UnlockManager>();
            List<TowerFactory> result = new();
            foreach (ITowerFactory factoryData in dataManager.GameData.TowerFactories)
            {
                if (unlockManager.IsTowerUnlocked(factoryData.TowerId))
                    result.Add((TowerFactory)factoryData);
            }
            result.Sort(DataManager.Instance.Get<UnlockManager>().TowerFactoryComparer);
            return result;
        }

        public void SetFactory(TowerFactory factory, bool transition = false)
        {
            currentFactory = factory;
            ammoPartSelector?.SetFactory(currentFactory);
            FactoryWidget.SetTower(factory);
            FactoryWidget.UpdateIsNewNotifications();
            upgradeButton.SetPrice(dataManager.Get<UpgradeProvider>().GetNextUpgrade(factory.TowerId, factory.Level).Cost);
            if (!transition)
                FactoryWidget.SelectTowerIcon();

            SetInfo(FactoryWidget.LastSelected);
            currentFactoryIndex = availableFactories.IndexOf(factory);
            UpdateScrollButtons();
        }

        public void SetInfo(SelectableElement selectedWidget)
        {
            switch (selectedWidget)
            {
                case DirectiveWidget directiveWidget:
                    directiveSelector.Show(currentFactory.TowerId, directiveWidget.Directive);
                    towerInfoWidget.Hide();
                    partSelector.Hide();
                    ammoPartSelector.Hide();
                    break;

                case SlotWidget slotWidget:
                    partSelector.Show(currentFactory.TowerId, slotWidget.Slot.WeaponPart);
                    towerInfoWidget.Hide();
                    directiveSelector.Hide();
                    ammoPartSelector.Hide();
                    break;

                case AmmoWidget ammoWidget:
                    ammoPartSelector.Show(currentFactory.TowerId, ammoWidget.Slot.WeaponPart);
                    towerInfoWidget.Hide();
                    directiveSelector.Hide();
                    partSelector.Hide();
                    break;

                default:
                    towerInfoWidget.Show(currentFactory);
                    directiveSelector.Hide();
                    partSelector.Hide();
                    ammoPartSelector.Hide();
                    break;
            }
        }

        private void OnUpgradeButtonClick(ClickEvent clk)
        {
            if (dataManager.GameData.UpgradeFactory(currentFactory))
            {
                FactoryWidget.UpdateStats(currentFactory);
                towerInfoWidget.UpdateUpgradeDescription();
                FactoryWidget.ShowUpgradeEffect();
                MenuUpgrade nextUpgrade = dataManager.Get<UpgradeProvider>().GetNextUpgrade(currentFactory.TowerId, currentFactory.Level);
                upgradeButton.SetPrice(nextUpgrade.Cost);
                PlaySound2D(SoundKey.Menu_workshop_upgrade);
            }
            else
            {
                confirmWindow.SetUp(uiHelper.SoftCurrencyReward, LocalizationManager.GetTranslation("ConfirmWindow/NotEnoughSoft_desc"), () => Messenger<AllEnums.CurrencyType>.Broadcast(UIEvents.GoToShop, AllEnums.CurrencyType.Soft, MessengerMode.DONT_REQUIRE_LISTENER));
                confirmWindow.Show();
            }
        }

        private void ApplyPart(WeaponPart part)
        {
            switch (FactoryWidget.LastSelected)
            {
                case DirectiveWidget directive:
                    directive.SetDirective(part);
                    dataManager.GameData.InsertPart(currentFactory, part, directive.Slot);
                    dataManager.GameData.Inventory.RemoveDirectiveFromInventory(part);
                    break;
                case SlotWidget slot:
                    slot.SetWeaponPart(part);
                    dataManager.GameData.InsertPart(currentFactory, part, slot.Slot);
                    break;
                case AmmoWidget ammo:
                    ammo.SetAmmo(part, currentFactory.GetAssembledTower().AttackStats.ReloadStats.MagazineSize);
                    dataManager.GameData.InsertPart(currentFactory, part, ammo.Slot);
                    dataManager.GameData.Inventory.RemoveAmmoPartFromInventory(ammo.Ammo);
                    break;
            }

            currentFactory.RefreshTower();
            FactoryWidget.UpdateStats(currentFactory);
            FactoryWidget.LastSelected.PlayOnClickAnimation(new ClickEvent());
        }

        private void RemoveDirective()
        {
            dataManager.GameData.RemoveDirective(currentFactory, ((DirectiveWidget)FactoryWidget.LastSelected).Slot);
            ((DirectiveWidget)FactoryWidget.LastSelected).RemoveDirective();

            currentFactory.RefreshTower();
            FactoryWidget.UpdateStats(currentFactory);
        }

        private void OnDirectiveShopButtonClick(ClickEvent clk) => OnDirectiveShopButton?.Invoke();

        private void OnScrollButtonClick(ClickEvent clk)
        {
            VisualElement target = (VisualElement)clk.currentTarget;
            if ((currentFactoryIndex == 0 && target == leftScrollButton) || (currentFactoryIndex >= availableFactories.Count - 1 && target == rightScrollButton)) return;

            scrollButtonTween?.Kill();
            scrollButtonTween = target == leftScrollButton ? uiHelper.InOutScaleTween(target, -1, -1.1f, 0.3f) : uiHelper.InOutScaleTween(rightScrollButton, 1, 1.1f, 0.3f);
            scrollButtonTween.SetTarget(target).Play();
            UpdateScrollButtons();

            int nextFactoryIndex = target == rightScrollButton ? currentFactoryIndex + 1 : currentFactoryIndex - 1;
            PlayFactoryPanelTransition(nextFactoryIndex);
        }

        private void PlayFactoryPanelTransition(int nextFactoryIndex)
        {
            factoryTransitionSeq?.Complete(true);
            factoryTransitionSeq = DOTween.Sequence();

            factoryTransitionSeq.Append(uiHelper.GetMenuPanelFadeTween(factoryWidgetContainer, false, true));
            factoryTransitionSeq.Insert(0, DOVirtual.DelayedCall(0, () => PlayInfoPanelTransition(FactoryWidget.LastSelected)));

            factoryTransitionSeq.Append(DOVirtual.DelayedCall(0, () =>
            {
                SetFactory(availableFactories[nextFactoryIndex], true);
            }));
            factoryTransitionSeq.Append(uiHelper.GetMenuPanelFadeTween(factoryWidgetContainer, true, true));
            factoryTransitionSeq.SetTarget(factoryWidgetContainer).Play();
        }

        private void PlayInfoPanelTransition(SelectableElement selectedPart)
        {
            if (infoTransitionSeq != null && infoTransitionSeq.IsActive() && !infoTransitionSeq.IsComplete())
                infoTransitionSeq.Complete(true);
            infoTransitionSeq = DOTween.Sequence();
            infoTransitionSeq.Append(uiHelper.GetMenuPanelFadeTween(infoContainer, false, true));
            infoTransitionSeq.Append(DOVirtual.DelayedCall(0, () => SetInfo(selectedPart)));
            infoTransitionSeq.Append(uiHelper.GetMenuPanelFadeTween(infoContainer, true, true));
            infoTransitionSeq.SetTarget(infoContainer).Play();
        }

        private void UpdateScrollButtons()
        {
            leftScrollButton.style.opacity = currentFactoryIndex <= 0 ? new StyleFloat(0.3f) : new StyleFloat(1f);
            leftScrollButton.SoundName = currentFactoryIndex <= 0 ? SoundConstants.EmptyKey : SoundKey.Interface_button;
            rightScrollButton.style.opacity = currentFactoryIndex >= availableFactories.Count - 1 ? new StyleFloat(0.3f) : new StyleFloat(1f);
            rightScrollButton.SoundName = currentFactoryIndex >= availableFactories.Count - 1 ? SoundConstants.EmptyKey : SoundKey.Interface_button;
        }

        public void UpdateLocalization()
        {
            titleLabel.text = LocalizationManager.GetTranslation("Menu/Assemble");
            FactoryWidget.UpdateLocalization();
            partSelector.UpdateLocalization();
            upgradeButton.SetText(LocalizationManager.GetTranslation("Menu/TuneUp"));
        }
    }
}