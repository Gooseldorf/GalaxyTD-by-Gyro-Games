using CardTD.Utilities;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UI;
using Unity.Mathematics;
using UnityEngine;

public class UnlockManager : SerializedScriptableObject
{
    [Tooltip("Уровень на котором будет активироваться механика траты потрон на перезярядку")]
    [SerializeField] private int reloadCostUnlockLevel = 10;
    [OdinSerialize, ShowInInspector] private Dictionary<AllEnums.TowerId, int> towerUnlockDictionary;

    [OdinSerialize, ShowInInspector]
    [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.OneLine, KeyLabel = "Mission Index", ValueLabel = "Rewards & Unlocks")]
    private Dictionary<int, MissionRewardAndUnlocks> missionRewardsAndUnlocks;

    [OdinSerialize, ShowInInspector]
    private List<WeaponPart> startItems;

    private List<WeaponPart> sortedDirectives = new();

    public bool IsUseReloadCostMechanic(int countMissions) => reloadCostUnlockLevel <= countMissions;

    public List<WeaponPart> GetRewardsForChoose(int missionIndex) => missionRewardsAndUnlocks[missionIndex].RewardForChoose;

    [OdinSerialize, BoxGroup("UI Unlock missions count")]
    public int AmmoUnlockMission = 10;
    [OdinSerialize, BoxGroup("UI Unlock missions count")]
    public int DirectivesUnlockMission =4;
    [OdinSerialize, BoxGroup("UI Unlock missions count")]
    public int PartsUnlockMission = 15;

    public int GetPartUnlockMission(WeaponPart part)
    {
        if (startItems.Contains(part))
            return -1;

        foreach (var missionReward in missionRewardsAndUnlocks)
        {
            if (missionReward.Value.RewardForChoose.Contains(part) ||
                (missionReward.Value.PartToUnlock != null && missionReward.Value.PartToUnlock.Contains(part)))
                return missionReward.Key;
        }

        Debug.LogError($"{part.SerializedID} is not in unlocker. Unlock mission index is set to 99");
        return 99;
        //throw new Exception($"{part.name} doesn't present in unlocker, just add");
    }

    public bool IsPartUnlocked(WeaponPart part)
    {
        if (DataManager.Instance.GameData.Stars == null)
        {
            Debug.LogError("Starts is null");
            return false;
        }

        int missionNum = GetPartUnlockMission(part);

        if (missionNum == -1)
            return true;

        return DataManager.Instance.GameData.Stars.ContainsKey(missionNum);
    }

    public bool IsTowerUnlocked(AllEnums.TowerId towerId) => DataManager.Instance.GameData.Stars.ContainsKey(towerUnlockDictionary[towerId]) || towerUnlockDictionary[towerId] == 0;

    public int GetDirectiveId(WeaponPart directive)
    {
        if (sortedDirectives.Count == 0)
        {
            foreach (var missionReward in missionRewardsAndUnlocks)
            {
                if (missionReward.Value == null)
                    continue;

                foreach (WeaponPart part in missionReward.Value.RewardForChoose)
                {
                    if (part != null && part.PartType == AllEnums.PartType.Directive)
                    {
                        sortedDirectives.Add(part);
                    }
                }
            }

            foreach (WeaponPart startItem in startItems)
            {
                if (startItem.PartType == AllEnums.PartType.Directive)
                {
                    sortedDirectives.Add(startItem);
                }
            }
            sortedDirectives.Sort(WeaponPartComparer);
        }

        int index = sortedDirectives.IndexOf(directive);
        return index != -1 ? (index + 1) : -1;
    }

    public int WeaponPartComparer(WeaponPart x, WeaponPart y)
    {
        int compareVal = GetPartUnlockMission(x).CompareTo(GetPartUnlockMission(y));
        return compareVal != 0 ? compareVal : string.Compare(x.SerializedID, y.SerializedID, StringComparison.Ordinal);
    }

    public int TowerFactoryComparer(ITowerFactory x, ITowerFactory y) => towerUnlockDictionary[x.TowerId].CompareTo(towerUnlockDictionary[y.TowerId]);

    public int TowerComparer(Tower x, Tower y) => towerUnlockDictionary[x.TowerId].CompareTo(towerUnlockDictionary[y.TowerId]);

    [Serializable]
    [HideReferenceObjectPicker]
    private class MissionRewardAndUnlocks
    {
        [OdinSerialize, RequiredListLength(2), HorizontalGroup, ListDrawerSettings(ShowFoldout = false)]
        public List<WeaponPart> RewardForChoose;
        [OdinSerialize, HorizontalGroup, ListDrawerSettings(ShowFoldout = false)]
        public List<WeaponPart> PartToUnlock;
        [OdinSerialize]
        public WeaponPart Reward;
    }

    private const float multiplier1 = 1.2f;
    private const float multiplier2 = 1.1f;
    private const float multiplier3 = 1.05f;

    private const int breakPoint1 = 11;
    private const int breakPoint2 = 31;

    private const float replay1Star = 0.33f;
    private const float replay2Star = 0.66f;

    private const float replayMultiplier1 = 1;
    private const float replayMultiplier2 = 0.7f;
    private const float replayMultiplier3 = 0.5f;

    private const float softStart = 35;
    private const float scrapStart = 15;
    private const float dustStart = 5;

    private const int hardCurrencyGain = 10;

    private const float loseMult = 0.5f;


    /// <summary>
    /// 
    /// </summary>
    /// <param name="missionIndex"></param>
    /// <param name="stars"></param>
    /// <param name="firstTime"></param>
    /// <returns></returns>
    public Reward GetRewardForMission(int missionIndex, int stars, bool firstTime)
    {
        Reward reward = new Reward();

        if (stars >= 1 && firstTime)
            reward.PartsToUnlock = missionRewardsAndUnlocks[missionIndex].PartToUnlock;

        if (stars >= 2 && firstTime)
            reward.HardCurrency = hardCurrencyGain;

        if (stars == 3 && firstTime)
            reward.WeaponPart = missionRewardsAndUnlocks[missionIndex].Reward;

        if (missionIndex != 0  && towerUnlockDictionary.ContainsValue(missionIndex))
            reward.Tower = towerUnlockDictionary.GetKeyForValue(missionIndex);


        float replayMult;
        float mult;


        switch (missionIndex)
        {
            case > breakPoint2:
                mult = math.pow(multiplier1, breakPoint1) * math.pow(multiplier2, breakPoint2 - breakPoint1) * math.pow(multiplier3, missionIndex - breakPoint2);
                replayMult = replayMultiplier3;
                break;
            case > breakPoint1:
                mult = math.pow(multiplier1, breakPoint1) * math.pow(multiplier2, missionIndex - breakPoint1);
                replayMult = replayMultiplier2;
                break;
            default:
                mult = math.pow(multiplier1, missionIndex);
                replayMult = replayMultiplier1;
                break;
        }

        if (stars == 0)
            mult *= loseMult;

        if (!firstTime)
        {
            replayMult *= stars switch
            {
                1 => replay1Star,
                2 => replay2Star,
                3 => 1,
                _ => 1
            };
            mult *= replayMult;
        }

        reward.SoftCurrency = (int)math.round(softStart * mult);
        reward.Scrap = (int)math.round(scrapStart * mult);

        if (stars >= 2)
            reward.Dust = (int)math.round(dustStart * mult);

        return reward;
    }

