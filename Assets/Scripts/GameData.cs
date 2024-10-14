using CardTD.Utilities;
using Data.Managers;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tags;
using UI;
using UnityEditor;
using UnityEngine;
using static AllEnums;
using Random = UnityEngine.Random;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;

public class GameData : SerializedScriptableObject
{
    private const string saveFileName = "Save.data";
    private const string defaultGameDataName = "DefaultGameData";

    [OdinSerialize, NonSerialized] private List<TowerFactory> factories;
    [SerializeField] private Dictionary<int, int> stars;
    [SerializeField] private Dictionary<int, int> hardStars;
    [SerializeField] private int softCurrency;
    [SerializeField] private int hardCurrency;
    [SerializeField] private bool skipADS;
    [SerializeField] private int scrap;
    [SerializeField] private int tickets;

    [OdinSerialize, NonSerialized] private Inventory inventory = new Inventory();
    [SerializeField] private List<WeaponPart> newItems = new();
    [SerializeField] private List<TowerId> newFactories = new();
    [SerializeField] private List<WeaponPart> newUnlockedItems = new();
    [SerializeField] private List<TowerId> newUnlockedFactories = new();
    public bool HasNewUnlockedItems => newUnlockedItems?.Count > 0 || newUnlockedFactories?.Count > 0;
    [SerializeField] private Dictionary<int, int> selectedRewards = new();

    [SerializeField, ReadOnly] private bool dataIsSyncedWithDiskSave;
    [SerializeField] private int dust;
    private bool saveToDisk = true;

    public int LastDialogBefore = -1;
    public int LastDialogAfter = -1;
    
    public int LastCompletedMissionIndex = -1;
    
    public bool UseBulletCost =>  DataManager.Instance.Get<UnlockManager>().IsUseReloadCostMechanic(Stars.Count);
    
    public Mission GetLastMission
    {
        get
        {
            int missionIndex = Stars.Count + 1;
            MissionList missionList = DataManager.Instance.Get<MissionList>();

            if (missionList.Missions.Count <= 0)
                throw new Exception("mission list is empty");

            if (missionIndex >= missionList.Missions.Count)
                missionIndex = missionList.Missions.Count - 1;

            if (missionIndex < 0)
                missionIndex = 0;
            return missionList.Missions[missionIndex];
        }
    }

    [HideInInspector] public bool ShowRewardSelection;
    public IReadOnlyList<ITowerFactory> TowerFactories => factories;
    public IReadOnlyDictionary<int, int> Stars => stars;
    public IReadOnlyDictionary<int, int> HardStars => hardStars;

    [ReadOnly] public bool ShouldShowDialog = false;

    public int SoftCurrency
    {
        get => softCurrency;
        private set
        {
            softCurrency = value > 0 ? value : 0;
            CurrencyUpdated?.Invoke();
        }
    }

    public int HardCurrency
    {
        get => hardCurrency;
        private set
        {
            hardCurrency = value > 0 ? value : 0;
            CurrencyUpdated?.Invoke();
        }
    }

    public bool SkipAds => skipADS;

    public int Scrap
    {
        get => scrap;
        private set
        {
            scrap = value > 0 ? value : 0;
            CurrencyUpdated?.Invoke();
        }
    }

    public int Dust
    {
        get => dust;
        private set
        {
            dust = value > 0 ? value : 0;
            CurrencyUpdated?.Invoke();
        }
    }

    public int Tickets
    {
        get => tickets;
        private set
        {
            tickets = value > 0 ? value : 0;
            CurrencyUpdated?.Invoke();
        }
    }

    public List<TowerFactory> Factories => factories;

    public Inventory Inventory => inventory;
    public List<WeaponPart> NewItems => newItems;
    public List<AllEnums.TowerId> NewFactories => newFactories;
    public List<AllEnums.TowerId> NewUnlockedFactories => newUnlockedFactories;
    public List<WeaponPart> NewUnlockedItems => newUnlockedItems;

    public IReadOnlyDictionary<int, int> SelectedRewards => selectedRewards;

