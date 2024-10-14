using Unity.Entities;

/// <summary>
/// Overrides previous
/// </summary>
public struct RadiationEffect : IComponentData
{

    /// <summary>
    /// This is Damage Per Second, But ticks happen more often than once in a second 
    /// </summary>
    public float DPS;
    public float Duration;
}
