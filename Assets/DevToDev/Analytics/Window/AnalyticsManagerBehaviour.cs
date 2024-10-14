using CardTD.Utilities;
using Managers;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevToDev.Analytics.Window
{
    public class AnalyticsManagerBehaviour : MonoBehaviour
    {
        private static AnalyticsManager link;
        private bool showLog;

        private void Awake()
        {
            if (link != null)
            {
                Destroy(this.gameObject);
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            showLog = true;
#endif


// #if UNITY_ANDROID
            link = new AnalyticsManager(DataManager.Instance.GameData, showLog);
            DontDestroyOnLoad(this.gameObject);
// #endif
        }
    }

    public class AnalyticsManager
    {
        private const string androidKey = "8b459400-8ff3-09f7-82fe-8dddfa6d133f";

        private const string missionIndexKey = "Mission Index";

        private const string completedMissionEvent = "Completed mission";
        private const string replayingMissionsEvent = "Replaying missions";

        private readonly GameData gameData;
        private readonly MissionList missionList;

        private Dictionary<string, Dictionary<string, int>> saveDirectives = new();
        private Dictionary<string, List<string>> saveParts = new();
        private Dictionary<string, int> buildTowers = new();

        private readonly bool showLogs;

        public AnalyticsManager(GameData data, bool logs)
        {
            Debug.Log("AnalyticsManager");
            showLogs = logs;
            gameData = data;

            DTDAnalyticsConfiguration config = new() {LogLevel = DTDLogLevel.No, CurrentLevel = gameData.Stars.Count};
            DTDAnalytics.Initialize(androidKey, config);

            missionList = DataManager.Instance.Get<MissionList>();
            Messenger<int, int>.AddListener(GameEvents.Win, OnWin);
            Messenger<WeaponPart>.AddListener(GameEvents.BuyPart, OnBuyPart);
            Messenger<int, int>.AddListener(GameEvents.BuySoft, OnBuySoft);
            Messenger<bool>.AddListener(GameEvents.ShowLoseWindow, OnShowLose);
            Messenger<WeaponPart>.AddListener(GameEvents.BuyWeaponPart, OnBuyWeaponPart);
            Messenger<GoodsItem>.AddListener(GameEvents.BuyForCrystals, OnBuyForCrystals);
            Messenger<AdsRewardType, int>.AddListener(GameEvents.ShowAdsReward, OnShowAdsReward);
            Messenger.AddListener(GameEvents.TryStartMission, OnTryStartMission);
            Messenger<IMenuPanel>.AddListener(GameEvents.HidePanel, OnHidePanel);
            Messenger<IMenuPanel>.AddListener(GameEvents.ShowPanel, OnShowPanel);
            Messenger.AddListener(GameEvents.Restart, OnRestartGame);
            Messenger.AddListener(GameEvents.InitGame, OnRestartGame);
            Messenger<string>.AddListener(GameEvents.BuildTowerTowerId, OnTowerBuild);
        }

        ~AnalyticsManager()
        {
            Messenger<int, int>.RemoveListener(GameEvents.Win, OnWin);
            Messenger<WeaponPart>.RemoveListener(GameEvents.BuyPart, OnBuyPart);
            Messenger<int, int>.RemoveListener(GameEvents.BuySoft, OnBuySoft);
            Messenger<bool>.RemoveListener(GameEvents.ShowLoseWindow, OnShowLose);
            Messenger<WeaponPart>.RemoveListener(GameEvents.BuyWeaponPart, OnBuyWeaponPart);
            Messenger<GoodsItem>.RemoveListener(GameEvents.BuyForCrystals, OnBuyForCrystals);
            Messenger<AdsRewardType, int>.RemoveListener(GameEvents.ShowAdsReward, OnShowAdsReward);
            Messenger.RemoveListener(GameEvents.TryStartMission, OnTryStartMission);
            Messenger<IMenuPanel>.RemoveListener(GameEvents.HidePanel, OnHidePanel);
            Messenger<IMenuPanel>.RemoveListener(GameEvents.ShowPanel, OnShowPanel);
            Messenger.RemoveListener(GameEvents.Restart, OnRestartGame);
            Messenger.RemoveListener(GameEvents.InitGame, OnRestartGame);
            Messenger<string>.RemoveListener(GameEvents.BuildTowerTowerId, OnTowerBuild);
        }

        private void OnTowerBuild(string towerId)
        {
            if (!buildTowers.TryAdd(towerId, 1))
            {
                buildTowers[towerId] += 1;
            }
        }

        private void OnRestartGame()
        {
            buildTowers.Clear();
        }

        private void OnShowPanel(IMenuPanel iPanel)
        {
            if (iPanel == null || iPanel is not VisualElement panel)
                return;

            if (panel.name == "WorkshopPanel")
            {
                saveDirectives.Clear();
                saveParts.Clear();

                foreach (TowerFactory factory in gameData.Factories)
                {
                    string towerName = $"{factory.TowerId}";
                    saveDirectives.Add(towerName, GetDirectives(factory));
                    saveParts.Add(towerName, GetParts(factory));
                }
            }
        }

        private Dictionary<string, int> GetDirectives(TowerFactory factory)
        {
            Dictionary<string, int> tmp = new();
            foreach (ISlot directive in factory.Directives)
            {
                if (directive != null && directive.WeaponPart != null)
                {
                    string name = directive.WeaponPart.name;
                    if (!tmp.ContainsKey(name))
                    {
                        tmp.Add(name, 0);
                    }

                    tmp[name] += 1;
                }
            }

            return tmp;
        }

        private List<string> GetParts(TowerFactory factory)
        {
            List<string> tmp = new();
            foreach (ISlot part in factory.Parts)
            {
                if (part != null && part.WeaponPart != null)
                    tmp.Add(part.WeaponPart.name);
            }

            return tmp;
        }

        private void OnHidePanel(IMenuPanel iPanel)
        {
            if (iPanel == null || iPanel is not VisualElement panel)
                return;

            if (panel.name == "WorkshopPanel")
            {
                if (saveDirectives.Count == 0 || saveParts.Count == 0)
                    return;

                foreach (TowerFactory factory in gameData.Factories)
                {
                    string factoryName = $"{factory.TowerId}";

                    if (saveParts.ContainsKey(factoryName))
                    {
                        List<string> parts = GetParts(factory);
                        foreach (string part in parts)
                        {
                            if (!saveParts[factoryName].Contains(part))
                            {
                                DTDCustomEventParameters parameters = new();
                                parameters.Add("Part name", part);
                                DTDAnalytics.CustomEvent("Set Part", parameters);
                            }
                        }
                    }

                    if (saveDirectives.ContainsKey(factoryName))
                    {
                        Dictionary<string, int> directives = GetDirectives(factory);

                        foreach (KeyValuePair<string, int> pDirective in directives)
                        {
                            if (!saveDirectives[factoryName].ContainsKey(pDirective.Key) || saveDirectives[factoryName][pDirective.Key] < pDirective.Value)
                            {
                                int oldValue = saveDirectives[factoryName].ContainsKey(pDirective.Key) ? saveDirectives[factoryName][pDirective.Key] : 0;

                                for (int i = 0; i < pDirective.Value - oldValue; i++)
                                {
                                    DTDCustomEventParameters parameters = new();
                                    parameters.Add("Directive name", pDirective.Key);
                                    DTDAnalytics.CustomEvent("Set directive", parameters);
                                }
                            }
                        }
                    }
                }

                saveDirectives.Clear();
                saveParts.Clear();
            }
        }


        private void OnTryStartMission()
        {
            int missionIndex = GameServices.Instance.CurrentMission.MissionIndex + 1;

            string eventKey = GameServices.Instance.IsHard ? "FirstTryHardMission" : "FirstTryMission";
            int lastMissionIndex = PlayerPrefs.GetInt(eventKey, 0);

            if (missionIndex <= lastMissionIndex) return;

            DTDCustomEventParameters parameters = new();
            parameters.Add(missionIndexKey, missionIndex);
            DTDAnalytics.CustomEvent(eventKey, parameters);

            PlayerPrefs.SetInt(eventKey, missionIndex);
            PlayerPrefs.Save();
        }

        private void OnShowLose(bool hasSecondChance)
        {
            DTDCustomEventParameters parameters = new();
            parameters.Add("Hase second chance", hasSecondChance);
            parameters.Add("Is hard", GameServices.Instance.IsHard);
            int missionIndex = GameServices.Instance.CurrentMission.MissionIndex + 1;
            parameters.Add(missionIndexKey, missionIndex);
            DTDAnalytics.CustomEvent(eventName: "Show Lose Window");
        }

        private void OnBuySoft(int amount, int price)
        {
            DTDCustomEventParameters parameters = new();
            parameters.Add("Amount", amount);
            parameters.Add("Price", price);
            DTDAnalytics.CustomEvent("Buy soft", parameters);
        }

        private void OnBuyPart(WeaponPart directive)
        {
            DTDCustomEventParameters parameters = new();
            parameters.Add("Name", directive.name);
            DTDAnalytics.CustomEvent("Buy directive", parameters);

            DTDCustomEventParameters countParameters = new();
            int count = PlayerPrefs.GetInt(nameof(countParameters), 1);
            countParameters.Add("Count", count);
            count++;
            DTDAnalytics.CustomEvent("Count buy directive", countParameters);

            PlayerPrefs.SetInt(nameof(countParameters), count);
            PlayerPrefs.Save();
        }

        private void OnShowAdsReward(AdsRewardType rewardType, int value)
        {
            DTDCustomEventParameters parameters = new();
            parameters.Add("Value", value);
            DTDAnalytics.CustomEvent($"Show ads {rewardType}", parameters);
        }

        private void OnBuyForCrystals(GoodsItem item)
        {
            DTDCustomEventParameters parameters = new();
            parameters.Add("Type", $"{item.CurrencyType}");
            parameters.Add("Amount", $"{item.Amount}");
            parameters.Add("Price", $"{item.Price}");
            DTDAnalytics.CustomEvent("Buy for crystals", parameters);
        }

        private void OnBuyWeaponPart(WeaponPart weaponPart)
        {
            DTDCustomEventParameters weaponCountParameters = new();
            int count = PlayerPrefs.GetInt(nameof(weaponCountParameters), 1);
            weaponCountParameters.Add("Count", count);
            count++;
            DTDAnalytics.CustomEvent("Count buy weapon parts", weaponCountParameters);

            PlayerPrefs.SetInt(nameof(weaponCountParameters), count);
            PlayerPrefs.Save();
        }

        private void OnWin(int i, int j)
        {
            Mission mission = GameServices.Instance.CurrentMission;
            bool isHard = missionList.IsHardMission(mission);
            int missionIndex = mission.MissionIndex + 1;

            int countMissionsComplete = isHard ? gameData.HardStars.Count : gameData.Stars.Count;

            DTDCustomEventParameters parameters = new();
            parameters.Add(missionIndexKey, missionIndex);
            parameters.Add("Is Hard", isHard);
            DTDAnalytics.CustomEvent(countMissionsComplete > mission.MissionIndex ? replayingMissionsEvent : completedMissionEvent, parameters);

            if (isHard)
            {
                DTDCustomEventParameters hardParameters = new();
                hardParameters.Add(missionIndexKey, missionIndex);
                DTDAnalytics.CustomEvent("Completed hard mission", hardParameters);
            }

            string towerId = "";
            int count = 0;

            foreach (KeyValuePair<string, int> towerData in buildTowers)
            {
                if (towerData.Value > count)
                {
                    towerId = towerData.Key;
                    count = towerData.Value;
                }
            }

            if (count > 0)
            {
                Log($"tower id {towerId} count {count}");
                DTDCustomEventParameters towersParameters = new();
                towersParameters.Add("Tower id:", towerId);
                towersParameters.Add("Count ", count);
                DTDAnalytics.CustomEvent("Most built", towersParameters);
            }
        }

        private void Log(string message)
        {
            if (showLogs)
                Debug.Log(message);
        }
    }
}