using Managers;
using System;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Data.Managers
{
    public class IAPManager : IStoreListener
    {
        private IStoreController Controller;
        private IExtensionProvider extensions;

        private static IAPManager Link;

        public static async void InitServices()
        {
            if (Link == null)
            {
                IAPHelper.Instance.Init();
                try
                {
                    var options = new InitializationOptions();
                    await UnityServices.InitializeAsync(options);
                    InitAIP();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }
        }

        private static void InitAIP()
        {
            Link = new IAPManager();
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            var purchaseIds = IAPHelper.Instance.PurchaseIds;

            foreach (string id in purchaseIds)
                builder.AddProduct(id, ProductType.Consumable);

            // var productCatalog = ProductCatalog.LoadDefaultCatalog();
            //
            // foreach (var product in productCatalog.allProducts)
            // {
            //     builder.AddProduct(product.id, product.type);
            // }

            UnityPurchasing.Initialize(Link, builder);
        }

        public static ProductData GetProduct(string productId) =>IAPHelper.Instance.GetProduct(productId);

        // public static int GetProductAmount(string productId)
        // {
        //     var productCatalog = ProductCatalog.LoadDefaultCatalog();
        //
        //     foreach (var product in productCatalog.allProducts)
        //     {
        //         if (product.id == productId)
        //         {
        //             return (int)product.Payouts[0].quantity;
        //         }
        //     }
        //
        //     return 0;
        // }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            this.Controller = controller;
            this.extensions = extensions;
        }


        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"OnInitializeFailed: {error.ToString()}");
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogError($"OnPurchaseFailed: product:{product.definition} failureReason: {failureReason.ToString()}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            string purchasedItem = purchaseEvent.purchasedProduct.definition.id;

            IAPHelper.Instance.ProcessPurchase(purchasedItem);

            // if (purchasedItem.Contains("ads"))
            //     DataManager.Instance.GameData.SetSkipAds();
            // else
            // {
            //     int amount = GetProductAmount(purchasedItem);
            //     DataManager.Instance.GameData.AddHardCurrency(amount);
            // }

            return PurchaseProcessingResult.Complete;
        }

        public static bool IsInitialized => Link != null && Link.Controller != null;

        public static bool CanBuyProduct(string id) => IsInitialized && IAPHelper.Instance.CanBuyProduct(id);
        
        /// <summary>
        /// buy product
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="onShowReward">for ads show - hide button on show reward</param>
        public static void BuyProduct(string productId,Action onShowReward=null)
        {
            if (IsInitialized && IAPHelper.Instance.CanBuyProduct(productId))
            {
                var product = GetProduct(productId);

                if (product.CostType == Cost.Ads)
                {
                    AdsManager.LoadReward(AdsRewardType.BuyDirective);

                    AdsManager.TryShowReward(onShowReward, () =>
                    {
                        IAPHelper.Instance.ProcessPurchase(productId);
                    });
                    return;
                }
                Link.Controller.InitiatePurchase(productId);
            }
            else
                WriteNotInitialized();
        }

        public static string GetProductCost(string productId)
        {
            if (IsInitialized)
            {
                foreach (var product in Link.Controller.products.all)
                {
                    if (product.definition.id == productId)
                    {
                        return product.metadata.localizedPriceString;
                    }
                }
            }

            return string.Empty;
        }

        public static void WriteNotInitialized() => Debug.LogError("Is not initialized");

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError("On Initialize failed");
        }
    }
}