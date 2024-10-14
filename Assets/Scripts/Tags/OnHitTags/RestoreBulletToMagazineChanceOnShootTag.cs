using ECSTest.Components;
using I2.Loc;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class RestoreBulletToMagazineChanceOnShootTag : OnShootTag
{
    [SerializeField,Range(0, 100)] private float increaseProbability = 15;
    [SerializeField] private int bulletsCount;

    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        float rand = Random.Range(0f, 100f);

        if (rand <= increaseProbability)
        {
            AttackerComponent component = manager.GetComponentData<AttackerComponent>(tower);
            component.Bullets = math.min(component.Bullets + bulletsCount, component.AttackStats.ReloadStats.MagazineSize);
            manager.SetComponentData(tower, component);
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/RestoreBulletChance")
                                                .Replace("{param1}", increaseProbability+ "<color=#1fb2de>%</color>")
                                                .Replace("{param2}", bulletsCount.ToString());
}