    public event Action CurrencyUpdated;
    public event Action<ITowerFactory> FactoryUpdated;

    public bool UpgradeFactory(ITowerFactory iFactory)
    {
        //No such factory
        TowerFactory factory = factories.Find(x => x == iFactory);
        if (factory == null) return false;

        MenuUpgrade upgrade = DataManager.Instance.Get<UpgradeProvider>().GetNextUpgrade(factory.TowerId, factory.Level);
        int upgradeCost = upgrade.Cost /* + upgrade.Cost * factory.Level*/;
        if (DataManager.Instance.GameData.SoftCurrency < upgradeCost)
        {
            return false;
        }

        SoftCurrency -= upgradeCost;
        factory.UpgradeFactory();
        dataIsSyncedWithDiskSave = false;
        SaveToDisk();
        return true;
    }

    public Reward WinMission(Mission mission, int starsForWin, bool isHard)
    {
        LastCompletedMissionIndex = mission.MissionIndex;
        UnlockManager unlockManager = DataManager.Instance.Get<UnlockManager>();
        Dictionary<int, int> currentStars = isHard ? hardStars : stars;
        bool firstTry = !currentStars.ContainsKey(mission.MissionIndex);

        Reward reward = unlockManager.GetRewardForMission(mission.MissionIndex, starsForWin, firstTry);

        AddCurrencies(reward);

        // We need to simplify this. This is a mess.
        //--------------------------------------------------------------------
        AddTower(reward.Tower);

        if (reward.WeaponPart != null)
            Inventory.AddWeaponPart(reward.WeaponPart);

        UnlockParts(reward);
        //--------------------------------------------------------------------
        if (firstTry)
            currentStars[mission.MissionIndex] = starsForWin;
        else currentStars[mission.MissionIndex] = Mathf.Max(currentStars[mission.MissionIndex], starsForWin);

        ShouldShowDialog = true;
        dataIsSyncedWithDiskSave = false;
        SaveToDisk();

        return reward;
    }

    private void UnlockParts(Reward reward)
    {
        if (reward.PartsToUnlock != null)
        {
            foreach (WeaponPart unlock in reward.PartsToUnlock)
                if (!NewItems.Contains(unlock))
                {
                    NewItems.Add(unlock);
                    if (!NewUnlockedItems.Contains(unlock) && unlock.PartType != AllEnums.PartType.Ammo)
                        NewUnlockedItems.Add(unlock);
                }
        }
    }

    private void AddTower(TowerId towerID)
    {
        if (towerID != 0)
        {
            if (!NewUnlockedFactories.Contains(towerID)) NewUnlockedFactories.Add(towerID);
            if (!NewFactories.Contains(towerID)) NewFactories.Add(towerID);
        }
    }

    private void AddCurrencies(Reward reward)
    {
        SoftCurrency += reward.SoftCurrency;
        HardCurrency += reward.HardCurrency;
        Scrap += reward.Scrap;
        Dust += reward.Dust;
    }

    public void TakeReward(DailyReward reward, out WeaponPart directiveReward)
    {
        directiveReward = null;
        switch (reward.DailyRewardType)
        {
            case DailyRewardType.Soft:
                SoftCurrency += reward.GetRewardAmount;
                break;
            case DailyRewardType.Hard:
                HardCurrency += reward.GetRewardAmount;
                break;
            case DailyRewardType.Scrap:
                Scrap += reward.GetRewardAmount;
                break;
            case DailyRewardType.Ticket:
                Tickets += reward.GetRewardAmount;
                break;
            case DailyRewardType.RandomDirective:

                var unlockManager = DataManager.Instance.Get<UnlockManager>();
                List<WeaponPart> directivesData = DataManager.Instance.Get<PartsHolder>().Directives;

                List<WeaponPart> openDirectives = new();

                foreach (WeaponPart directive in directivesData)
                {
                    if (unlockManager.IsPartUnlocked(directive))
                        openDirectives.Add(directive);
                }

                if (openDirectives.Count > 0)
                {
                    directiveReward = openDirectives[Random.Range(0, openDirectives.Count)];
                    Inventory.AddWeaponPart(directiveReward);
                }
                else
                {
                    //7 days and do not open directives
                    Tickets += reward.GetRewardAmount;
                }

                break;
        }

        dataIsSyncedWithDiskSave = false;
        SaveToDisk();
    }

