using System.Collections.Generic;
using Unity.Mathematics;

public interface IAttacker : IPosition
{
    AttackStats AttackStats { get; set; }
    float AttackDelay { get; set; }
    float StartOffset { get; set; }

    float3 GetProjectilePosition(int i) => Position + (StartOffset * Direction);
    public object this[string propertyName]
    {
        get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
        set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
    }

}