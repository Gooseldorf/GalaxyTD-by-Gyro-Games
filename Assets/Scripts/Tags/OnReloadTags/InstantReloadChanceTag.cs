using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class InstantReloadChanceTag : OnReloadTag
{
    [SerializeField, InfoBox("Percents"), Range(0, 100)] private float instantReloadProbability = 50f;

    public override void OnReload(Entity tower, EntityManager manager)
    {
        float rand = Random.Range(0f, 100f);

        if (rand <= instantReloadProbability)
        {
            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);

            attackerComponent.ReloadTimer = 0;
            
            manager.SetComponentData(tower, attackerComponent);
        }
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/InstantReload")
                                                .Replace("{param}", instantReloadProbability+ "<color=#1fb2de>%</color>");
}