#if UNITY_EDITOR
    [Button]
    private void CheckPartsFromPartsHolder()
    {
        bool allPartsPresent = true;

        PartsHolder partHolder = DataManager.Instance.Get<PartsHolder>();
        foreach (WeaponPart weaponPart in partHolder.Items)
        {
            try
            {
                GetPartUnlockMission(weaponPart);
            }
            catch
            {
                allPartsPresent = false;
                Debug.LogError($"{weaponPart.name} doesn't present in Unlocker");
            }
        }

        foreach (WeaponPart directive in partHolder.Directives)
        {
            try
            {
                GetPartUnlockMission(directive);
            }
            catch
            {
                allPartsPresent = false;
                Debug.LogError($"{directive.name} doesn't present in Unlocker");
            }
        }

        if (allPartsPresent)
            Debug.Log("---> all parts from PartsHolder present in Unlocker");
    }

    [Button]
    private void CheckDirectivesInUnlocker()
    {
        string missingDirectives = "Missing directives:\n";

        foreach (WeaponPart directive in DataManager.Instance.Get<PartsHolder>().Directives)
        {
            if (GetPartUnlockMission(directive) == 99)
            {
                missingDirectives += directive.name + "\n";
            }
        }
        Debug.Log(missingDirectives);
    }
    [Button]
    private string GetUnlockMissionForPart(WeaponPart weaponPart)
    {
        int missionIndex = GetPartUnlockMission(weaponPart);
        string result;
        if (missionIndex < 0)
            result = $"{weaponPart.name} available on start";
        else
            result = $"{weaponPart.name} unlocks on mission {missionIndex}";

        Debug.Log(result);
        return result;
    }

#endif
}