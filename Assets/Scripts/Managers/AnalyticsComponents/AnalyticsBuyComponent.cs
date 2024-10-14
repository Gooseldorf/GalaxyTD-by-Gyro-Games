using UnityEngine;

namespace Data.Managers.AnalyticsComponents
{
    public class AnalyticsBuyComponent : MonoBehaviour
    {
        // [SerializeField] private ConfirmPurchaseShopWindow window;
        //
        // private void Awake()
        // {
        //     window.OnPurchaseComplete += OnPurchaseComplete;
        //     Debug.Log("Awake");
        // }
        //
        // private void OnPurchaseComplete()
        // {
        //     if (window.ShopItem.AssignedShopEntry is ShopEntryItem shopEntryItem)
        //     {
        //         AnalyticsManager.BuyProductEvent(shopEntryItem.Artifact.Name);
        //     }
        //     else
        //     {
        //         AnalyticsManager.BuyProductEvent(window.ShopItem.AssignedShopEntry.ID);
        //     }
        // }
        //
        // private void OnDestroy()
        // {
        //     window.OnPurchaseComplete -= OnPurchaseComplete;
        // }
    }
}