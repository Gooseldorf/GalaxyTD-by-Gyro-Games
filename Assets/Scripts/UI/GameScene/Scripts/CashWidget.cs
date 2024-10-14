using CardTD.Utilities;
using DG.Tweening;
using ECSTest.Components;
using I2.Loc;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class CashWidget : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<CashWidget>
        {
        }

        private const float lowCashPercent = 0.05f;
        private const float highCashPercent = 0.2f;
        private const float updateTimeThreshold = 0.2f;

        private Label cashLabel;
        private int previousCash;
        private int currentCash;
        private Tween cashLabelTween;

        private Label titleLabel;
        private VisualElement progressBar;
        private VisualElement progressBarFilling;
        private float lastProgressBarPercent = 100;
        private Tween progressBarTween;

        private VisualElement gainLabelsContainer;
        private Queue<Label> gainLabels;
        private int cumulativeCashDelta = 0;
        private float lastUpdateTime;
        private bool showCashForWave = false;

        private UIHelper uiHelper;
        private CashWidgetAnimationData animData;

        public ClickableVisualElement CheatButton;

        public void Init()
        {
            titleLabel = this.Q<Label>("TitleLabel");
            cashLabel = this.Q<Label>("CashLabel");
            CashComponent cashComponent = GameServices.Instance.GetCashComponent();
            cashLabel.text = cashComponent.Cash.ToString();
            previousCash = cashComponent.Cash;
            currentCash = previousCash;

            gainLabelsContainer = this.Q<VisualElement>("GainLabelsContainer");
            List<Label> gainLabelsList = this.Query<Label>("GainLabel").ToList();
            gainLabels = new Queue<Label>(gainLabelsList);

            progressBar = this.Q<VisualElement>("CashProgressBar");
            progressBarFilling = progressBar.Q<VisualElement>("Filling");

            CheatButton = this.Q<ClickableVisualElement>("CheatButton");

            uiHelper = UIHelper.Instance;
            animData = uiHelper.CashWidgetAnimationData;
            progressBarTween = uiHelper.ChangeWidthByPercent(progressBar, 0, 100, 3).SetUpdate(true).Play();

            Messenger<int, bool>.AddListener(GameEvents.CashUpdated, OnCashChanged);
        }

        public void Dispose()
        {
            Messenger<int, bool>.RemoveListener(GameEvents.CashUpdated, OnCashChanged);
        }

        public void Reset()
        {
            CashComponent cashComponent = GameServices.Instance.GetCashComponent();
            cashLabelTween?.Kill(true);
            AnimateCashLabel(previousCash, cashComponent.Cash);
            previousCash = cashComponent.Cash;
            currentCash = previousCash;
            cashLabel.text = previousCash.ToString();
            cumulativeCashDelta = 0;
            lastProgressBarPercent = 100;
            progressBarTween = uiHelper.ChangeWidthByPercent(progressBar, 0, 100, 2).SetUpdate(true).Play();
            progressBarFilling.style.backgroundColor = uiHelper.CashWidgetBlue;
        }

        private void OnCashChanged(int cash, bool cashForWave)
        {
            cumulativeCashDelta += cash - currentCash;
            currentCash = cash;
            showCashForWave = cashForWave;
        }

        public void Update()
        {
            if (Time.unscaledTime - lastUpdateTime > updateTimeThreshold && cumulativeCashDelta != 0)
            {
                currentCash = Mathf.Max(previousCash + cumulativeCashDelta, 0);
                AnimateCashLabel(previousCash, currentCash);
                previousCash = currentCash;
                AnimateGainLabel(cumulativeCashDelta);
                UpdateProgressBar();
                lastUpdateTime = Time.unscaledTime;
                cumulativeCashDelta = 0;
            }
        }

        private void UpdateProgressBar()
        {
            CashComponent cashComponent = GameServices.Instance.GetCashComponent();
            float min = cashComponent.CashsToReloadingForMin * lowCashPercent;
            float max = cashComponent.CashsToReloadingForMin * highCashPercent;

            int percent = (!GameServices.Instance.UseBulletCost) ? 100 : cashComponent.CashsToReloadingForMin > 0 ? (int)(((cashComponent.Cash / (max + min)) * 100)) : 100;

            progressBarFilling.style.backgroundColor = percent switch
            {
                < 33 => new StyleColor(uiHelper.CashWidgetRed),
                > 33 and < 66 => new StyleColor(uiHelper.CashWidgetYellow),
                _ => new StyleColor(uiHelper.CashWidgetBlue)
            };
            progressBarTween?.Kill(true);
            progressBarTween = uiHelper.ChangeWidthByPercent(progressBar, lastProgressBarPercent, percent, animData.ProgressBarChangeTime).OnComplete(() => lastProgressBarPercent = percent)
                .SetUpdate(true).Play();
        }

        private void AnimateGainLabel(int gain)
        {
            Label label = GetNextGainLabel();
            label.style.color = gain >= 0 ? UIHelper.Instance.Green : UIHelper.Instance.Red;
            label.text = gain > 0 ? $"+{gain.ToString()}" : gain.ToString();
            label.style.scale = showCashForWave ? new StyleScale(new Vector2(1.2f, 1.2f)) : new StyleScale(Vector2.one);

            if (showCashForWave)
            {
                label.style.color = uiHelper.CashWidgetBlue;
                showCashForWave = false;
            }

            Sequence gainLabelSeq = DOTween.Sequence();

            gainLabelSeq.Append(uiHelper.FadeTween(label, 0, 1, animData.GainLabelFadeTime));
            gainLabelSeq.AppendInterval(animData.GainLabelIdleTime);
            gainLabelSeq.Append(uiHelper.TranslateYTween(label, 0, animData.GainLabelTranslateYValue, animData.GainLabelTranslateTime));
            gainLabelSeq.Insert(animData.GainLabelFadeTime + animData.GainLabelIdleTime, uiHelper.FadeTween(label, 1, 0, animData.GainLabelFadeTime));

            gainLabelSeq.SetUpdate(true).Play();
        }

        private void AnimateCashLabel(int from, int to)
        {
            cashLabelTween?.Kill(true);
            cashLabelTween = uiHelper.ChangeNumberInLabelTween(cashLabel, from, to, animData.CashLabelChangeTime).SetUpdate(true);
        }

        private Label GetNextGainLabel()
        {
            Label result = gainLabels.Dequeue();
            result.transform.position = gainLabelsContainer.transform.position;
            result.BringToFront();

            gainLabels.Enqueue(result);

            return result;
        }

        public void UpdateLocalization()
        {
            titleLabel.text = LocalizationManager.GetTranslation("GameScene/Supply");
        }
    }
}