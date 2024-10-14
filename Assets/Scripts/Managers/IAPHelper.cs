using CardTD.Utilities;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UI;
using UnityEngine;
using static MusicManager;
using Random = UnityEngine.Random;

namespace Data.Managers
{
    public enum PurchaseType { Cash = 0, Crystals = 1, DirectiveForSmallPrice = 2, DirectiveForBigPrice = 3, Directive = 4, AdsDisabler = 5 }

    public enum Cost { Real = 1, Ads = 2 }

    public class IAPHelper : ScriptableObjSingleton<IAPHelper>
    {
        [SerializeField] private List<ProductData> products = new();
        [SerializeField] private List<WeaponPart> directivesForSmallCrystals = new();
        [SerializeField] private List<WeaponPart> directivesForBigCrystals = new();
        public WeaponPart GetSmallDirective => directivesForSmallCrystals[PlayerPrefs.GetInt(nameof(GetSmallDirective), 0)];
        public WeaponPart GetBigDirective => directivesForBigCrystals[PlayerPrefs.GetInt(nameof(GetBigDirective), 0)];

        
        public void Init()
        {
            if((GetLastUpdateDay-DateTime.Now).Days==0)
                return;

            SetSmallDirectiveIndex(Random.Range(0, directivesForSmallCrystals.Count));
            SetBigDirectiveIndex(Random.Range(0, directivesForBigCrystals.Count));
            
            UpdateLastDay();
        }
        
        public void SetSmallDirectiveIndex(int index)
        {
            PlayerPrefs.SetInt(nameof(GetSmallDirective), index);
            PlayerPrefs.Save();
        }

        public void SetBigDirectiveIndex(int index)
        {
            PlayerPrefs.SetInt(nameof(GetBigDirective), index);
            PlayerPrefs.Save();
        }

        public List<string> PurchaseIds
        {
            get
            {
                List<string> result = new();
                foreach (var product in products)
                    result.Add(product.PurchaseId);
                return result;
            }
        }

        public bool IsSpecialProduct(string id) => products.Exists((product) => product.PurchaseId == id);

        public bool CanBuyProduct(string id)
        {
            foreach (ProductData product in products)
                if (product.PurchaseId == id)
                    return CanBuyProduct(product);

            throw new Exception($"Do not have id {id}");
        }

        public bool CanBuyProduct(ProductData product)
        {
            if (product.Data.Exists((item) => item.Type == PurchaseType.AdsDisabler) && DataManager.Instance.GameData.SkipAds)
                return false;
            return !product.OnePerDay || ((DateTime.Now - GetPurchaseDay(product.PurchaseId).Date).Days > 0);
        }

        public ProductData GetProduct(string id) => products.Find((item) => item.PurchaseId == id);

        public void ProcessPurchase(string id)
        {
            ProductData product = GetProduct(id);

            if (product == null)
                throw new Exception($"purchase do not found {id}");

            foreach (ProductPart productPart in product.Data)
            {
                switch (productPart.Type)
                {
                    case PurchaseType.Cash:
                        DataManager.Instance.GameData.AddSoftCurrency(DataManager.Instance.GameData.GetLastMission.Reward.SoftCurrency * productPart.Amount);
                        PlaySound2D(SoundKey.Menu_shop_credits);
                        break;
                    case PurchaseType.Crystals:
                        DataManager.Instance.GameData.AddHardCurrency(productPart.Amount);
                        PlaySound2D(productPart.Amount < 1000 ? SoundKey.Menu_shop_crystals : SoundKey.Menu_shop_crystals_case);
                        break;
                    case PurchaseType.DirectiveForSmallPrice:
                        AddDirectives(new List<WeaponPart> {GetSmallDirective}, productPart.Amount);
                        break;
                    case PurchaseType.DirectiveForBigPrice:
                        AddDirectives(new List<WeaponPart> {GetBigDirective}, productPart.Amount);
                        break;
                    case PurchaseType.Directive:
                        AddDirectives(DataManager.Instance.Get<PartsHolder>().Directives, productPart.Amount);
                        break;
                    case PurchaseType.AdsDisabler:
                        DataManager.Instance.GameData.SetSkipAds();
                        PlaySound2D(SoundKey.Menu_shop_ad_remove);
                        break;
                }
            }
            SetPurchaseDay(id, DateTime.Now);
            Messenger<string>.Broadcast(UIEvents.PurchaseCompleted, id, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void AddDirectives(List<WeaponPart> parts, int count = 1)
        {
            List<WeaponPart> receivedParts = new();
            for (int i = 0; i < count; i++)
            {
                int randIndex = Random.Range(0, parts.Count);
                DataManager.Instance.GameData.Inventory.AddWeaponPart(parts[randIndex]);
                receivedParts.Add(parts[randIndex]);
            }
            DataManager.Instance.GameData.NewItems.AddRange(receivedParts);
            Messenger.Broadcast(UIEvents.OnNewItemsUpdated, MessengerMode.DONT_REQUIRE_LISTENER);
            Messenger<List<WeaponPart>>.Broadcast(UIEvents.PurchaseWeaponPartsCompleted, receivedParts, MessengerMode.DONT_REQUIRE_LISTENER);
            PlaySound2D(SoundKey.Menu_shop_bundle);
        }

        private DateTime GetLastUpdateDay => new(long.Parse(PlayerPrefs.GetString(nameof(GetLastUpdateDay), "0")));

        public void UpdateLastDay()
        {
            PlayerPrefs.SetString(nameof(GetLastUpdateDay), $"{DateTime.Now.Ticks}");
            PlayerPrefs.Save();
        }
        private DateTime GetPurchaseDay(string id) => new(long.Parse(PlayerPrefs.GetString(id, "0")));

        private void SetPurchaseDay(string id, DateTime date)
        {
            PlayerPrefs.SetString(id, $"{date.Ticks}");
            PlayerPrefs.Save();
        }

        public bool HasOncePerDayItems() 
        {
            foreach (ProductData product in products)
            {
                if (product.OnePerDay && CanBuyProduct(product))
                {
                    if(product.Data.Exists((item) => item.Type == PurchaseType.AdsDisabler))
                        return false;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class ProductData
    {
        public string PurchaseId;
        public List<ProductPart> Data = new();
        [FoldoutGroup("dop")] public bool OnePerDay;
        [FoldoutGroup("dop")] public Cost CostType = Cost.Real;

        public bool CanBuy => IAPHelper.Instance.CanBuyProduct(this);

        public bool TryGetWeaponPart(out WeaponPart weaponPart)
        {
            weaponPart = default;
            foreach (var productPart in Data)
            {
                if (productPart.Type == PurchaseType.DirectiveForSmallPrice)
                {
                    weaponPart = IAPHelper.Instance.GetSmallDirective;
                    return true;
                }

                if (productPart.Type == PurchaseType.DirectiveForBigPrice)
                {
                    weaponPart = IAPHelper.Instance.GetBigDirective;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class ProductPart
    {
        public PurchaseType Type = PurchaseType.Cash;
        public int Amount = 1;
    }
}