public static class GameEvents
{
    public const string BuySoft = nameof(BuySoft);
    public const string ShowLoseWindow = nameof(ShowLoseWindow);
    public const string BuyForCrystals = nameof(BuyForCrystals);
    public const string ShowAdsReward = nameof(ShowAdsReward);
    public const string TryStartMission = nameof(TryStartMission);
    public const string ShowPanel = nameof(ShowPanel);
    public const string HidePanel = nameof(HidePanel);
    public const string InitGame = nameof(InitGame);
    public const string BuildTowerTowerId = nameof(BuildTowerTowerId);

    public const string BuyWeaponPart = nameof(BuyWeaponPart);
    
    public const string BuyPart = nameof(BuyPart);
    
    public const string CashUpdated = nameof(CashUpdated);

    public const string UpdateUnitsLeftToSpawn = nameof(UpdateUnitsLeftToSpawn);

    public const string NextWave = nameof(NextWave);
    public const string UnitSpawned = nameof(UnitSpawned);
    public const string CellDestroyed = nameof(CellDestroyed);
    public const string CellDetached = nameof(CellDetached);
    public const string CellAttached = nameof(CellAttached);
    public const string CellDestroyedAll = nameof(CellDestroyedAll);
    public const string CellAttachedNew = nameof(CellAttachedNew);

    public const string ObjectDestroyed = nameof(ObjectDestroyed);
    public const string TeleportEvent = nameof(TeleportEvent);

    public const string BuildTower = nameof(BuildTower);

    public const string GetDailyReward = nameof(GetDailyReward);
    
    public const string TowerSell = nameof(TowerSell);

    public const string Lost = nameof(Lost);
    public const string Win = nameof(Win);
    public const string Restart = nameof(Restart);
    public const string TowerUpgraded = nameof(TowerUpgraded);

    public const string TowerUpdated = nameof(TowerUpdated);
    public const string TowerReload = nameof(TowerReload);

    public const string BubbleEvent = nameof(BubbleEvent);

    public const string UpdateVisualWarning = nameof(UpdateVisualWarning);

    public const string DropZoneStateChanged = nameof(DropZoneStateChanged);
    public const string TowerTeleported = nameof(TowerTeleported);

    public const string SecondChanceUsed = nameof(SecondChanceUsed);
    public const string PowerChanged = nameof(PowerChanged);
}

public static class UIEvents
{
    public const string ObjectSelected = nameof(ObjectSelected);
    public const string LanguageChanged = nameof(LanguageChanged);
    public const string ModeChanged = nameof(ModeChanged);
    public const string OnUIAnimation = nameof(OnUIAnimation);
    public const string OnNewItemsUpdated = nameof(OnNewItemsUpdated);
    public const string OnElementResolved = nameof(OnElementResolved);
    public const string GoToShop = nameof(GoToShop);
    public const string ShowNotification = nameof(ShowNotification);
    public const string DailyRewardsReceived = nameof(DailyRewardsReceived);
    public const string LoadingCompleted = nameof(LoadingCompleted);
    public const string PurchaseCompleted = nameof(PurchaseCompleted);
    public const string PurchaseWeaponPartsCompleted = nameof(PurchaseWeaponPartsCompleted);
    public const string Test = nameof(Test);
}

public static class TutorialKeys
{
    //0 Game
    public const string TowerBuilding = nameof(TowerBuilding);
    public const string EnemyBehaviour_dialog = nameof(EnemyBehaviour_dialog);
    public const string EnemyBehaviour = nameof(EnemyBehaviour);
    public const string BridgeShowcase_setup = nameof(BridgeShowcase_setup);
    public const string BridgeShowcase = nameof(BridgeShowcase);
    //0 Menu
    public const string MenuUpgrades = nameof(MenuUpgrades);
    //1 Game
    public const string GameUpgrades = nameof(GameUpgrades);
    public const string UberTowerShowcase_setup = nameof(UberTowerShowcase_setup);
    public const string UberTowerShowcase = nameof(UberTowerShowcase);
    //2 Game
    public const string BlockPath = nameof(BlockPath);
    //3 Game
    public const string CoreDeactivation = nameof(CoreDeactivation);
    public const string DeathRoomShowcase_setup = nameof(DeathRoomShowcase_setup);
    public const string DeathRoomShowcase = nameof(DeathRoomShowcase);
    //4 Game
    public const string MultipleWays_dialog = nameof(MultipleWays_dialog);
    public const string MultipleWays = nameof(MultipleWays);
    //4 Menu
    public const string Directives = nameof(Directives);
    
    public const string NewAmmo = nameof(NewAmmo);
    //After 14 Menu
    public const string HardModeUnlocked = nameof(HardModeUnlocked);
    //After 15 Menu
    public const string NewPart = nameof(NewPart);
    //19 InGame
    public const string Portals = nameof(Portals);
    //Others
    public const string CellDetachedTutor = nameof(CellDetachedTutor);
    public const string NoCashForReload = nameof(NoCashForReload);
    public const string DropZoneState = nameof(DropZoneState);
    public const string NewTower = nameof(NewTower);
    public const string NewEnemy = nameof(NewEnemy);
    public const string DropZoneActivation = nameof(DropZoneActivation);
}

public static class PrefKeys
{
    public static string FirstStart = nameof(FirstStart);
    public static string SkipOldDialogs = nameof(SkipOldDialogs);
}