using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct PowerEventBuffer : IBufferElementData
{
    public Entity Entity;
    public float Timer;
    public int Activations;
}
