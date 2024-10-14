using Unity.Mathematics;
using UnityEngine;
using static AllEnums;

public class SpawnZonePartialVisual : EnvironmentVisual
{
    [SerializeField] private Sprite combinedZoneSprite;
    [SerializeField] private SpriteRenderer creepTypeIcon;

    private CreepType currentCreepType;

    public bool IsCombinedZone;

    public void SetCombinedZoneSprite()
    {
        icon.sprite = combinedZoneSprite;
        IsCombinedZone = true;
    }

    public void InitSpawnZoneVisual(float2 position, bool isCombined)
    {
        transform.position = new float3(position, 0);
        if (isCombined)
            SetCombinedZoneSprite();
    }

    public void UpdateIncomingCreeps(AllEnums.CreepType creepType, int count)
    {
        if (count > 0)
        {
            if (currentCreepType != creepType || creepTypeIcon.sprite == null)
            {
                creepTypeIcon.sprite = GameServices.Instance.Get<SimpleEffectManager>().CreepIcons[creepType];
                currentCreepType = creepType;
            }
        }
        else if(creepTypeIcon.sprite != null)
            creepTypeIcon.sprite = null;
    }
}