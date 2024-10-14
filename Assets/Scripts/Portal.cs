using Unity.Mathematics;
using System;
using Random = UnityEngine.Random;
using UnityEngine;

[Serializable]
public class Portal : IPowerable
{
    [field: SerializeField] public int Id { get; set; }
    [field: SerializeField] public GridPosition In { get; set; }
    [field: SerializeField] public GridPosition Out { get; set; }
    [field: SerializeField] public bool IsPowered { get; set; }

    public int2 RandomOutPosition => Out.GridPos + new int2(Random.Range(0, Out.GridSize.x), Random.Range(0, Out.GridSize.y));
    
    public event Action OnTogglePower;

    public void TogglePower()
    {
        IsPowered = !IsPowered;
        OnTogglePower?.Invoke();
    }
}