    public void RemoveFromNewItems(object item)
    {
        switch (item)
        {
            case AllEnums.TowerId tower:
                if (newFactories != null && newFactories.Contains(tower))
                {
                    newFactories.Remove(tower);
                    break;
                }
                else return;
            case WeaponPart part:
                if (newItems != null && newItems.Contains(part))
                {
                    newItems.Remove(part);
                    break;
                }
                else return;
            default:
                return;
        }

        Messenger.Broadcast(UIEvents.OnNewItemsUpdated, MessengerMode.DONT_REQUIRE_LISTENER);
    }

    public void ClearNewUnlockedItems()
    {
        dataIsSyncedWithDiskSave = false;
        newUnlockedItems.Clear();
        newUnlockedFactories.Clear();
        SaveToDisk();
    }

    public Reward IncreaseReward(Reward reward)
    {
        dataIsSyncedWithDiskSave = false;
        AddCurrencies(reward);
        SaveToDisk();
        reward.Dust *= 2;
        reward.HardCurrency *= 2;
        reward.Scrap *= 2;
        reward.SoftCurrency *= 2;

        return reward;
    }

    public bool BuyPart(WeaponPart part)
    {
        if (scrap < part.ScrapCost || softCurrency < part.SoftCost || hardCurrency < part.HardCost) return false;

        dataIsSyncedWithDiskSave = false;

        SoftCurrency -= part.SoftCost;
        HardCurrency -= part.HardCost;
        Scrap -= part.ScrapCost;

        inventory.AddWeaponPart(part);
        
        Messenger<WeaponPart>.Broadcast(GameEvents.BuyPart, part, MessengerMode.DONT_REQUIRE_LISTENER);

        SaveToDisk();
        return true;
    }

    public bool InsertPart(ITowerFactory iFactory, IWeaponPart weaponPart, ISlot slot)
    {
        //No such factory
        TowerFactory factory = factories.Find(x => x.TowerId == iFactory.TowerId);
        if (factory == null) return false;
        //no such slot in factory
        if (factory.Ammo != slot && !factory.Parts.Contains(slot) && !factory.Directives.Contains(slot)) return false;
        //no such weapon part owned
        if (!inventory.UnusedWeaponParts.Contains(weaponPart) && !inventory.UnusedAmmoParts.Contains(weaponPart) && !inventory.UnusedDirectives.ContainsKey((WeaponPart)weaponPart)) return false;
        //part cannot be inserted into the slot
        if (!weaponPart.PartType.HasFlag(slot.PartType) || !weaponPart.TowerId.HasFlag(factory.TowerId)) return false;
        if (slot.WeaponPart != null) inventory.AddWeaponPart(slot.WeaponPart);

        (slot as Slot).WeaponPart = weaponPart as WeaponPart;
        dataIsSyncedWithDiskSave = false;
        FactoryUpdated?.Invoke(factory);
        SaveToDisk();
        return true;
    }

    public bool RemoveDirective(ITowerFactory iFactory, ISlot slot)
    {
        //No such factory
        TowerFactory factory = factories.Find(x => x == iFactory);
        if (factory == null) return false;

        //no such slot in factory
        if (!factory.Directives.Contains(slot)) return false;

        //slot is empty
        if (slot.WeaponPart == null) return false;
        WeaponPart addWeaponPart = slot.WeaponPart;
        (slot as Slot).WeaponPart = null;
        inventory.AddWeaponPart(addWeaponPart);

        dataIsSyncedWithDiskSave = false;
        FactoryUpdated?.Invoke(factory);
        SaveToDisk();
        return true;
    }

