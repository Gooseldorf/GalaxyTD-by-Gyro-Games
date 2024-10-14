using CardTD.Utilities;
using DG.Tweening;
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class LineStats : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<LineStats>
        {
        }

        private Label statsName;
        private Label statsLabel;
        private Label bonusLabel;
        private VisualElement statIcon;
        private VisualElement lineContainer;
        private VisualElement lineFg;
        private VisualElement bonusPart;
        private VisualElement minusPart;

        private float maxValue;
        private float minValue;
        private bool isInverted;
        private const float resolvedContainerWidth = 151; //TODO: Try to resolve lineContainer before UpdateMenuStats() called!!


        private const float tolerance = 0.001f;
        private float animationDuration;
        private float currentValueWidth = 0;
        private float currentBonusWidth = 0;
        private float currentMinusWidth = 0;

        private UIHelper uiHelper;

        public void Init(Sprite statSprite, float statMinValue, float statMaxValue)
        {
            statsName = this.Q<Label>("StatsName");
            statsLabel = this.Q<Label>("StatsLabel");
            bonusLabel = this.Q<Label>("BonusLabel");
            statIcon = this.Q<VisualElement>("StatIcon");
            statIcon.style.backgroundImage = new StyleBackground(statSprite);
            lineContainer = this.Q<VisualElement>("LineContainer");
            lineFg = this.Q<VisualElement>("LineFg");
            bonusPart = this.Q<VisualElement>("BonusPart");
            minusPart = this.Q<VisualElement>("MinusPart");

            if (math.abs(math.abs(statMaxValue) - math.abs(statMinValue)) < tolerance)
                Debug.LogError($"{this.parent.name}: Max and min values are equal! Set up correct values in UIHelper.StatsWidgetData!");

            minValue = statMinValue;
            maxValue = statMaxValue;
            isInverted = statMinValue > statMaxValue;

            uiHelper = UIHelper.Instance;
            animationDuration = uiHelper.StatsWidgetData.LineStatChangeTime;
        }

        public void UpdateMenuStats(float value, float bonus, bool roundValue = false)
        {
            bonusLabel.style.display = DisplayStyle.None;
            KillTweens();
            Sequence seq = DOTween.Sequence();
            float valueInterval = math.abs(maxValue - minValue);
            float pixelWight = resolvedContainerWidth / valueInterval;

            float valueWidth;
            float bonusWidth;
            if (!isInverted)
            {
                valueWidth = value * pixelWight;
                bonusWidth = math.abs(bonus * pixelWight);

                if (math.abs(currentValueWidth - valueWidth) > tolerance)
                {
                    uiHelper.ChangeWidth(lineFg, currentValueWidth, valueWidth, animationDuration);
                    currentValueWidth = valueWidth;
                }
                if (bonus >= 0)
                {
                    if (currentMinusWidth != 0)
                    {
                        seq.Append(uiHelper.ChangeWidth(minusPart, currentMinusWidth, 0, animationDuration));
                        currentMinusWidth = 0;
                    }

                    if (math.abs(currentBonusWidth - bonusWidth) > tolerance)
                    {
                        seq.Append(uiHelper.ChangeWidth(bonusPart, currentBonusWidth, bonusWidth, animationDuration));
                        currentBonusWidth = bonusWidth;
                    }
                }
                else
                {
                    if (currentBonusWidth != 0)
                    {
                        seq.Append(uiHelper.ChangeWidth(bonusPart, currentBonusWidth, 0, animationDuration));
                        currentBonusWidth = 0;
                    }

                    if (math.abs(currentMinusWidth - bonusWidth) > tolerance)
                    {
                        seq.Append(uiHelper.ChangeWidth(minusPart, math.abs(currentMinusWidth), bonusWidth, animationDuration));
                        currentMinusWidth = bonusWidth;
                    }
                }
            }
            else
            {
                valueWidth = (minValue - value) * pixelWight;
                bonusWidth = math.abs(bonus * pixelWight);

                if (math.abs(currentValueWidth - valueWidth) > tolerance)
                {
                    uiHelper.ChangeWidth(lineFg, currentValueWidth, valueWidth, animationDuration);
                    currentValueWidth = valueWidth;
                }

                if (bonus <= 0)
                {
                    if (currentMinusWidth != 0)
                    {
                        seq.Append(uiHelper.ChangeWidth(minusPart, currentMinusWidth, 0, animationDuration));
                        currentMinusWidth = 0;
                    }

                    if (math.abs(currentBonusWidth - bonusWidth) > tolerance)
                    {
                        seq.Append(uiHelper.ChangeWidth(bonusPart, currentBonusWidth, bonusWidth, animationDuration));
                        currentBonusWidth = bonusWidth;
                    }
                }
                else
                {
                    if (currentBonusWidth != 0)
                    {
                        seq.Append(uiHelper.ChangeWidth(bonusPart, currentBonusWidth, 0, animationDuration));
                        currentBonusWidth = 0;
                    }

                    if (math.abs(currentMinusWidth - bonusWidth) > tolerance)
                    {
                        seq.Append(uiHelper.ChangeWidth(minusPart, currentMinusWidth, bonusWidth, animationDuration));
                        currentMinusWidth = bonusWidth;
                    }
                }
            }

            seq.SetUpdate(true).SetTarget(this).Play();

            float referenceValue = isInverted ? maxValue : minValue;
            statsLabel.text = roundValue ?
                $"{((int)math.max(value + bonus, referenceValue)).ToStringBigValue()}" :
                $"{math.max(value + bonus, referenceValue).ToStringBigValue()}";
        }

        private void KillTweens()
        {
            DOTween.Kill(lineFg);
            DOTween.Kill(bonusPart);
            DOTween.Kill(minusPart);
            DOTween.Kill(this);
        }

        public void UpdateGameStats(float currentValue, bool roundValue = false)
        {
            minusPart.style.display = DisplayStyle.None;
            bonusPart.style.display = DisplayStyle.None;

            float valueDelta;

            if (!isInverted)
            {
                valueDelta = maxValue - minValue;
                lineFg.style.width = Length.Percent(currentValue / valueDelta * 100);
            }
            else
            {
                valueDelta = minValue - maxValue;
                lineFg.style.width = Length.Percent((valueDelta - currentValue) / valueDelta * 100);
            }

            statsLabel.text = roundValue ? $"{((int)currentValue).ToStringBigValue()}" : $"{currentValue.ToStringBigValue()}";
        }

        public void UpdateNextStats(float currentValue, float nextValue, bool roundValue = false)
        {
            if (!isInverted)
            {
                bonusLabel.style.color = nextValue >= 0 ? new StyleColor(uiHelper.LineStatGreen) : new StyleColor(uiHelper.Red);
            }
            else
            {
                bonusLabel.style.color = nextValue <= 0 ? new StyleColor(uiHelper.LineStatGreen) : new StyleColor(uiHelper.Red);
            }

            bonusLabel.style.display = nextValue == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            float value = (currentValue * nextValue);
            bonusLabel.text = value > 0 ? "(+" : "(-";
            bonusLabel.text += (roundValue ? ((int)math.abs(value)).ToStringBigValue() : math.abs(value).ToStringBigValue()) + ")";
        }

        public void HideBonus() => bonusLabel.style.display = DisplayStyle.None;

        public void SetAsSeconds() => statsLabel.text += "s";

        public void SetStatName(string statName) => statsName.text = statName;

    }
}