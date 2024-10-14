using Unity.Entities;
using UnityEngine;

namespace ECSTest.Authorings
{
    public abstract class SimpleBaker<T> : Baker<T> where T : Component
    {
        public override void Bake(T authoring)
        {
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity entity = manager.CreateEntity();
            OnEntityCreated(entity, manager, authoring);
        }

        protected abstract void OnEntityCreated(Entity entity, EntityManager manager, T authoring);
    }
}