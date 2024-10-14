using ECSTest.Components;

public abstract class OnKillTag : Tag
{
    public abstract void OnKill(OnKillData handler, ref CreepComponent creepComponent);
}