    public void ChooseRewardForMission(Mission mission, int rewardIndex)
    {
        List<WeaponPart> rewardItems = DataManager.Instance.Get<UnlockManager>().GetRewardsForChoose(mission.MissionIndex);
        
        inventory.AddWeaponPart(rewardItems[rewardIndex]);
        if (!newItems.Contains(rewardItems[rewardIndex]))
            newItems.Add(rewardItems[rewardIndex]);

        if (newUnlockedItems.Contains(rewardItems[rewardIndex]))
            newUnlockedItems.Remove(rewardItems[rewardIndex]);
        
        selectedRewards.TryAdd(mission.MissionIndex, rewardIndex);
        ShowRewardSelection = false;

        SaveToDisk();
    }

    public void BuySoftCurrency(int amount, int price)
    {
        HardCurrency -= price;
        AddSoftCurrency(amount);
        Messenger<int,int>.Broadcast(GameEvents.BuySoft,amount,price,MessengerMode.DONT_REQUIRE_LISTENER);
    }

    public void AddHardCurrency(int amount)
    {
        HardCurrency += amount;
        dataIsSyncedWithDiskSave = false;
        SaveToDisk();
    }

    public void AddSoftCurrency(int amount)
    {
        SoftCurrency += amount;
        dataIsSyncedWithDiskSave = false;
        SaveToDisk();
    }

    public void SetSkipAds()
    {
        skipADS = true;
        dataIsSyncedWithDiskSave = false;
        SaveToDisk();
    }

    public void BuyScrap(int amount, int price)
    {
        HardCurrency -= price;
        AddScrap(amount);
    }

    public void BuyTickets(int amount, int price)
    {
        Tickets += amount;
        HardCurrency -= price;
        dataIsSyncedWithDiskSave = false;
        SaveToDisk();
    }

    public void AddScrap(int amount)
    {
        Scrap += amount;
        dataIsSyncedWithDiskSave = false;
        SaveToDisk();
    }

    public void SpendTicket()
    {
        tickets -= 1;
        dataIsSyncedWithDiskSave = false;
        SaveToDisk();
    }

    public Tower[] GetTowersByUnlockManager()
    {
        UnlockManager unlockManager = DataManager.Instance.Get<UnlockManager>();
        List<Tower> towers = new();

        foreach (TowerFactory factory in factories)
        {
            if (unlockManager.IsTowerUnlocked(factory.TowerId))
                towers.Add(factory.GetAssembledTower());
        }

        return towers.ToArray();
    }

    public Tower[] GetTowers()
    {
        List<Tower> towers = new();
        foreach (TowerFactory factory in factories)
        {
            towers.Add(factory.GetAssembledTower());
        }

        return towers.ToArray();
    }

    public Tower[] GetStartTowerForRoguelike()
    {
        List<Tower> towers = new();
        for (int index = 0; index < 2; index++)
        {
            towers.Add(factories[index].GetAssembledTower());
        }

        return towers.ToArray();
    }
    
    [Button, HideIf("IsDefaultGameData")]
    public void SaveToDisk()
    {
        if (!saveToDisk)
            return;

        string savePath = Path.Combine(Application.persistentDataPath, saveFileName);

        //TODO: rotation of saves    
        dataIsSyncedWithDiskSave = true;
        SaveDataToDisk(savePath, this);
        dataIsSyncedWithDiskSave = dataIsSyncedWithDiskSave && (dataIsSyncedWithDiskSave == true);
    }

    public static void SaveDefaultDataToDisk()
    {
        string savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        GameData gameData = Resources.Load<GameData>(defaultGameDataName);

        //TODO: rotation of saves    
        SaveDataToDisk(savePath, gameData);
    }

