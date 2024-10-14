using Unity.Entities;

public struct AttackerStatisticComponent : IComponentData
{
    public int Kills;
    public int Shoots;
    public int Reloads;
    public int CashBuildSpent;
    public int CashUpgradeSpent;
    public int CashReloadSpent;
    public float CreepDamage;
    public float OverheatDamage;
}