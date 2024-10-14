using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct Timer : IComponentData
{
    public float Value;
    public float TimeBetweenToggles;
    public int Activations;
    public bool Activated;
}
