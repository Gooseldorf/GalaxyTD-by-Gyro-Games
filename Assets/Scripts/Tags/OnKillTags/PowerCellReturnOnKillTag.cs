using ECSTest.Components;
using I2.Loc;
using Unity.Collections;
using Unity.Entities;

public sealed class PowerCellReturnOnKillTag : OnKillTag
{
    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        NativeArray<Entity> powerCells = handler.Manager.CreateEntityQuery(new ComponentType[] {typeof(PowerCellComponent)}).ToEntityArray(Allocator.Temp);

        foreach (Entity powerCell in powerCells)
        {
            DestroyComponent destroyComponent = handler.Manager.GetComponentData<DestroyComponent>(powerCell);
            
            if(destroyComponent.IsNeedToDestroy) continue;
            
            PowerCellComponent powerCellComponent = handler.Manager.GetComponentData<PowerCellComponent>(powerCell);
            
            if (!powerCellComponent.IsMoves) continue;

            if (powerCellComponent.Creep.Equals(handler.Creep))
            {
                TimerComponent timer = handler.Manager.GetComponentData<TimerComponent>(powerCell);
                timer.Timer = 0;
                powerCellComponent.Detach();
                handler.Manager.SetComponentData(powerCell, timer);
                handler.Manager.SetComponentData(powerCell, powerCellComponent);
            }
        }

        powerCells.Dispose();
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/PowerCellReturn");
}