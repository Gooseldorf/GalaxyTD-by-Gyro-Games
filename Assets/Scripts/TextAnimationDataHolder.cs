using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace DefaultNamespace
{
    public class TextAnimationDataHolder : ScriptableObject
    {
        [FoldoutGroup("DamageText")] public float DamagePopUpTime;
        [FoldoutGroup("DamageText")] public float DamagePopUpYStartOffset;
        [FoldoutGroup("DamageText")] public float DamagePopUpYFinishOffset;
        [FoldoutGroup("DamageText")] public float DamageTextStartScale;
        [FoldoutGroup("DamageText")] public float DamageTextFinishScale;
        [FoldoutGroup("DamageText")] public float DamageIdleTime;
        [FoldoutGroup("DamageText")] public float DamageFadingTime;
        [FoldoutGroup("DamageText")] public float4 DamageTextColor = new float4(1, 0, 0, 1);

        [FoldoutGroup("CashText")] public float CashPopUpTime;
        [FoldoutGroup("CashText")] public float CashPopUpXStartOffset;
        [FoldoutGroup("CashText")] public float CashPopUpYStartOffset;
        [FoldoutGroup("CashText")] public float CashPopUpYFinishOffset;
        [FoldoutGroup("CashText")] public float CashTextStartScale;
        [FoldoutGroup("CashText")] public float CashTextFinishScale;
        [FoldoutGroup("CashText")] public float CashIdleTime;
        [FoldoutGroup("CashText")] public float CashFadingTime;
        [FoldoutGroup("CashText")] public float4 SubtractCashTextColor = new float4(1, 1, 0, 1);
        [FoldoutGroup("CashText")] public float4 AddCashTextColor = new float4(0, 1, 0, 1);


        [FoldoutGroup("DrZTimerText")] public float DropZoneTextIdleTime;
        [FoldoutGroup("DrZTimerText")] public float DropZoneTextFadingTime;

        [FoldoutGroup("DrZTimerText")] public float DropZoneXStartOffset;
        [FoldoutGroup("DrZTimerText")] public float DropZoneYStartOffset;
        [FoldoutGroup("DrZTimerText")] public float DropZoneStartScale;
        [FoldoutGroup("DrZTimerText")] public float4 DropZoneTextColor = new float4(1, 0, 0, 1);
        [FoldoutGroup("DrZTimerText")] public float SpawnZoneXStartOffset;
        [FoldoutGroup("DrZTimerText")] public float SpawnZoneYStartOffset;
        [FoldoutGroup("DrZTimerText")] public float SpawnZoneStartScale;
        [FoldoutGroup("DrZTimerText")] public float4 SpawnZoneTextColor = new float4(1, 1, 1, 1);

        public TextAnimationData GetTextAnimationData => new TextAnimationData
        {
            DamagePopUpTime = this.DamagePopUpTime,
            DamagePopUpSpeed = (DamagePopUpYFinishOffset - DamagePopUpYStartOffset) / DamagePopUpTime,
            DamageScaleSpeed = (DamageTextFinishScale - DamageTextStartScale) / DamagePopUpTime,
            DamageIdleTime = this.DamageIdleTime,
            DamageFadingTime = this.DamageFadingTime,
            DamageTextColor = this.DamageTextColor,

            CashPopUpTime = this.CashPopUpTime,
            CashPopUpSpeed = (CashPopUpYFinishOffset - CashPopUpYStartOffset) / CashPopUpTime,
            CashScaleSpeed = (CashTextFinishScale - CashTextStartScale) / CashPopUpTime,
            CashIdleTime = this.CashIdleTime,
            CashFadingTime = this.CashFadingTime,
            SubtractCashTextColor = this.SubtractCashTextColor,
            AddCashTextColor = this.AddCashTextColor,


            DropZoneTextIdleTime = this.DropZoneTextIdleTime,
            DropZoneTextFadingTime = this.DropZoneTextFadingTime,
            DropZoneXStartOffset = this.DropZoneXStartOffset,
            DropZoneYStartOffset = this.DropZoneYStartOffset,
            DropZoneScale = DropZoneStartScale,
            DropZoneTextColor = this.DropZoneTextColor,
            SpawnZoneXStartOffset = this.SpawnZoneXStartOffset,
            SpawnZoneYStartOffset = this.SpawnZoneYStartOffset,
            SpawnZoneScale = SpawnZoneStartScale,
            SpawnZoneTextColor = this.SpawnZoneTextColor
        };
    }


    public struct TextAnimationData
    {
        public float DamagePopUpTime;
        public float DamagePopUpSpeed;
        public float DamageScaleSpeed;
        public float DamageIdleTime;
        public float DamageFadingTime;
        public float4 DamageTextColor;

        public float CashPopUpTime;
        public float CashPopUpSpeed;
        public float CashScaleSpeed;
        public float CashIdleTime;
        public float CashFadingTime;
        public float4 SubtractCashTextColor;
        public float4 AddCashTextColor;


        public float DropZoneTextIdleTime;
        public float DropZoneTextFadingTime;
        public float DropZoneXStartOffset;
        public float DropZoneYStartOffset;
        public float DropZoneScale;
        public float4 DropZoneTextColor;
        public float SpawnZoneXStartOffset;
        public float SpawnZoneYStartOffset;
        public float SpawnZoneScale;
        public float4 SpawnZoneTextColor;

        public float TotalDamageAnimationTime => DamagePopUpTime + DamageIdleTime + DamageFadingTime;
        public float TotalCashAnimationTime => CashPopUpTime + CashIdleTime + CashFadingTime;
        public float TotalDropZoneAnimationTime => DropZoneTextIdleTime + DropZoneTextFadingTime;
    }
}