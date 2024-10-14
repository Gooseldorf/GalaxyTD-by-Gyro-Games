using CardTD.Utilities;
using Data.Managers;
using DG.Tweening;
using I2.Loc;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static AllEnums;
using static MusicManager;

namespace UI
{
    public class ShopPanel : Selector, IMenuPanel
    {
        public new class UxmlFactory : UxmlFactory<ShopPanel>
        {
        }

        private Label titleLabel;
        private ScrollView scrollView;
        
        private List<SelectableElement> tabSelectButtons;
        private List<VisualElement> tabs;
        private List<VisualElement> columns;
        private ClickableVisualElement infoButton;
        private VisualElement questionMark;
        private Notification notification;
        private Vector2[] tabScrollOffsets;
        private int currentTabIndex;
        private float2 currentTabBorders;
        private Vector2[] columnsScrollOffsets;

        private UIHelper uiHelper;
        private const float snappingSpeed = 0.5f;
        private CurrencyType currentCurrency;

        private List<GoodsWidget> allGoodsWidgets;
        
        private ConfirmWindow confirmWindow;
        private NewItemsWindow newItemsWindow;
        private Action onConfirmAction;
        private bool firstInit;
        
        public void Init(ConfirmWindow confirmWindow, NewItemsWindow newItemsWindow)
        {
            this.confirmWindow = confirmWindow;
            this.newItemsWindow = newItemsWindow;
            uiHelper = UIHelper.Instance;
            
            titleLabel = this.Q<Label>("TitleLabel");
            scrollView = this.Q<ScrollView>();
            tabSelectButtons = this.Q<VisualElement>("TabSelectContainer").Query<SelectableElement>().ToList();
            foreach (SelectableElement tabSelectButton in tabSelectButtons)
            {
                tabSelectButton.Init();
                tabSelectButton.RegisterCallback<ClickEvent>(OnTabSelectButtonClick);
            }

            tabs = new();
            foreach (var child in scrollView.Children())
                tabs.Add(child);
            SetUpShopItems();
            columns = scrollView.Query<VisualElement>("Column").ToList();
            infoButton = this.Q<ClickableVisualElement>("InfoButton");
            infoButton.Init();
            infoButton.RegisterCallback<ClickEvent>(OnInfoClick);
            
            currentTabIndex = 0;
            this.RegisterCallback<GeometryChangedEvent>(OnResolve);
            Select(tabSelectButtons[0]);
            
            Messenger<string>.AddListener(UIEvents.PurchaseCompleted, OnCompletePurchase);
            Messenger<List<WeaponPart>>.AddListener(UIEvents.PurchaseWeaponPartsCompleted, ShowPurchasedItems);

            notification = this.Q<TemplateContainer>("Notification").Q<Notification>();
            notification.Init();
            firstInit = true;
            //style.display = DisplayStyle.None;
        }
        
        public void Dispose()
        {
            foreach (var widget in allGoodsWidgets)
            {
                widget.Dispose();
                widget.UnregisterCallback<ClickEvent>(OnGoodsClick);
            }
            infoButton.UnregisterCallback<ClickEvent>(OnInfoClick);
            Messenger<string>.RemoveListener(UIEvents.PurchaseCompleted, OnCompletePurchase);
        }

        private void SetUpShopItems()
        {
            allGoodsWidgets = new();
            List<GoodsWidget> offers = scrollView.contentContainer.Q<VisualElement>("Offers").Query<GoodsWidget>().ToList();
            SetUpWidgetsGroup(offers, uiHelper.OffersItems);
            List<GoodsWidget> directives = scrollView.contentContainer.Q<VisualElement>("Directives").Query<GoodsWidget>().ToList();
            SetUpWidgetsGroup(directives, uiHelper.DirectiveBundlesItems);
            List<GoodsWidget> bundles = scrollView.contentContainer.Q<VisualElement>("Bundles").Query<GoodsWidget>().ToList();
            SetUpWidgetsGroup(bundles, uiHelper.BundlesItems);
            List<GoodsWidget> hard = scrollView.contentContainer.Q<VisualElement>("Hard").Query<GoodsWidget>().ToList();
            SetUpWidgetsGroup(hard, uiHelper.HardItems);
            List<GoodsWidget> soft = scrollView.contentContainer.Q<VisualElement>("Soft").Query<GoodsWidget>().ToList();
            SetUpWidgetsGroup(soft, uiHelper.SoftItems);
            List<GoodsWidget> tickets = scrollView.contentContainer.Q<VisualElement>("Tickets").Query<GoodsWidget>().ToList();
            SetUpWidgetsGroup(tickets, uiHelper.TicketItems);
            List<GoodsWidget> scrap = scrollView.contentContainer.Q<VisualElement>("Scrap").Query<GoodsWidget>().ToList();
            SetUpWidgetsGroup(scrap, uiHelper.ScrapItems);
        }

