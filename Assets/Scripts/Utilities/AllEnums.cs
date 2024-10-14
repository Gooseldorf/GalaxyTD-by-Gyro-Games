using System;

public class AllEnums
{
    [Flags]
    public enum AttackPattern : int
    {
        Off = 1 << 0,
        Auto = 1 << 1,
        Burst = 1 << 2,
        Single = 1 << 3,
        All = ~0
    }

    [Flags]
    public enum TowerId
    {   
        Light = 1 << 0,
        Heavy = 1 << 1,
        Shotgun = 1 << 2,
        TwinGun = 1 << 3,
        Cannon = 1 << 4,
        Gatling = 1 << 5,
        Plasma = 1 << 6,
        //where is 1 << 7?? pls add new tower here and restore Utilities.TowerIdToInt
        Laser = 1 << 8,
        Mortar = 1 << 9,
        Rocket = 1 << 10,
        Gauss = 1 << 11,
    }

    [Flags]
    public enum PartType
    {
        Barrel = 1 << 0,
        Magazine = 1 << 1,
        Ammo = 1 << 2,
        RecoilSystem = 1 << 3,
        TargetingSystem = 1 << 4,
        Directive = 1 << 5,
    }

    [Flags]
    public enum ObstacleType
    {
        OnlyRicochet = 1 << 0,
        OnlyPenetrate = 1 << 1,
        ConsumeProjectile = 1 << 2,
        None = 1 << 4,
        All = ~0
    }

    public enum TargetingSystemType
    {
        Gun = 1,
        Homing = 2,
    }

    public enum TargetType
    {
        Creep = 1,
        Tower = 2,
        DropZone = 3
    }

    public enum CreepType : int
    {
        Bio1,
        Bio2,
        Bio3,
        Mech1,
        Mech2,
        Mech3,
        Energy1,
        Energy2,
        Energy3,
        BioHydra,
        Bio4,
        BioSpawner,
        Egg,
        Bio5,
        MechSpawner,
        Mech4,
        BioEvolve1,
        BioEvolve2,
        BioEvolve3,
        TeleportUnit,
    }

    [Serializable]
    public enum CritterType
    {
        TestCritter = 1 << 2
    }
    [Flags]
    public enum ArmorType
    {
        Unarmored = 1 << 0,
        Light = 1 << 1,
        Heavy = 1 << 2,
    }
    [Flags]
    public enum FleshType
    {
        Bio = 1 << 0,
        Mech = 1 << 1,
        Energy = 1 << 2,
    }

    [Serializable]
    public enum Direction
    {
        Left,
        Right,
        Up,
        Down
    }

    public enum AnimationState : byte
    {
        Run,
        Death
    }

    public enum CurrencyType
    {
        Soft = 1,
        Hard = 2,
        Scrap = 3,
        Tickets = 4,
        AdsSkipper = 5,
        Bundle = 6,
    }
    
    public enum PurchaseValueType
    {
        Crystals = 0,
        Real = 1,
        Ads = 2,
    }

    public enum UIState
    {
        Locked = 1,
        Available = 2,
        Unavailable = 3,
        Active = 4
    }

    public enum TextType
    {
        Damage = 1,
        Cash = 2,
        DropZone = 3,
        SpawnZone = 4
    }

    public enum CellEventType
    {
        Detach = 0,
        Return = 1,
        Destroy = 2,
        DestroyAll = 3,
        AttachNew = 4,
    }

    public enum BuffType
    {
        Damage = 0,
        Firerate = 1,
        Range = 2,
        ReloadSpeed = 3,
        Penetration = 4,
        Ricochet = 5
    }

    public enum TagEffectType
    {
        AoeDamage,
        AoeDebuff,
        InstantKill,
        Quantum
    }

    public enum DialogCharacter
    {
        HeadWarden,
        DrElara,
        ShipsCommander,
        ChiefofSecurity,
        ShipsNavigator,
        TacticalDefenseOfficer,
        AlienQueen,
        OrenTheHacker,
        ShipsAI,
        Tauri,
        CommanderAlthor,
        AutomatedGuardian
    }

    public enum DialogPosition
    {
        Any,
        Left,
        Right,
        AnySingle,
        LeftSingle,
        RightSingle,
    }

    public enum TowerState
    {
        Active,
        TurnedOff,
        Reloading,
        NoCashForReload
    }
}