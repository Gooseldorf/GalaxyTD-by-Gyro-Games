using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct PowerSwitch : IComponentData
{
    public bool IsTurnedOn;
}
