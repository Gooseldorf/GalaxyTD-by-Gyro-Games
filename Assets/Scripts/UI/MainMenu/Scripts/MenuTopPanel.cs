using CardTD.Utilities;
using Data.Managers;
using I2.Loc;
using Managers;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using static AllEnums;

namespace UI
{
    public class MenuTopPanel : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<MenuTopPanel>
        {
        }

        private const string AdsTimerName = "AdsTimer";

        private CurrencyWidget softCurrencyWidget;
        private CurrencyWidget scrapCurrencyWidget;
        private CurrencyWidget hardCurrencyWidget;
        private PriceButton adButton;
        private VisualElement adsIcon;
        private ConfirmWindow confirmWindow;
        private int adsReward = 100;
        private const float asdMissionRewardFactor = 2;

        public event Action<CurrencyType> OnCurrencyClick;
        private ADSTimerCoroutine timer;

        public void Init(ConfirmWindow confirmWindow)
        {
            this.confirmWindow = confirmWindow;
            adButton = this.Q<PriceButton>("AdButton");
            adsIcon = adButton.Q<VisualElement>("AdsIcon");

            adButton.Init();
            adButton.RegisterCallback<ClickEvent>(OnAdClick);
            adButton.visible = false;

            softCurrencyWidget = this.Q<CurrencyWidget>("SoftCurrencyWidget");
            softCurrencyWidget.Init(CurrencyType.Soft);
            softCurrencyWidget.RegisterCallback<ClickEvent>(OnCurrencyWidgetClick);

            hardCurrencyWidget = this.Q<CurrencyWidget>("HardCurrencyWidget");
            hardCurrencyWidget.Init(CurrencyType.Hard);
            hardCurrencyWidget.RegisterCallback<ClickEvent>(OnCurrencyWidgetClick);

            scrapCurrencyWidget = this.Q<CurrencyWidget>("ScrapCurrencyWidget");
            scrapCurrencyWidget.Init(CurrencyType.Scrap);
            scrapCurrencyWidget.RegisterCallback<ClickEvent>(OnCurrencyWidgetClick);

            DataManager.Instance.GameData.CurrencyUpdated += UpdateCurrency;

            UpdateCurrency();
            UpdateAdsReward();
        }

        public void Dispose()
        {
            if (timer != null)
            {
                timer.OnComplete -= OnAdsTimerComplete;
                timer.OnAdsReady -= OnAdsReady;
                timer.StopCoroutine();
            }

            softCurrencyWidget.UnregisterCallback<ClickEvent>(OnCurrencyWidgetClick);
            hardCurrencyWidget.UnregisterCallback<ClickEvent>(OnCurrencyWidgetClick);
            scrapCurrencyWidget.UnregisterCallback<ClickEvent>(OnCurrencyWidgetClick);
            adButton.UnregisterCallback<ClickEvent>(OnAdClick);

            DataManager.Instance.GameData.CurrencyUpdated -= UpdateCurrency;
        }

        public void OnPanelsUpdated(int countPanels)
        {
            EnableAdButton(countPanels <= 0);
        }

        private void OnAdClick(ClickEvent clk)
        {
            if (DataManager.Instance.GameData.SkipAds)
            {
                TryShowAds();
                return;
            }

            AdsManager.LoadReward(AdsRewardType.MainMenu);
            //TODO: Calculate reward
            confirmWindow.SetUp((Texture2D)null,
                LocalizationManager.GetTranslation("ConfirmWindow/WatchAD_desc").Replace("{param}", $"{adsReward.ToStringBigValue()}"),
                () =>
                {
                    TryShowAds();
                });
            confirmWindow.Show();
        }

        private void TryShowAds()
        {
            AdsManager.TryShowReward(
                () =>
                {
                    adButton.visible = false;
                    adsIcon.visible = false;
                    confirmWindow.Hide();
                },
                () =>
                {
                    Messenger<AdsRewardType, int>.Broadcast(GameEvents.ShowAdsReward, AdsRewardType.MainMenu, adsReward, MessengerMode.DONT_REQUIRE_LISTENER);
                    DataManager.Instance.GameData.BuySoftCurrency(adsReward, 0);
                }
            );
            AdsMainMenuHelper.UpdateTime();
            timer.SetTime(AdsMainMenuHelper.GetTime);
        }

