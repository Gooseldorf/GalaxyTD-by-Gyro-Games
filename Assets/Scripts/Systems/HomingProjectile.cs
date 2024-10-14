public class HomingProjectile : Projectile
{
    public IPosition Target { get; set; }
    public bool GoingToGround { get; set; }

    public HomingProjectile(IAttacker attacker, float lastUpdate) : base(attacker, lastUpdate)
    {
    }
}