        private void SetUpWidgetsGroup(List<GoodsWidget> widgets, IReadOnlyList<GoodsItem> items)
        {
            if (widgets.Count != items.Count)
            {
                Debug.LogError($"{nameof(ShopPanel)}: unequal widgets and items count!");
            }
            for (int i = 0; i < widgets.Count; i++)
            {
                if (i >= items.Count) break;
                widgets[i].Init(items[i]);
                widgets[i].RegisterCallback<ClickEvent>(OnGoodsClick);
            }
            allGoodsWidgets.AddRange(widgets);
        }
        
        private void OnResolve(GeometryChangedEvent geom)
        {
            if(float.IsNaN(tabs[^1].resolvedStyle.width) || tabs[^1].resolvedStyle.width == 0) return;
            
            //this.UnregisterCallback<GeometryChangedEvent>(OnResolve);
            ((VisualElement)geom.currentTarget).UnregisterCallback<GeometryChangedEvent>(OnResolve);
            CalculateOffsets();
        }

        private void CalculateOffsets()
        {
            tabScrollOffsets = new Vector2[tabs.Count];
            for (int i = 0; i < tabs.Count; i++)
            {
                tabScrollOffsets[i] = new Vector2(tabs[i].worldBound.x - scrollView.worldBound.x, 0);
            }

            columnsScrollOffsets = new Vector2[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                columnsScrollOffsets[i] = new Vector2(columns[i].worldBound.x - scrollView.worldBound.x, 0);
            }

            currentTabBorders = GetCurrentTabBorders();
            if (firstInit)
            {
                firstInit = false;
                style.display = DisplayStyle.None;
            }
        }

        private void OnTabSelectButtonClick(ClickEvent evt)
        {
            Select((SelectableElement)evt.currentTarget);
            ScrollToTab(tabSelectButtons.IndexOf((SelectableElement)evt.currentTarget));
        }
        
        public void UpdateCurrentTabIndex()
        {
            if (scrollView.scrollOffset.x + scrollView.contentViewport.resolvedStyle.width / 2 <= currentTabBorders.x && currentTabIndex > 0)
            {
                currentTabIndex--;
                Select(tabSelectButtons[currentTabIndex]);
                currentTabBorders = GetCurrentTabBorders();
            }
            else if (scrollView.scrollOffset.x + scrollView.contentViewport.resolvedStyle.width / 2 >= currentTabBorders.y && currentTabIndex < tabs.Count - 1)
            {
                currentTabIndex++;
                Select(tabSelectButtons[currentTabIndex]);
                currentTabBorders = GetCurrentTabBorders();
            }
        }
        
        private float2 GetCurrentTabBorders() => new (tabScrollOffsets[currentTabIndex].x, tabScrollOffsets[currentTabIndex].x + tabs[currentTabIndex].resolvedStyle.width);

        protected override void Select(SelectableElement selectable)
        {
            if(LastSelected != null)
                LastSelected.Q<VisualElement>("Icon").style.unityBackgroundImageTintColor = new StyleColor(Color.white);
            base.Select(selectable);
            selectable.Q<VisualElement>("Icon").style.unityBackgroundImageTintColor = uiHelper.StarsBackgroundColor;
        }

        private void ScrollToTab(int index) => MoveScrollTo(tabScrollOffsets[index], snappingSpeed).Play();
        
        public void SnapToClosest()
        {
            Vector2 closestPoint = GetClosestByX(columnsScrollOffsets, scrollView.scrollOffset.x, out int currentOffsetIndex);
            MoveScrollTo(new Vector2(closestPoint.x - 25, closestPoint.y), snappingSpeed);
            DOVirtual.DelayedCall(snappingSpeed, UpdateCurrentTabIndex);
        }
        
        private Tween MoveScrollTo(Vector2 targetOffset, float duration)
        {
            Vector2 currentScrollOffset = scrollView.scrollOffset;
            return DOTween.To(() => currentScrollOffset,x => currentScrollOffset = x, targetOffset, duration)
                .OnUpdate(() => scrollView.scrollOffset = currentScrollOffset)
                .SetTarget(this);
        }
        
        private Vector2 GetClosestByX(Vector2[] array, float x, out int closestIndex)
        {
            Vector2 closest = array[0];
            closestIndex = 0;
            float smallestDifference = Mathf.Abs(x - array[0].x);

            for (int i = 1; i < array.Length; i++)
            {
                float difference = Mathf.Abs(x - array[i].x);

                if (difference < smallestDifference)
                {
                    smallestDifference = difference;
                    closest = array[i];
                    closestIndex = i;
                }
            }

            return closest;
        }

