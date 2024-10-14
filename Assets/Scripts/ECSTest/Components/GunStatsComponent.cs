using Unity.Entities;

public struct GunStatsComponent : IComponentData
{
    public int RicochetCount;
    public int PenetrationCount;
    public float RicochetDeviation;
    public float DamageMultPerPenetration;
    public float DamageMultPerRicochet;

    public float Deviation;
    public float Control;
    public float Recoil;

    public float DamageFallof;

    public GunStatsComponent(GunStats gunStats) : this()
    {
        Deviation = gunStats.AccuracyStats.Deviation;
        Control = gunStats.AccuracyStats.Control;
        Recoil = gunStats.AccuracyStats.Recoil;

        RicochetCount = gunStats.RicochetStats.RicochetCount;
        PenetrationCount = gunStats.RicochetStats.PenetrationCount;
        RicochetDeviation = gunStats.RicochetStats.RicochetDeviation;
        DamageMultPerPenetration = gunStats.RicochetStats.DamageMultPerPenetration;
        DamageMultPerRicochet = gunStats.RicochetStats.DamageMultPerRicochet;

        DamageFallof = gunStats.DamageFallof;
    }
}
