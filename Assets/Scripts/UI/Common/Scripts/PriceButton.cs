using CardTD.Utilities;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;
using static AllEnums;

namespace UI
{
    public class PriceButton : CommonButton
    {
        public new class UxmlFactory : UxmlFactory<PriceButton>{}

        private Label priceLabel;
        private VisualElement currencyIcon;
        private int price;
        
        public override void Init()
        {
            base.Init();
            priceLabel = this.Q<Label>("Price");
            currencyIcon = this.Q<VisualElement>("Icon");
        }

        public void SetPrice(int price)
        {
            if(priceLabel == null) return;
            priceLabel.text = price.ToStringBigValue();
            this.price = price;
        }

        public void SetPrice(string price)
        {
            if(priceLabel == null) return;
            this.priceLabel.text = price;
        }

        public override void SetState(UIState state)
        {
            base.SetState(state);
            switch (state)
            {
                case UIState.Locked:
                    priceLabel.style.display = DisplayStyle.None;
                    SetText(LocalizationManager.GetTranslation("Locked"));
                    currencyIcon.style.display = DisplayStyle.None;
                    break;
                case UIState.Unavailable:
                    priceLabel.style.display = DisplayStyle.Flex;
                    priceLabel.text = price.ToString();
                    priceLabel.style.color = new StyleColor(Color.gray);
                    if (label != null) label.style.color = new StyleColor(Color.gray);
                    if (currencyIcon != null)currencyIcon.style.display = DisplayStyle.Flex;
                    SetBackground(UIHelper.Instance.LockedCommonButtonBackground);
                    break;
                case UIState.Available:
                    priceLabel.style.display = DisplayStyle.Flex;
                    priceLabel.text = price.ToString();
                    priceLabel.style.color = new StyleColor(Color.white);
                    if (label != null) label.style.color = new StyleColor(Color.white);
                    if (currencyIcon != null) currencyIcon.style.display = DisplayStyle.Flex;
                    break;
            }
        }
        public override void PlaySoundOnClick(ClickEvent clk) { }
    }
}