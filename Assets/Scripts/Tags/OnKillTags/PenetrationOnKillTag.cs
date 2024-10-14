using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class PenetrationOnKillTag : OnKillTag
{
    [SerializeField, InfoBox("Percents"), Range(0, 100)] private float increaseProbability = 5;
    [SerializeField, InfoBox("Absolute number. Positive for increase PenetrationCount, negative decrease")] private int increaseCount = 1;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        float rand = Random.Range(0f, 100f);
        
        if (rand <= increaseProbability)
        {
            if(!handler.Exist())
                return;
            GunStatsComponent gunStatsComponent = handler.Manager.GetComponentData<GunStatsComponent>(handler.Tower);
            gunStatsComponent.PenetrationCount += increaseCount;
            handler.Manager.SetComponentData(handler.Tower, gunStatsComponent);
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/PenetrationOnKill")
                                                .Replace("{param}", increaseProbability+ "<color=#1fb2de>%</color>");
}