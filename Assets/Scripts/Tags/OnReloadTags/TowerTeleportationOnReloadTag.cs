using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

public sealed class TowerTeleportationOnReloadTag : OnReloadTag
{
    public override void OnReload(Entity tower, EntityManager manager)
    {
        EntityQuery dropZonesQuery = manager.CreateEntityQuery(new ComponentType[]
        {
            typeof(DropZoneComponent), typeof(GridPositionComponent),  typeof(PowerableComponent)
        });
        
        NativeArray<PowerableComponent> powerables =  dropZonesQuery.ToComponentDataArray<PowerableComponent>(Allocator.Temp);
        NativeArray<DropZoneComponent> dropZoneComponents =  dropZonesQuery.ToComponentDataArray<DropZoneComponent>(Allocator.Temp);
        NativeArray<Entity> entities = dropZonesQuery.ToEntityArray(Allocator.Temp);
        NativeArray<GridPositionComponent> gridPositionComponents = dropZonesQuery.ToComponentDataArray<GridPositionComponent>(Allocator.Temp);
        
        GetRandomIndices(entities.Length, out List<int> indices);
        
        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            if(dropZoneComponents[index].IsOccupied) continue;
            if(!dropZoneComponents[index].IsPossibleToBuild) continue;
            if(!powerables[index].IsTurnedOn) continue;
            
            RefRW<BaseFlowField> cellsHolder = GameServices.Instance.GetBaseFlowField();
            
            AdjustOldDropzone(manager, tower, cellsHolder);
            AdjustNewDropzone(manager, tower, cellsHolder, entities[index], gridPositionComponents[index]);
            
            Messenger<Entity>.Broadcast(GameEvents.TowerTeleported, entities[index], MessengerMode.DONT_REQUIRE_LISTENER);
            
            break;
        }

        powerables.Dispose();
        dropZoneComponents.Dispose();
        entities.Dispose();
        gridPositionComponents.Dispose();
    }

    private void AdjustOldDropzone(EntityManager manager, Entity tower, RefRW<BaseFlowField> cellsHolder)
    {
        Entity oldDropZoneEntity = manager.GetComponentData<EntityHolderComponent>(tower).Entity;
        EntityHolderComponent oldHolderComponent = manager.GetComponentData<EntityHolderComponent>(oldDropZoneEntity);
        oldHolderComponent.Entity = Entity.Null;
        manager.SetComponentData(oldDropZoneEntity, oldHolderComponent);
        DropZoneComponent oldDropZoneComponent = manager.GetComponentData<DropZoneComponent>(oldDropZoneEntity);
        oldDropZoneComponent.IsOccupied = false;
        oldDropZoneComponent.IsPossibleToBuild = true;
        manager.SetComponentData(oldDropZoneEntity, oldDropZoneComponent);
        GridPositionComponent oldGridPosition = manager.GetComponentData<GridPositionComponent>(tower);
        cellsHolder.ValueRW.ChangeFlowField(oldGridPosition.Value, false);
        Messenger<DropZoneComponent,GridPositionComponent,bool>.Broadcast(GameEvents.DropZoneStateChanged, oldDropZoneComponent, oldGridPosition, false, MessengerMode.DONT_REQUIRE_LISTENER);
        Entity dropZoneEvent = manager.CreateEntity();
        manager.SetName(dropZoneEvent, nameof(DropZoneEvent));
        manager.AddComponentData(dropZoneEvent, new DropZoneEvent() { Entity = oldDropZoneEntity });
    }

    private void AdjustNewDropzone(EntityManager manager, Entity tower, RefRW<BaseFlowField> cellsHolder, Entity dropzoneEntity, GridPositionComponent gridPositionComponent)
    {
        DropZoneComponent dropZoneComponent = GameServices.SetDropZoneOccupied(ref manager, dropzoneEntity, tower);
        PositionComponent positionComponent = manager.GetComponentData<PositionComponent>(dropzoneEntity);
        manager.SetComponentData(tower, positionComponent);
        manager.SetComponentData(tower, gridPositionComponent);
        manager.SetComponentData(tower, manager.GetComponentData<Identifiable>(dropzoneEntity));
        GameServices.Instance.CreateObstacle(ref manager, gridPositionComponent, tower);
        manager.SetComponentData(tower, new EntityHolderComponent {Entity = dropzoneEntity});
        cellsHolder.ValueRW.ChangeFlowField(gridPositionComponent.Value, true);
        GameServices.Instance.CreateBaseFieldUpdateEvent();
        Messenger<DropZoneComponent,GridPositionComponent,bool>.Broadcast(GameEvents.DropZoneStateChanged, dropZoneComponent, gridPositionComponent,true,MessengerMode.DONT_REQUIRE_LISTENER);
    }

    private void GetRandomIndices(int count, out List<int> indices)
    {
        Random rng = new Random();
        indices = new List<int>();

        for (int i = 0; i < count; i++)
            indices.Add(i);

        int n = indices.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            (indices[k], indices[i]) = (indices[i], indices[k]);
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/TowerTeleportationOnReload");
}