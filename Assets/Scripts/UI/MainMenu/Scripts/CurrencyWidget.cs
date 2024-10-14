using DG.Tweening;
using UnityEngine.UIElements;

namespace UI
{
    public class CurrencyWidget: ClickableVisualElement
    {
        public new class UxmlFactory: UxmlFactory<CurrencyWidget>{}
        
        private Label amountLabel;
        private VisualElement icon;
        private int cachedAmount;
        private AllEnums.CurrencyType currencyType;

        public AllEnums.CurrencyType CurrencyType => currencyType;

        public void Init(AllEnums.CurrencyType currencyType)
        {
            base.Init();
            amountLabel = this.Q<Label>("Amount");
            switch (currencyType)
            {
                case AllEnums.CurrencyType.Soft:
                    cachedAmount = DataManager.Instance.GameData.SoftCurrency;
                    break;
                case AllEnums.CurrencyType.Hard:
                    cachedAmount = DataManager.Instance.GameData.HardCurrency;
                    break;
                case AllEnums.CurrencyType.Scrap:
                    cachedAmount = DataManager.Instance.GameData.Scrap;
                    break;
                default:
                    cachedAmount = 0;
                    break;
            }
            icon = this.Q<VisualElement>("Icon");

            this.currencyType = currencyType;
        }

        public void UpdateValue(int amount)
        {
            DOTween.Kill(this);
            UIHelper.Instance.ChangeBigNumberInLabelTween(amountLabel, cachedAmount, amount, 1).SetTarget(this).OnComplete(() => cachedAmount = amount).Play();
        }
    }
}