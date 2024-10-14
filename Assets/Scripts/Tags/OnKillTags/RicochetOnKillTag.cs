using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class RicochetOnKillTag : OnKillTag
{
    [SerializeField, InfoBox("Percents"), Range(0, 100)] private float increaseProbability = 5;
    [SerializeField, InfoBox("Absolute number. Positive to increase, negative decrease")] private int increaseCount = 1;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        float rand = Random.Range(0f, 100f);
        
        if (rand <= increaseProbability)
        {
            if(!handler.Exist())
                return;
            GunStatsComponent component = handler.Manager.GetComponentData<GunStatsComponent>(handler.Tower);
            component.RicochetCount += increaseCount;
            handler.Manager.SetComponentData(handler.Tower, component);
        }
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/RicochetOnKill")
                                                .Replace("{param}", increaseProbability + "<color=#1fb2de>%</color>");
}