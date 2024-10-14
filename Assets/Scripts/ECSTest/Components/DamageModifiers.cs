using Unity.Entities;


public struct DamageModifiers : IComponentData
{
    public float DamageToUnarmored;
    public float DamageToLight;
    public float DamageToHeavy;
    public float DamageToBio;
    public float DamageToMechanical;
    public float DamageToEnergy;
}
