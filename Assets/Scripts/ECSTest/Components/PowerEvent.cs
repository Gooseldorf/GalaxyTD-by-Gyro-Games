using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct PowerEvent: IComponentData
{
    public bool IsActive;
    public bool CanBeActivatedByUser;
}
