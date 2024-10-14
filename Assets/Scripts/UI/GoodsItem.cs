using Data.Managers;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using static AllEnums;

namespace UI
{
    [Serializable]
    public class GoodsItem
    {
        public PurchaseValueType PurchaseValueType = PurchaseValueType.Crystals;
        public CurrencyType CurrencyType;
        [ShowIf("IsHard")] public string ProductId;
        [ShowIf("IsForCrystal")][SerializeField] private int amount;
        [ShowIf("IsForCrystal")] public int Price;
        public Texture2D Icon;

        public bool IsHard() => PurchaseValueType is PurchaseValueType.Real or PurchaseValueType.Ads;
        public bool IsForCrystal() => PurchaseValueType is PurchaseValueType.Crystals;

        public int Amount
        {
            get
            {
                if (CurrencyType == CurrencyType.Soft)
                    return amount * DataManager.Instance.GameData.GetLastMission.Reward.SoftCurrency;
                if (CurrencyType == CurrencyType.Scrap)
                    return amount * DataManager.Instance.GameData.GetLastMission.Reward.Scrap;
                return amount;
            }
        }
        public GoodsItem GetItem()
        {
            GoodsItem item = new(Price, amount, CurrencyType, ProductId, Icon,PurchaseValueType);
            return item;
        }

        public GoodsItem(int price, int amount, CurrencyType type, string productId, Texture2D icon,PurchaseValueType purchaseType)
        {
            PurchaseValueType = purchaseType;
            Icon = icon;
            ProductId = productId;
            CurrencyType = type;

            if (PurchaseValueType != PurchaseValueType.Crystals)
            {
                //all data
                ProductData productData = IAPManager.GetProduct(ProductId);

                Price = 0;
                this.amount = -1;
                return;
            }

            Price = price;
            this.amount = amount;
        }
    }
}