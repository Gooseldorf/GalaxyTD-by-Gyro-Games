public class GunTower : Tower
{
    public float Deviation => (AttackStats as GunStats).AccuracyStats.Deviation;
}