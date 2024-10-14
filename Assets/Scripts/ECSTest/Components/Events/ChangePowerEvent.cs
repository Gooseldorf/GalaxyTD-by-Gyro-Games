using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Components
{
    public struct ChangePowerEvent : IComponentData, IEnableableComponent
    {
        public Entity Entity;
        public bool IsTurnedOn;
    }
}
