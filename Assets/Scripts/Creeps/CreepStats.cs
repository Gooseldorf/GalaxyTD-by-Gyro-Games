using System;
using Sirenix.OdinInspector;
using UnityEngine;
using static AllEnums;

[Serializable]
[CreateAssetMenu(fileName = "NewCreepStats", menuName = "ScriptableObjects/CreepStats")]
public class CreepStats : ScriptableObject, ILocalized
{
    public AllEnums.CreepType CreepType;

    public int CashReward;
    public float Mass;
    public float CollisionRange;
    public ObstacleType ObstacleType = ObstacleType.OnlyPenetrate;

    [ValidateInput("@MaxHP > 0", "Max HP could not be 0")]
    public float MaxHP;

    //[ValidateInput("@Speed > 0", "Speed could not be 0")]

    public float Speed;

    public float MaxForce;
    public float NeighborRange;

    public ArmorType ArmorType;
    public FleshType FleshType;

    /// <summary>
    /// Danger of Creep in Hp it moves per second => For Balance analysis
    /// </summary>
    [ShowInInspector]
    public float Danger => MaxHP * Speed;

    public string SerializedID => name;

    [SerializeField] public CreepRenderStats RenderStats;

    [Button]
    public string GetTitle() => ""; // Localizer.GetTranslation($"Enemies/{name}_title");

    [Button]
    public string GetDescription() => ""; //=> Localizer.GetTranslation($"Enemies/{name}_desc");

    public float GetCreepActualHp(CreepStats creepStats)
    {
        return creepStats.MaxHP;
    }

    public float GetCreepActualHp() => GetCreepActualHp(this);

    //public AllEnums.ObstacleType ObstacleType = AllEnums.ObstacleType.OnlyPenetrate;

    public CreepStatsConfig GetConfig()
    {
        CreepStatsConfig result = new();
        result.CreepType = this.CreepType;
        result.MaxHP = this.MaxHP;
        result.Mass = this.Mass;
        result.CollisionRange = CollisionRange;
        result.Speed = this.Speed;
        result.CashReward = this.CashReward;
        result.MaxForce = this.MaxForce;
        result.NeighborRange = this.NeighborRange;
        result.ObstacleType = this.ObstacleType;

        return result;
    }
}

public struct CreepStatsConfig
{
    public float MaxHP;
    public float Mass;
    public float CollisionRange;
    public float NeighborRange;
    public float Speed;
    public float MaxForce;
    public CreepType CreepType;
    public int CashReward;
    public ObstacleType ObstacleType;
}