    private static void SaveDataToDisk(string path, GameData gameData)
    {
        try
        {
            using (FileStream fs = new(path, FileMode.Create))
            {
                var context = new SerializationContext() {StringReferenceResolver = StringReferenceResolver.Instance};
                IDataWriter writer = SerializationUtility.CreateWriter(fs, context, DataFormat.Binary);

                SerializeGameData(gameData, writer);
                // Debug.Log("Save Successful");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed To Save");
        }
    }

    private static void SerializeGameData(GameData gameData, IDataWriter writer)
    {
        SerializationUtility.SerializeValue(gameData.factories, writer);
        SerializationUtility.SerializeValue(gameData.newFactories, writer);
        SerializationUtility.SerializeValue(gameData.newUnlockedFactories, writer);

        SerializationUtility.SerializeValue(gameData.softCurrency, writer);
        SerializationUtility.SerializeValue(gameData.hardCurrency, writer);
        SerializationUtility.SerializeValue(gameData.scrap, writer);
        SerializationUtility.SerializeValue(gameData.tickets, writer);

        SerializationUtility.SerializeValue(gameData.stars, writer);
        SerializationUtility.SerializeValue(gameData.hardStars, writer);
        SerializationUtility.SerializeValue(gameData.LastCompletedMissionIndex, writer);
        SerializationUtility.SerializeValue(gameData.LastDialogBefore, writer);
        SerializationUtility.SerializeValue(gameData.LastDialogAfter, writer);

        SerializationUtility.SerializeValue(gameData.selectedRewards, writer);
        SerializationUtility.SerializeValue(gameData.ShowRewardSelection, writer);

        SerializationUtility.SerializeValue(gameData.inventory, writer);
        SerializationUtility.SerializeValue(gameData.newItems, writer);
        SerializationUtility.SerializeValue(gameData.newUnlockedItems, writer);

        SerializationUtility.SerializeValue(gameData.skipADS, writer);

        SerializationUtility.SerializeValue(gameData.ShouldShowDialog, writer);

        SerializationUtility.SerializeValue(gameData.dust, writer);
    }

    public bool IsDefaultGameData => this.name == defaultGameDataName;

    [Button, HideIf("IsDefaultGameData")]
    public void LoadFromDisk()
    {
        try
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Load");
#endif
            dataIsSyncedWithDiskSave = true;
            string savePath = Path.Combine(Application.persistentDataPath, saveFileName);
            Debug.Log($"savePath {savePath}");
            //TODO: rotation of saves
            using (FileStream fs = new(savePath, FileMode.Open))
            {
                DeserializationContext context = new() { StringReferenceResolver = StringReferenceResolver.Instance, };

                var reader = SerializationUtility.CreateReader(fs, context, DataFormat.Binary);
                factories = SerializationUtility.DeserializeValue<List<TowerFactory>>(reader);
                newFactories = SerializationUtility.DeserializeValue<List<AllEnums.TowerId>>(reader);
                newUnlockedFactories = SerializationUtility.DeserializeValue<List<AllEnums.TowerId>>(reader);

                SoftCurrency = SerializationUtility.DeserializeValue<int>(reader);
                HardCurrency = SerializationUtility.DeserializeValue<int>(reader);
                Scrap = SerializationUtility.DeserializeValue<int>(reader);
                Tickets = SerializationUtility.DeserializeValue<int>(reader);
                stars = SerializationUtility.DeserializeValue<Dictionary<int, int>>(reader);
                hardStars = SerializationUtility.DeserializeValue<Dictionary<int, int>>(reader);
                LastCompletedMissionIndex = SerializationUtility.DeserializeValue<int>(reader);
                LastDialogBefore = SerializationUtility.DeserializeValue<int>(reader);
                LastDialogAfter = SerializationUtility.DeserializeValue<int>(reader);

                selectedRewards = SerializationUtility.DeserializeValue<Dictionary<int, int>>(reader);

                ShowRewardSelection = SerializationUtility.DeserializeValue<bool>(reader);
                inventory = SerializationUtility.DeserializeValue<Inventory>(reader);

                newItems = SerializationUtility.DeserializeValue<List<WeaponPart>>(reader);
                newUnlockedItems = SerializationUtility.DeserializeValue<List<WeaponPart>>(reader);

                skipADS = SerializationUtility.DeserializeValue<bool>(reader);

                ShouldShowDialog = SerializationUtility.DeserializeValue<bool>(reader);
                try
                {
                    Dust = SerializationUtility.DeserializeValue<int>(reader);
                }
                catch
                {
                    Debug.Log("oops old player has no dust in save file");
                    Dust = 0;
                }
            }

            Debug.Log("Load Successful");
        }
        catch (Exception ex)
        {
            dataIsSyncedWithDiskSave = false;
            Debug.LogException(ex);
            Debug.LogError("Failed To Load");
        }
    }

#if UNITY_EDITOR

