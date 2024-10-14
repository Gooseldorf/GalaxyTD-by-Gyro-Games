using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class CashToDamageOnShootTag : OnShootTag
{
    [SerializeField, BoxGroup("Bounds")] private int minCashBound = 0;
    [SerializeField, BoxGroup("Bounds")] private int maxCashBound = 1000;
    [SerializeField, BoxGroup("Bounds")] private float minPercentBound = 0f;
    [SerializeField, BoxGroup("Bounds")] private float maxPercentBound = 1f;

    [SerializeField] private bool isDirectDependency = true;

    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        CashComponent cashComponent = GameServices.Instance.GetCashComponent(manager);
        
        float lowerValue = isDirectDependency ? minPercentBound : maxPercentBound;
        float higherValue = isDirectDependency ? maxPercentBound : minPercentBound;

        float percent = Utilities.GetLerpedValue(minCashBound, maxCashBound, lowerValue, higherValue, cashComponent.Cash);
        
        foreach (Entity entity in dynamicBuffer)
        {
            ProjectileComponent projectileComponent = manager.GetComponentData<ProjectileComponent>(entity);

            projectileComponent.Damage *= (1 + percent);
            
            manager.SetComponentData(entity, projectileComponent);
        }
    }

    public override string GetDescription() => 
        LocalizationManager.GetTranslation(isDirectDependency ? "Tags/CashToDamage": "Tags/CashToDamageInvert")
                        .Replace("{param1}", isDirectDependency ? maxCashBound.ToString(): minCashBound.ToString())
                        .Replace("{param2}", (maxPercentBound * 100).ToString() + "<color=#1fb2de>%</color> ");
}