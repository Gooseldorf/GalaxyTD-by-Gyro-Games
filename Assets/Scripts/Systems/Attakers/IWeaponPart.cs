public interface IWeaponPart: ICustomSerialized
{
    AllEnums.PartType PartType { get; }
    AllEnums.TowerId TowerId { get; }
}