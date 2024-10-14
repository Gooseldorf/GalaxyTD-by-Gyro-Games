using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public class MovementSettingsSO: ScriptableObject
{
    public float FlowFieldForceMultiplier = 1;
    [FoldoutGroup("ShortRange")][Range(0,10)] public float CollisionRangeMultiplier = 1;
    [FoldoutGroup("ShortRange")][Range(0,10)] public float EvasionForceMultiplier = 1;
    [FoldoutGroup("ShortRange")][Range(0,10)] public float WallsCollisionRangeMultiplier = 1;
    [FoldoutGroup("ShortRange")] [Range(0, 100)] public float WallsPushForceMultiplier = 10;
    
    [FoldoutGroup("LongRange")][Range(0,10)] public float NeighbourRangeMultiplier = 1;
    [FoldoutGroup("LongRange")][Range(0,10)] public float CollisionAvoidanceForceMultiplier = 1;
    [FoldoutGroup("LongRange")][Range(0,10)] public float SeparationForceMultiplier = 1;
    [FoldoutGroup("LongRange")][Range(0,10)]public float AlignmentForceMultiplier = 1;
    [FoldoutGroup("LongRange")][Range(0,10)] public float CohesionForceMultiplier = 1;
}

public struct MovementSettings : IComponentData
{
    public float CollisionRangeMultiplier;
    public float EvasionForceMultiplier;
    public float WallsCollisionRangeMultiplier;
    public float WallsPushForceMultiplier;
    
    public float CollisionAvoidanceForceMultiplier;
    public float SeparationForceMultiplier;
    public float AlignmentForceMultiplier;
    public float CohesionForceMultiplier;
}
