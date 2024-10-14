using CardTD.Utilities;
using Data.Managers;
using GoogleMobileAds.Api;
using I2.Loc;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Managers
{
    public enum AdsRewardType { Test = 0, MainMenu = 1, IncreaseReward = 2, SecondChance = 3, GetTicket = 4, BuyDirective = 5 }

    public class AdsManager
    {
        private const string tag = nameof(AdsManager);

        private const string mainMainMenuAdsKey = "ca-app-pub-5223567023415499/6247147830";
        private const string increaseRewardAdsKey = "ca-app-pub-5223567023415499/6318713293";
        private const string secondChanceAdsKey = "ca-app-pub-5223567023415499/9009042548";
        private const string ticketsAdsKey = "ca-app-pub-5223567023415499/2628548598";
        private const string buyDirective = "ca-app-pub-5223567023415499/9328020011";

        private static AdsManager link;
        private InitializationStatus status;

        private RewardedAd rewardedAd;
        private AdsRewardType currentRewardType = AdsRewardType.Test;

        public static void InitServices()
        {
            if (link != null) return;
            if (DataManager.Instance.GameData.SkipAds) return;
            link = new();
            MobileAdsEventExecutor.Initialize();
            MobileAds.Initialize(link.OnInitializationComplete);
        }

        private static bool isAdsInitialised => link is {status: not null};
        private static bool isAdsLoaded;
        private static bool isStartPreLoaded;

        public static void LoadReward(AdsRewardType rewardType)
        {
#if UNITY_STANDALONE
            return;
#endif
            if (DataManager.Instance.GameData.SkipAds) return;
            try
            {
                if (isAdsInitialised)
                {
                    link.currentRewardType = rewardType;

                    if (isStartPreLoaded)
                        return;

                    if (isAdsLoaded)
                        return;

                    string rewardId = (rewardType) switch
                    {
                        AdsRewardType.MainMenu => mainMainMenuAdsKey,
                        AdsRewardType.IncreaseReward => increaseRewardAdsKey,
                        AdsRewardType.SecondChance => secondChanceAdsKey,
                        AdsRewardType.GetTicket => ticketsAdsKey,
                        AdsRewardType.BuyDirective => buyDirective,
                        _ => mainMainMenuAdsKey
                    };

#if DEVELOPMENT_BUILD
                rewardId = "ca-app-pub-3940256099942544/5224354917";
#endif
                    link.InitReward(rewardId);
                }
                else
                {
                    Debug.Log("Ads is not initialised");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception on load reward: {ex}");
            }
        }

        public static bool RewardIsReadyToShow => link is {rewardedAd: not null} && link.rewardedAd.CanShowAd();

        private static void ShowReward(Action onShowAds, Action rewarded)
        {
            try
            {
                if(link==null || link.rewardedAd==null || !link.rewardedAd.CanShowAd())
                    return;
                
                onShowAds?.Invoke();
                MobileAdsEventExecutor.IsActive = false;
                MobileAdsEventExecutor.SetAction(rewarded);
                
                link.rewardedAd.Show(_ =>
                {
                    isAdsLoaded = false;
                    MobileAdsEventExecutor.IsActive = true;
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception on show reward: {ex}");
            }
        }

        public static void TryShowReward(Action onShowAds, Action onRewarded)
        {
#if UNITY_STANDALONE
            return;
#endif
            try
            {
                if (DataManager.Instance.GameData.SkipAds)
                {
                    onShowAds?.Invoke();
                    onRewarded?.Invoke();
                    return;
                }

                if (RewardIsReadyToShow)
                    ShowReward(onShowAds, onRewarded);
                else
                    ShowToastRewardIsNotReady();
            }
            catch (Exception ex)
            {
                Debug.LogError($"ex {ex}");
            }
        }

        private static void ShowToastRewardIsNotReady()
        {
            Messenger<string, float2>.Broadcast(UIEvents.ShowNotification, LocalizationManager.GetTranslation("ConfirmWindow/AdNotReady"), new float2(100, 100));

            if (isStartPreLoaded)
                return;

            AdsRewardType rewardType = AdsRewardType.GetTicket;
            if (link != null)
                rewardType = link.currentRewardType;
            LoadReward(rewardType);
        }

        private void InitReward(string id)
        {
            if (DataManager.Instance.GameData.SkipAds) return;

            isStartPreLoaded = true;

            try
            {
                isAdsLoaded = false;
                rewardedAd = null; // new RewardedAd(id);
                AdRequest adRequest = new();

                RewardedAd.Load(id, adRequest,
                    (ad, error) =>
                    {
                        isStartPreLoaded = false;

                        if (error != null || ad == null)
                        {
                            isAdsLoaded = false;
                            return;
                        }

                        isAdsLoaded = true;

                        rewardedAd = ad;
                    });
            }
            catch (Exception ex)
            {
                isStartPreLoaded = false;
                Debug.LogError($"Exception on init reward: {ex}");
            }
        }


        private void OnInitializationComplete(InitializationStatus stat)
        {
            this.status = stat;
        }
    }
}