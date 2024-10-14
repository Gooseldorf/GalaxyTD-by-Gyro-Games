using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class TeleportationInAreaOnKillTag : OnKillTag
{
    [SerializeField, BoxGroup("Bounds")] private float lowerDistance = 1f;
    [SerializeField, BoxGroup("Bounds")] private float topDistance = 4f;
    [SerializeField, BoxGroup("Bounds")] private float minBound = 0f;
    [SerializeField, BoxGroup("Bounds")] private float maxBound = 1f;
    [SerializeField, InfoBox("Iterations to find position for teleportation")] private int iterations = 4;
    [SerializeField] private float range = 10f;
    
    private OutFlowField outFlowField;
    
    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        float overkillDamage = -creepComponent.Hp;
        float percentFromHp = overkillDamage / creepComponent.MaxHp;
        float teleportationDistance = Utilities.GetLerpedValue(minBound, maxBound, lowerDistance, topDistance, percentFromHp);
        
        EntityQuery entityQuery = handler.Manager.CreateEntityQuery(new ComponentType[] {typeof(OutFlowField)});
        outFlowField = entityQuery.GetSingleton<OutFlowField>();
        
        handler.AoeEffectOnKill(range, (creepInfo) =>
        {
            Entity creepEntity = creepInfo.Entity;
            
            AdjustNewPosition(creepEntity, handler, teleportationDistance);
        });
        
        //TODO need something like event for aoe visual
    }

    private void AdjustNewPosition(Entity creepEntity, OnKillData handler, float teleportationDistance)
    {
        PositionComponent creepPositionComponent = handler.Manager.GetComponentData<PositionComponent>(creepEntity);;

        TeleportationOnHitTag.SetUpTeleportation(iterations, outFlowField, creepPositionComponent, teleportationDistance, creepEntity, handler.Manager);
    }
    
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/TeleportationInAreaOnKill")
                                                .Replace("{param}", range.ToString());
}