        public void SetCurrencyType(CurrencyType currencyType)
        {
            if (currencyType == currentCurrency && style.display == DisplayStyle.Flex) return;
            SelectableElement tabButton = null;
            switch (currencyType)
            {
                case CurrencyType.Soft:
                    tabButton = tabSelectButtons.Find(x => x.name == "Soft");
                    break;
                case CurrencyType.Hard:
                    tabButton = tabSelectButtons.Find(x => x.name == "Hard");
                    break;
                case CurrencyType.Scrap:
                    tabButton = tabSelectButtons.Find(x => x.name == "Scrap");
                    break;
                case CurrencyType.Tickets:
                    tabButton = tabSelectButtons.Find(x => x.name == "Tickets");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (tabButton != null)
            {
                Select(tabButton);
                if(style.display == DisplayStyle.Flex)
                    ScrollToTab(tabSelectButtons.IndexOf(tabButton));
                else
                {
                    scrollView.scrollOffset = tabScrollOffsets[tabSelectButtons.IndexOf(tabButton)];
                }
            }
        }

        private void OnGoodsClick(ClickEvent clk)
        {
            GoodsWidget target = (GoodsWidget)clk.currentTarget;
            if(target.IsSold) return;
            string confirmDesc = null;

            switch (target.Item.PurchaseValueType)
            {
                case PurchaseValueType.Crystals:
                    if (target.Item.Price > DataManager.Instance.GameData.HardCurrency)
                    {
                        SetCurrencyType(CurrencyType.Hard);
                        return;
                    }
                    onConfirmAction = () =>
                    {
                        Messenger<GoodsItem>.Broadcast(GameEvents.BuyForCrystals,target.Item,MessengerMode.DONT_REQUIRE_LISTENER);

                        switch (target.Item.CurrencyType)
                        {
                            case CurrencyType.Soft:
                                DataManager.Instance.GameData.BuySoftCurrency(target.Item.Amount, target.Item.Price);
                                PlaySound2D(SoundKey.Menu_shop_credits);
                                break;
                            case CurrencyType.Scrap:
                                DataManager.Instance.GameData.BuyScrap(target.Item.Amount, target.Item.Price);
                                PlaySound2D(SoundKey.Menu_shop_scrap);
                                break;
                            case CurrencyType.Tickets:
                                DataManager.Instance.GameData.BuyTickets(target.Item.Amount, target.Item.Price);
                                PlaySound2D(SoundKey.Menu_shop_ticket);
                                break;
                        }
                    };

                    string translationKey = target.Item.CurrencyType switch
                    {
                        CurrencyType.Soft => "ConfirmWindow/BuySoft_desc",
                        CurrencyType.Scrap => "ConfirmWindow/BuyScrap_desc",
                        CurrencyType.Tickets => "ConfirmWindow/BuyTickets_desc",
                        _ => "",
                    };
                        
                    confirmDesc = LocalizationManager.GetTranslation(translationKey)
                        .Replace("{param1}", target.Item.Amount.ToStringBigValue())
                        .Replace("{param2}", target.Item.Price.ToStringBigValue());
                    
                    confirmWindow.SetUp(target.Item.Icon, confirmDesc, onConfirmAction);
                    confirmWindow.Show();
                    break;
                case PurchaseValueType.Real:
                case PurchaseValueType.Ads:
                    IAPManager.BuyProduct(target.Item.ProductId);
                    break;
            }
        }

        private void OnInfoClick(ClickEvent clk)
        {
            confirmWindow.SetUp(uiHelper.EmptyDirective, LocalizationManager.GetTranslation("Menu/DirectiveChance"), null);
            confirmWindow.Show();
        }

        private void OnCompletePurchase(string id)
        {
            GoodsWidget widget = allGoodsWidgets.Find(x=> x.ProductData.PurchaseId == id);
            if (widget.ProductData.OnePerDay || widget.ProductData.Data.Exists(x => x.Type == PurchaseType.AdsDisabler))
            {
                widget.SetSold();
                widget.parent.RegisterCallback<GeometryChangedEvent>(OnResolve);
                widget.MarkDirtyRepaint();
            }
        }

        private void ShowPurchasedItems(List<WeaponPart> purchasedItems)
        {
            newItemsWindow.ShowPurchasedDirectives(purchasedItems);
        }

        public void UpdateLocalization()
        {
            titleLabel.text = LocalizationManager.GetTranslation("Menu/ShopButton");
            foreach (var widget in allGoodsWidgets)
            {
                uiHelper.SetLocalizationFont(widget);
            }
        }
    }
}