using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class PartSelector : Selector
    {
        public new class UxmlFactory : UxmlFactory<PartSelector>
        {
        }

        private VisualElement partsContainer;
        private Label selectorTitleLabel;
        private Label partDesc;
        private ScrollView partDescriptionScrollView;
        private PriceButton buyButton;
        private VisualElement lockedBuyButton;
        private Label lockedLabel;

        protected List<WeaponPart> currentParts = new();
        private List<PartWidget> partWidgets = new();
        private VisualTreeAsset partWidgetPrefab;

        private UnlockManager unlockManager;
        private ShopPanel shopPanel;
        private ConfirmWindow confirmWindow;

        private CostDescriptionWidget costDescriptionWidget;
        private FireModeWidget fireModeWidget;
        private WeaponPart currentPart;
        private UIHelper uiHelper;

        public event Action<WeaponPart> OnApplyPart;

        public virtual void Init(VisualTreeAsset partWidgetPrefab, ShopPanel shopPanel, ConfirmWindow confirmWindow)
        {
            uiHelper = UIHelper.Instance;
            this.partWidgetPrefab = partWidgetPrefab;

            selectorTitleLabel = this.Q<Label>("SelectorTitle");

            partDesc = this.Q<Label>("Desc");
            partDescriptionScrollView = this.Q<ScrollView>("DescriptionScroll");
            partsContainer = this.Q<VisualElement>("PartsContainer");

            buyButton = this.Q<PriceButton>("BuyButtonParts");
            buyButton.Init();
            buyButton.RegisterCallback<ClickEvent>(OnBuyClick);

            lockedBuyButton = this.Q<VisualElement>("LockedBuyButton");
            lockedLabel = this.Q<Label>("LockedLabel");

            costDescriptionWidget = this.Q<TemplateContainer>("CostDescriptionWidget").Q<CostDescriptionWidget>();
            costDescriptionWidget.Init();

            fireModeWidget = this.Q<TemplateContainer>("FireModeWidget")?.Q<FireModeWidget>();
            fireModeWidget?.Init();

            this.shopPanel = shopPanel;
            this.confirmWindow = confirmWindow;
            unlockManager = DataManager.Instance.Get<UnlockManager>();
        }

        public void Dispose()
        {
            buyButton.Dispose();
            buyButton.UnregisterCallback<ClickEvent>(OnBuyClick);

            buyButton.Dispose();

            foreach (PartWidget partWidget in partWidgets)
            {
                partWidget.Dispose();
                partWidget.UnregisterCallback<ClickEvent>(OnPartClick);
            }
        }

        public virtual void Show(AllEnums.TowerId towerId, WeaponPart selectedPart)
        {
            style.display = DisplayStyle.Flex;

            if (selectedPart == null) return;

            UpdateWeaponParts(towerId, selectedPart.PartType);
            UpdateLocalization();

            PartWidget selectedWidget = partWidgets.Find(x => x.Part == selectedPart);
            Select(selectedWidget);
            SetPart(selectedWidget);
        }

        public void Hide() => style.display = DisplayStyle.None;

        private void UpdateWeaponParts(AllEnums.TowerId towerId, AllEnums.PartType partType)
        {
            partWidgets.Clear();
            partsContainer.Clear();

            currentParts = FindWeaponParts(towerId, partType);

            currentParts.Sort(DataManager.Instance.Get<UnlockManager>().WeaponPartComparer);

            if (partWidgets.Count < currentParts.Count)
            {
                while (currentParts.Count != partWidgets.Count)
                {
                    PartWidget newPartWidget = partWidgetPrefab.Instantiate().Q<PartWidget>();
                    newPartWidget.Init();
                    newPartWidget.RegisterCallback<ClickEvent>(OnPartClick);
                    partWidgets.Add(newPartWidget);
                    partsContainer.Add(newPartWidget);
                }
            }
            else
            {
                for (int i = partWidgets.Count - 1; i >= currentParts.Count; i--)
                    partWidgets[i].style.display = DisplayStyle.None;
            }

            AdjustUiState(partWidgets);
        }

        protected virtual List<WeaponPart> FindWeaponParts(AllEnums.TowerId towerId, AllEnums.PartType partType)
        {
            return DataManager.Instance.Get<PartsHolder>().Items.FindAll(x => x.TowerId == towerId && x.PartType == partType);
        }

        protected virtual void AdjustUiState(List<PartWidget> partWidgets)
        {
            for (int i = 0; i < partWidgets.Count; i++)
            {
                partWidgets[i].SetPart(currentParts[i]);
                partWidgets[i].name = $"PartWidget_{currentParts[i].SerializedID}";

                partWidgets[i].SetIsNewNotification(DataManager.Instance.GameData.NewItems.Contains(currentParts[i]));

                partWidgets[i].SetState(unlockManager.IsPartUnlocked(currentParts[i])
                    ? AllEnums.UIState.Available
                    : AllEnums.UIState.Locked);

                if ((DataManager.Instance.GameData.Inventory.UnusedWeaponParts).Contains(currentParts[i]))
                    partWidgets[i].SetState(AllEnums.UIState.Active);

                partWidgets[i].style.display = DisplayStyle.Flex;
            }
        }

        private void OnPartClick(ClickEvent clk)
        {
            PartWidget target = (PartWidget)clk.currentTarget;
            Select(target);
            SetPart(target);

            if (target.State == AllEnums.UIState.Active)
                OnApplyPart?.Invoke(target.Part);

            if (DataManager.Instance.GameData.NewItems.Contains(target.Part))
            {
                DataManager.Instance.GameData.RemoveFromNewItems(target.Part);
                target.SetIsNewNotification(false);
            }
        }

        protected virtual void SetPart(PartWidget partWidget)
        {
            currentPart = partWidget.Part;

            AnimateText();
            costDescriptionWidget.SetPart(partWidget.Part);
            this.Q<VisualElement>("BackgroundIcon").style.backgroundImage = new StyleBackground(currentPart.Sprite);
            if (fireModeWidget != null)
            {
                if (partWidget.Part.PartType is AllEnums.PartType.RecoilSystem or AllEnums.PartType.TargetingSystem)
                {
                    fireModeWidget.style.display = DisplayStyle.Flex;
                    fireModeWidget.SetPart(partWidget.Part);
                }
                else
                {
                    fireModeWidget.style.display = DisplayStyle.None;
                }
            }

            switch (partWidget.State)
            {
                case AllEnums.UIState.Locked:
                    buyButton.style.display = DisplayStyle.None;
                    lockedBuyButton.style.display = DisplayStyle.Flex;
                    lockedLabel.text = $"{LocalizationManager.GetTranslation("Mission")} {DataManager.Instance.Get<UnlockManager>().GetPartUnlockMission(partWidget.Part) + 1}";
                    break;
                case AllEnums.UIState.Available:
                    lockedBuyButton.style.display = DisplayStyle.None;
                    buyButton.style.display = DisplayStyle.Flex;
                    buyButton.SetText(LocalizationManager.GetTranslation("Menu/Construct"));
                    buyButton.SetPrice(partWidget.Part.ScrapCost);
                    break;
                case AllEnums.UIState.Active:
                    buyButton.style.display = DisplayStyle.None;
                    lockedBuyButton.style.display = DisplayStyle.Flex;
                    lockedLabel.text = LocalizationManager.GetTranslation("Menu/Constructed");
                    PlayPartTypeSound(partWidget.Part.PartType);
                    break;
            }
        }

        private void OnBuyClick(ClickEvent clk)
        {
            WeaponPart part = ((PartWidget)LastSelected).Part;
            if (DataManager.Instance.GameData.BuyPart(part))
            {
                Messenger<WeaponPart>.Broadcast(GameEvents.BuyWeaponPart, part, MessengerMode.DONT_REQUIRE_LISTENER);
                ((PartWidget)LastSelected).SetState(AllEnums.UIState.Active);
                SetPart((PartWidget)LastSelected);
                OnApplyPart?.Invoke(part);
            }
            else
            {
                confirmWindow.SetUp(UIHelper.Instance.ScrapReward, LocalizationManager.GetTranslation("ConfirmWindow/NotEnoughScrap_desc"),
                    () => Messenger<AllEnums.CurrencyType>.Broadcast(UIEvents.GoToShop, AllEnums.CurrencyType.Scrap, MessengerMode.DONT_REQUIRE_LISTENER));
                confirmWindow.Show();
            }
        }

        private void AnimateText()
        {
            AnimateTitle();
            AnimateDescription();
        }

        private void AnimateTitle()
        {
            DOTween.Kill(selectorTitleLabel);
            string title = currentPart.GetTitle();
            UIHelper.Instance.PlayTypewriter(selectorTitleLabel, title);
        }

        private void AnimateDescription()
        {
            DOTween.Kill(partDesc);
            string description = currentPart.GetDescription();
            UIHelper.Instance.PlayTypewriter(partDesc, description, true, partDescriptionScrollView, 1.5f);
        }

        private void PlayPartTypeSound(AllEnums.PartType type)
        {
            switch (type)
            {
                case AllEnums.PartType.Barrel:
                    PlaySound2D(SoundKey.Menu_workshop_barrelSet);
                    break;
                case AllEnums.PartType.Magazine:
                    PlaySound2D(SoundKey.Menu_workshop_magazineSet);
                    break;
                case AllEnums.PartType.RecoilSystem:
                    PlaySound2D(SoundKey.Menu_workshop_recoilSet);
                    break;
                case AllEnums.PartType.TargetingSystem:
                    PlaySound2D(SoundKey.Menu_workshop_recoilSet);
                    break;
                case AllEnums.PartType.Ammo:
                    PlaySound2D(SoundKey.Menu_workshop_ammoSet);
                    break;
                case AllEnums.PartType.Directive:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public void UpdateLocalization()
        {
            buyButton?.SetText(LocalizationManager.GetTranslation("Menu/Construct"));
            if (fireModeWidget != null)
            {
                fireModeWidget.UpdateLocalization();
                uiHelper.SetLocalizationFont(fireModeWidget);
            }

            foreach (PartWidget partWidget in partWidgets)
            {
                partWidget.UpdateLocalization();
                uiHelper.SetLocalizationFont(partWidget);
            }
        }
    }
}