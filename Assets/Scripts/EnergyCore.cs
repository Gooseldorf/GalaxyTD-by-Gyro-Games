using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class EnergyCore : IGridPosition, IPowerable, ICloneable
{
    [field: SerializeField] public int Id { get; set; }

    [field: SerializeField] public int2 GridPos { get; set; }
    [field: SerializeField] public int2 GridSize { get; set; }
    
    public float3 Direction { get; set; }

    [SerializeField] public List<int> Powerables;
    public int PowerCellCount;
    public float DeactivationTime;
    public bool IsPowered => PowerCellCount > 0;
    public event Action OnTogglePower;

    public bool IsTurn = true;

    public EnergyCore(int2 gridPos, int2 gridSize, List<int> powerables, int powerCellCount, int id,float deactivationTime)
    {
        GridPos = gridPos;
        GridSize = gridSize;
        Powerables = powerables;
        PowerCellCount = powerCellCount;
        Id = id;
        DeactivationTime = deactivationTime;
    }

    public void TurnOn()
    {
        Debug.LogError($"{nameof(EnergyCore)} {nameof(TurnOn)} method is depreciated! ECS should handle this functionality");
        /*IsTurn = true;
        IdentitySystem identitySystem = GameServices.Instance.Get<IdentitySystem>();

        foreach (int powerable in Powerables)
        {
            object obj = identitySystem.TryGetObjectById(powerable);
            if (obj != null && obj is IPowerable powerableItem)
                ChangeState(powerableItem);
        }*/
    }

    public void TurnOff()
    {
        Debug.LogError($"{nameof(EnergyCore)} {nameof(TurnOn)} method is depreciated! ");
        /*this.IsTurn = false;
        IdentitySystem identitySystem = GameServices.Instance.Get<IdentitySystem>();

        foreach (int powerable in Powerables)
        {
            object obj = identitySystem.TryGetObjectById(powerable);
            if (obj != null && obj is IPowerable powerableItem)
                ChangeState(powerableItem);
        }*/
    }

    public void TogglePower()
    {
        if(IsPowered) TurnOn();
        else TurnOff();
        OnTogglePower?.Invoke();
    }

    private void ChangeState(IPowerable powerable)
    {
        powerable.TogglePower();
    }

    public object Clone()
    {
        EnergyCore clone = this.MemberwiseClone() as EnergyCore;
        clone.Powerables = new List<int>(this.Powerables);
        return clone;
    }
}