        private void UpdateCurrency()
        {
            softCurrencyWidget.UpdateValue(DataManager.Instance.GameData.SoftCurrency);
            hardCurrencyWidget.UpdateValue(DataManager.Instance.GameData.HardCurrency);
            scrapCurrencyWidget.UpdateValue(DataManager.Instance.GameData.Scrap);
        }

        private void UpdateAdsReward()
        {
            int completedMissionsCount = DataManager.Instance.GameData.Stars.Count;

            if (!AdsMainMenuHelper.CanShow(completedMissionsCount))
            {
                //Debug.Log($"Completed missions count: {completedMissionsCount} Ad button is hidden");
                return;
            }

            adsReward = (int)(DataManager.Instance.GameData.GetLastMission.Reward.SoftCurrency * asdMissionRewardFactor);
            adButton.SetPrice(adsReward);

            GameObject timerObject = new() {name = AdsTimerName};
            timer = timerObject.AddComponent<ADSTimerCoroutine>();
            timer.OnComplete += OnAdsTimerComplete;
            timer.OnAdsReady += OnAdsReady;
            // Debug.Log($"set time {AdsMainMenuHelper.GetTime}");
            timer.SetTime(AdsMainMenuHelper.GetTime);
        }

        private void OnAdsTimerComplete()
        {
            if (DataManager.Instance.GameData.SkipAds)
            {
                OnAdsReady();
                return;
            }

            AdsManager.LoadReward(AdsRewardType.MainMenu);
            timer.AdsCoroutine();
        }

        private void OnAdsReady()
        {
            // adsIcon.style.visibility = DataManager.Instance.GameData.SkipAds ? Visibility.Hidden : Visibility.Visible;
            adsIcon.visible = !DataManager.Instance.GameData.SkipAds;
            adButton.visible = true;
        }

        private void EnableAdButton(bool enable)
        {
            adButton.style.opacity = enable ? 1f : 0.05f;
            adButton.pickingMode = enable ? PickingMode.Position : PickingMode.Ignore;
        }

        private void OnCurrencyWidgetClick(ClickEvent clk)
        {
            CurrencyWidget target = (CurrencyWidget)clk.currentTarget;
            OnCurrencyClick?.Invoke(target.CurrencyType);
        }
    }

    public class ADSTimerCoroutine : MonoBehaviour
    {
        [ReadOnly, ShowInInspector] private long endTime;
        [ReadOnly, ShowInInspector] private long CurentTime => DateTime.Now.Ticks;

        private bool isWork = false;

        private bool adsReady = false;

        public Action OnComplete;

        public Action OnAdsReady;

        public void SetTime(long time)
        {
            endTime = time;
            isWork = true;
        }

        private void Update()
        {
            if (endTime != 0)
            {
                // Debug.Log($"endTime {endTime}  tick {DateTime.Now.Ticks}");
                if (endTime < DateTime.Now.Ticks)
                {
                    OnComplete?.Invoke();
                    endTime = 0;
                }
            }
        }

        public void AdsCoroutine()
        {
            StartCoroutine(AdsCoroutineEnumerator());
        }

        private IEnumerator AdsCoroutineEnumerator()
        {
            // Debug.Log("AdsCoroutineEnumerator");
            while (!AdsManager.RewardIsReadyToShow)
            {
                // Debug.Log("!RewardIsReadyToShow");
                yield return new WaitForSecondsRealtime(.1f);
            }

            // Debug.Log("AdsCoroutineEnumerator complete");
            yield return null;
            OnAdsReady?.Invoke();
        }

        public void StopCoroutine()
        {
            StopAllCoroutines();
        }
    }

    public static class AdsMainMenuHelper
    {
        private const string prefKey = "AdsTime";
        private const long timeBetweenAdsInSeconds = 1200;
        private const int countMissionsToShow = 2;

        public static long GetTime => PlayerPrefs.HasKey(prefKey) ? long.Parse(PlayerPrefs.GetString(prefKey)) : DateTime.Now.Ticks;

        public static void UpdateTime()
        {
            long currentTime = DateTime.Now.Ticks + timeBetweenAdsInSeconds * TimeSpan.TicksPerSecond;
            PlayerPrefs.SetString(prefKey, $"{currentTime}");
            PlayerPrefs.Save();
        }

        public static bool CanShow(int countMissionComplete) => countMissionComplete >= countMissionsToShow;
    }
}