    #region DefaultData

    [Button, HideIf("IsDefaultGameData")]
    public void SetDefaultData()
    {
        var gameData = Resources.Load<GameData>(defaultGameDataName);
        factories = new();
        foreach (TowerFactory towerFactory in gameData.factories)
            factories.Add(towerFactory.Clone());

        newFactories = new(gameData.newFactories);
        newUnlockedFactories = new(gameData.newUnlockedFactories);

        SoftCurrency = gameData.SoftCurrency;
        HardCurrency = gameData.HardCurrency;
        Scrap = gameData.Scrap;
        Tickets = gameData.Tickets;
        Dust = gameData.Dust;

        stars = new(gameData.stars);
        hardStars = new(gameData.hardStars);
        LastCompletedMissionIndex = gameData.LastCompletedMissionIndex;
        LastDialogBefore = gameData.LastDialogBefore;
        LastDialogAfter = gameData.LastDialogAfter;

        selectedRewards = new(gameData.selectedRewards);
        ShowRewardSelection = gameData.ShowRewardSelection;

        inventory = gameData.inventory.Clone();
        newItems = new(gameData.newItems);
        newUnlockedItems = new(gameData.newUnlockedItems);

        ShouldShowDialog = gameData.ShouldShowDialog;
        skipADS = gameData.SkipAds;

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    #endregion

    [Button]
    private void FillInitialInventory()
    {
        foreach (var factory in factories)
        {
            foreach (var slot in factory.Parts)
            {
                if (!inventory.UnusedWeaponParts.Contains(slot.WeaponPart))
                    inventory.AddWeaponPart(slot.WeaponPart);
            }
        }
    }

    [HideIf("IsDefaultGameData")]
    [FoldoutGroup("CompleteUntil")]
    [Button]
    private void SimulateMissionCompletion(int missionsCount)
    {
        DataManager dataManager = DataManager.Instance;

        EditorUtility.SetDirty(this);
        SetDefaultData();
        MissionList missionList = DataManager.Instance.Get<MissionList>();
        if (missionsCount > missionList.Missions.Count)
        {
            Debug.LogError($"MissionIndex is too large! Missions available {missionList.Missions.Count}");
            return;
        }

        if (DataManager.Instance.PredictedSoft.Count < missionsCount)
            Debug.Log($"DataManager/PredictedSoft parsed until {dataManager.PredictedSoft.Count}!");

        if (DataManager.Instance.PredictedHard.Count < missionsCount)
            Debug.Log($"DataManager/PredictedHard parsed until {dataManager.PredictedHard.Count}! Default values have been set!");

        if (DataManager.Instance.PredictedScrap.Count < missionsCount)
            Debug.Log($"DataManager/PredictedScrap parsed until {dataManager.PredictedScrap.Count}!");

        for (int i = 0; i < missionsCount; i++)
        {
            stars.Add(i, 3);
            if (missionsCount >= DataManager.Instance.HardModeMissionCountThreshold)
                hardStars.Add(i, 3);
            LastCompletedMissionIndex = missionsCount - 1;

            if (i < dataManager.PredictedSoft.Count - 1)
                softCurrency = dataManager.PredictedSoft[missionsCount];

            if (i < dataManager.PredictedSoft.Count - 1)
                hardCurrency = dataManager.PredictedHard[missionsCount];
            else
                hardCurrency = missionsCount * 10 * 2;

            if (i < dataManager.PredictedScrap.Count - 1)
                scrap = dataManager.PredictedScrap[missionsCount];
            List<WeaponPart> rewardItems = DataManager.Instance.Get<UnlockManager>().GetRewardsForChoose(i);
            Inventory.AddWeaponPart(rewardItems[0]);
            Inventory.AddWeaponPart(rewardItems[1]);
            selectedRewards.Add(i, 0);
        }

        AssetDatabase.SaveAssets();
    }
#endif
}