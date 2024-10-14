using Unity.Entities;

/// <summary>
/// Overrides previous
/// </summary>
public struct SlowEffect : IComponentData
{
    /// <summary>
    /// MoveSpeedModifier = 1 - EffectStrength;
    /// </summary>
    public float EffectStrength;
    public float Duration;
    //TODO: remove MoveSpeedModifier from Creep
}
