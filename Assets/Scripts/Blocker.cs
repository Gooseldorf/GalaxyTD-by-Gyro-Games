using Unity.Mathematics;
using UnityEngine;


//Blocker Changes cost of  cells
public class Blocker: IGridPosition//, IPowerable,
{
    [field: SerializeField] public int Id { get; set; }
    [field: SerializeField] public int EnableCost { get; set; }
    [field: SerializeField] public int DisableCost { get; set; }
    [field: SerializeField] public int2 GridPos { get; set; }
    [field: SerializeField] public int2 GridSize { get; set; }
    public bool IsPowered { get; private set; }//on of
    public float3 Direction { get; set; }

    public void TurnOn()
    {
        //set cells cost
        throw new System.NotImplementedException();
    }

    public void TurnOff()
    {
        throw new System.NotImplementedException();
    }


}