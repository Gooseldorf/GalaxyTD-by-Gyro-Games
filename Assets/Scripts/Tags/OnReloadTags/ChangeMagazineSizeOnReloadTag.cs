using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class ChangeMagazineSizeOnReloadTag : OnReloadTag
{
    [SerializeField, InfoBox("Absolute number. Positive to increase MagazineSize, negative decrease")] private int changeSize = 1;

    public override void OnReload(Entity tower, EntityManager manager)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        attackerComponent.AttackStats.ReloadStats.RawMagazineSize = math.max(attackerComponent.AttackStats.ReloadStats.RawMagazineSize + changeSize, 1);
        
        manager.SetComponentData(tower, attackerComponent);
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/ChangeMagazineSize")
                                                .Replace("{param}", changeSize.ToString());
}