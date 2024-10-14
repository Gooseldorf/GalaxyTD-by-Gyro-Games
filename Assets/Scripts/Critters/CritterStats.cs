using Sirenix.OdinInspector;
using System;
using UnityEngine;

[Serializable]
public class CritterStats : ScriptableObject
{
    public AllEnums.CritterType CritterType;
    [ValidateInput("@Radius > 0", "Radius could not be 0")]
    public float Radius;
    [ValidateInput("@SearchTargetPositionMaxRadius > 1", "SearchTargetPositionMaxRadius could not be less than 1")]
    public int SearchTargetPositionMaxRadius;
    [ValidateInput("@Speed > 0", "Speed could not be 0")]
    public float Speed;
    [ValidateInput("@RotationSpeed > 1", "RotationSpeed could not be less than 1")]
    public float RotationSpeed;
    [Range(0, 1)]
    public float CleaningQuality;
}