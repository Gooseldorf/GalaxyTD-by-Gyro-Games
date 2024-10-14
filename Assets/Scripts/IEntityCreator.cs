using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public interface IEntityCreator
{
    public Entity CreateEntity();
    public void SetName(Entity entity, Unity.Collections.FixedString64Bytes name);
    public void AddComponent<T>(Entity e, T component) where T : unmanaged, IComponentData;
    public void AddSharedComponent<T>(Entity e, T component) where T : unmanaged, ISharedComponentData;
    public void SetComponentEnabled<T>(Entity e, bool value) where T : struct, IEnableableComponent;
}

public class ManagerCreator : IEntityCreator
{
    private EntityManager manager;

    public ManagerCreator(EntityManager manager) => this.manager = manager;

    public void AddComponent<T>(Entity e, T component) where T : unmanaged, IComponentData
        => manager.AddComponentData(e, component);

    public void AddSharedComponent<T>(Entity e, T component) where T : unmanaged, ISharedComponentData
        => manager.AddSharedComponent(e, component);

    public Entity CreateEntity() => manager.CreateEntity();

    public void SetName(Entity entity, Unity.Collections.FixedString64Bytes name) => manager.SetName(entity, name);

    public void SetComponentEnabled<T>(Entity e, bool value) where T : struct, IEnableableComponent
        => manager.SetComponentEnabled<T> (e, value);
}

public class ECBCreator: IEntityCreator
{
    private EntityCommandBuffer commandBuffer;

    public ECBCreator (EntityCommandBuffer commandBuffer) => this.commandBuffer = commandBuffer;

    public void AddComponent<T>(Entity e, T component) where T : unmanaged, IComponentData
        =>commandBuffer.AddComponent(e, component);

    public void AddSharedComponent<T>(Entity e, T component) where T : unmanaged, ISharedComponentData
        => commandBuffer.AddSharedComponent(e,component);

    public Entity CreateEntity() => commandBuffer.CreateEntity();

    public void SetName(Entity entity, FixedString64Bytes name) => commandBuffer.SetName(entity, name);

    public void SetComponentEnabled<T>(Entity e, bool value) where T : struct, IEnableableComponent
        => commandBuffer.SetComponentEnabled<T>(e, value);

    public void Playback(EntityManager manager) => commandBuffer.Playback(manager);

    public void Dispose() => commandBuffer.Dispose();

}

