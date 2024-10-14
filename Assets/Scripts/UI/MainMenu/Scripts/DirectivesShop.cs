using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UI
{
    public class DirectivesShop : Selector, IMenuPanel
    {
        public new class UxmlFactory : UxmlFactory<DirectivesShop> {}

        private Label titleLabel;
        private VisualElement icon;
        private Label directiveTitle;
        private Label descLabel;
        private ScrollView descScroll;
        private Label additionalDesc;
        private PriceButton buyButton;
        private Label lockedLabel;
        private VisualElement directivesContainer;
        private List<DirectiveWidget> directiveWidgets = new();
        private DirectiveWidget selectedDirective;
        private CostDescriptionWidget costDescriptionWidget;
        
        private ConfirmWindow confirmWindow;
        private NewItemsWindow newItemsWindow;
        private UnlockManager unlockManager;
        private UIHelper uiHelper;

        public void Init(VisualTreeAsset directiveWidgetPrefab, ConfirmWindow confirmWindow, NewItemsWindow newItemsWindow)
        {
            this.confirmWindow = confirmWindow;
            this.newItemsWindow = newItemsWindow;
            uiHelper = UIHelper.Instance;
            
            titleLabel = this.Q<Label>("TitleLabel");
            icon = this.Q<VisualElement>("Icon");
            directiveTitle = this.Q<Label>("DirectiveTitle");
            descLabel = this.Q<Label>("Desc");
            descScroll = this.Q<ScrollView>("DescriptionScroll");
            additionalDesc = this.Q<Label>("AdditionalDesc");

            buyButton = this.Q<PriceButton>("BuyButton");
            buyButton.Init();
            buyButton.RegisterCallback<ClickEvent>(OnBuyButtonClick);

            lockedLabel = this.Q<Label>("LockedLabel");
            
            directivesContainer = this.Q<VisualElement>("DirectivesContainer");
            
            costDescriptionWidget = this.Q<TemplateContainer>("CostDescriptionWidget").Q<CostDescriptionWidget>();
            costDescriptionWidget.Init();

            unlockManager = DataManager.Instance.Get<UnlockManager>();

            List<WeaponPart> directivesData = DataManager.Instance.Get<PartsHolder>().Directives;
            
            directivesData.Sort(DataManager.Instance.Get<UnlockManager>().WeaponPartComparer);
            
            for (int i = 0; i < directivesData.Count; i++) 
            {
                DirectiveWidget newDirective =
                    directiveWidgetPrefab.Instantiate().Q<DirectiveWidget>("DirectiveWidget");
                
                newDirective.Init();
                newDirective.SetDirective(directivesData[i]);
                UpdateDirectiveCount(newDirective);
                newDirective.SetState(unlockManager.IsPartUnlocked(directivesData[i])
                    ? AllEnums.UIState.Available : AllEnums.UIState.Locked);

                newDirective.RegisterCallback<ClickEvent>(OnDirectiveClick);
                newDirective.AddToClassList(USSClasses.Margin30);

                directiveWidgets.Add(newDirective);
                directivesContainer.Add(newDirective);
            }
            
            selectedDirective = directiveWidgets[0];
            Select(directiveWidgets[0]);
            DataManager.Instance.GameData.RemoveFromNewItems(directiveWidgets[0].Directive);
            UpdateIsNewNotifications();
            //UpdateInfo();

            Messenger.AddListener(UIEvents.OnNewItemsUpdated, UpdateIsNewNotifications);
            style.display = DisplayStyle.None;
        }

        private void UpdateIsNewNotifications()
        {
            GameData gameData = DataManager.Instance.GameData;
            foreach (var widget in directiveWidgets)
            {
                widget.SetIsNewNotification(gameData.NewItems.Contains(widget.Directive));
            }
        }

        public void Dispose()
        {
            buyButton.Dispose();
            buyButton.UnregisterCallback<ClickEvent>(OnBuyButtonClick);
            
            foreach (DirectiveWidget directiveWidget in directiveWidgets)
                directiveWidget.UnregisterCallback<ClickEvent>(OnDirectiveClick);
            
            Messenger.RemoveListener(UIEvents.OnNewItemsUpdated, UpdateIsNewNotifications);
        }
        
        private void OnDirectiveClick(ClickEvent clk)
        {
            if(((DirectiveWidget)clk.currentTarget).State == AllEnums.UIState.Locked) return;
            
            selectedDirective = (DirectiveWidget)clk.currentTarget;
            Select(selectedDirective);
            DataManager.Instance.GameData.RemoveFromNewItems(selectedDirective.Directive);
            selectedDirective.Q<VisualElement>("IsNewNotification").style.display = DisplayStyle.None;
            UpdateInfo();
        }
        
        public void UpdateInfo()
        {
            AnimateText();
            icon.style.backgroundImage = new StyleBackground(selectedDirective.Directive.Sprite);
            costDescriptionWidget.SetPart(selectedDirective.Directive);
            buyButton.SetPrice(selectedDirective.Directive.HardCost);
            switch (selectedDirective.State)
            {
                case AllEnums.UIState.Locked:
                    buyButton.SetState(AllEnums.UIState.Locked);
                    additionalDesc.style.visibility = Visibility.Hidden;
                    break;
                
                case AllEnums.UIState.Available:
                    buyButton.SetState(AllEnums.UIState.Available);
                    buyButton.SetText(LocalizationManager.GetTranslation("Menu/Buy"));
                    if ((DataManager.Instance.GameData.Inventory.UnusedDirectives).ContainsKey(selectedDirective.Directive))
                    {
                        additionalDesc.style.visibility = Visibility.Visible;
                        DOTween.Kill(additionalDesc);
                        uiHelper.PlayTypewriter(additionalDesc, LocalizationManager.GetTranslation("Menu/Directives_additionalText").Replace("{param}", DataManager.Instance.GameData.Inventory.UnusedDirectives[selectedDirective.Directive].ToString()));
                    }
                    else additionalDesc.text = "";
                    
                    break;
            }
        }

        private void OnBuyButtonClick(ClickEvent clk)
        {
            if(buyButton.State != AllEnums.UIState.Available) return;
            if (DataManager.Instance.GameData.HardCurrency >= selectedDirective.Directive.HardCost)
            {
                /*confirmWindow.SetUp(selectedDirective.Directive.Sprite, LocalizationManager.GetTranslation("ConfirmWindow/BuyForSoft_desc").Replace("{param1}", directiveTitle.text).Replace("{param2}", selectedDirective.Directive.HardCost.ToString()), 
                    () =>
                    {*/
                        DataManager.Instance.GameData.BuyPart(selectedDirective.Directive);
                        UpdateDirectiveCount(selectedDirective);
                        //selectedDirective.SetState(AllEnums.UIState.Active);
                        UpdateInfo();
                        newItemsWindow.ShowDirective(selectedDirective.Directive, LocalizationManager.GetTranslation("ConfirmWindow/NewDirectiveReceived"));
                        newItemsWindow.Show();
                    /*});*/
            }
            else
            {
                confirmWindow.SetUp(uiHelper.HardReward, LocalizationManager.GetTranslation("ConfirmWindow/NotEnoughHard_desc"), () => Messenger<AllEnums.CurrencyType>.Broadcast(UIEvents.GoToShop, AllEnums.CurrencyType.Hard, MessengerMode.DONT_REQUIRE_LISTENER));
                confirmWindow.Show();
            }
            
        }

        private void UpdateDirectiveCount(DirectiveWidget widget)
        {
            if (DataManager.Instance.GameData.Inventory.UnusedDirectives.ContainsKey(widget.Directive))
                widget.SetCount(DataManager.Instance.GameData.Inventory.UnusedDirectives[widget.Directive]);
            else
                widget.SetCount(0);
        }
        
        public void AnimateText()
        {
            AnimateTitle();
            AnimateDescription();
        }

        private void AnimateTitle()
        {
            DOTween.Kill(directiveTitle);
            string title = directiveWidgets.Count == 0 ? LocalizationManager.GetTranslation("WeaponParts/DirectiveSelection") : ((DirectiveWidget)LastSelected).Directive.GetTitle();
            uiHelper.PlayTypewriter(directiveTitle, title);
        }

        private void AnimateDescription()
        {
            DOTween.Kill(descLabel);
            string description = directiveWidgets.Count == 0 ? LocalizationManager.GetTranslation($"WeaponParts/DirectiveSelection_desc") : ((DirectiveWidget)LastSelected).Directive.GetDescription();
            uiHelper.PlayTypewriter(descLabel, description, true, descScroll, 1.5f, true);
        }

        public void UpdateLocalization()
        {
            titleLabel.text = LocalizationManager.GetTranslation("Menu/DirectivesButton");
            //uiHelper.SetLocalizationFont(titleLabel);
            buyButton.SetText(LocalizationManager.GetTranslation("Menu/Buy"));
            lockedLabel.text = LocalizationManager.GetTranslation("Locked");
            uiHelper.SetLocalizationFont(this);
            //uiHelper.SetLocalizationFont(lockedLabel);

        }
    }
}