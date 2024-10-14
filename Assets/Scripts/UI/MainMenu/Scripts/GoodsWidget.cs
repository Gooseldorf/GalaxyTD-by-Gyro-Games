using CardTD.Utilities;
using Data.Managers;
using I2.Loc;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static AllEnums;

namespace UI
{
    public class GoodsWidget: PriceButton
    {
        public new class UxmlFactory : UxmlFactory<GoodsWidget>{}
        
        private List<Label> valueLabels;
        private VisualElement questionMark;

        private GoodsItem item;
        private ProductData productData;
        public bool IsSold;
        public GoodsItem Item => item;

        public ProductData ProductData => productData;
        
        public void Init(GoodsItem goodsItem)
        {
            base.Init();
            item = goodsItem;
            
            valueLabels = this.Query<Label>("ValueLabel").ToList();
            
            questionMark = this.Q<VisualElement>("QuestionMark");
            if (questionMark != null)
                UIHelper.Instance.AnimateQuestionMark(questionMark);
            
            int result = goodsItem.Amount;

            //is product available today
            bool available = true;
            var product = IAPManager.GetProduct(item.ProductId);
            if (product != null)
            {
                productData = product;
                available = product.CanBuy;
                
                if (product.TryGetWeaponPart(out var weaponPart))
                {
                    VisualElement directiveIcon = this.Q<VisualElement>("DirectiveIcon");
                    if (directiveIcon != null)
                    {
                        directiveIcon.style.backgroundImage = new StyleBackground(weaponPart.Sprite);
                    }
                }

                for (int i = 0; i < product.Data.Count; i++)
                {
                    if (product.Data[i].Type == PurchaseType.Cash)
                    {
                        valueLabels[i].text = (product.Data[i].Amount * DataManager.Instance.GameData.GetLastMission.Reward.SoftCurrency).ToStringBigValue();
                    }
                    else
                    {
                        valueLabels[i].text = product.Data[i].Amount.ToStringBigValue();
                    }
                }

                if (product.Data.Exists(x => x.Type == PurchaseType.AdsDisabler))
                {
                    valueLabels[0].text = LocalizationManager.GetTranslation("Menu/RemoveAds");
                }
            }
            else
            {
                valueLabels[0].text = item.Amount.ToStringBigValue();
            }
            
            if (!available)
            {
                SetSold();
                return;
            }
            
            switch (goodsItem.PurchaseValueType)
            {
                case PurchaseValueType.Crystals:
                    SetPrice(goodsItem.Price);
                    break;
                case PurchaseValueType.Real:
                    SetRealPrice(goodsItem);
                    break;
                case PurchaseValueType.Ads:
                    break;
            }
        }

        private async void SetRealPrice(GoodsItem item)
        {
            string result = String.Empty;

            while (result == String.Empty)
            {
                result = IAPManager.GetProductCost(item.ProductId);
                await Awaitable.NextFrameAsync();
            }
    
            SetPrice(result);
        }

        public void SetSold()
        {
            if(IsSold) return;
            if (productData.Data.Exists(x => x.Type == PurchaseType.AdsDisabler)) 
                parent.style.display = DisplayStyle.None;
            this.Q<VisualElement>("PriceContainer").style.visibility = Visibility.Hidden;
            VisualElement soldIcon = this.Q<VisualElement>("SoldIcon");
            if (soldIcon != null)
                soldIcon.style.display = DisplayStyle.Flex;
            AddToClassList("noAnimation");
            IsSold = true;
        }
    }
}