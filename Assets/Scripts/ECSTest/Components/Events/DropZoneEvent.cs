using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Components
{
    public struct DropZoneEvent : IComponentData, IEnableableComponent
    {
        public Entity Entity;
    }
}
