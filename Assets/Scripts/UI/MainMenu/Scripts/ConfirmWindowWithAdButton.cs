using Data.Managers;
using Managers;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class ConfirmWindowWithAdButton : ConfirmWindow
    {
        #region UxmlStaff
        public new class UxmlFactory : UxmlFactory<ConfirmWindowWithAdButton, UxmlTraits>{}
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlAssetAttributeDescription<Sprite> adButtonBackground = new(){ name = "Ad_Button_Background", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> hardCurrency = new(){ name = "HardCurrency", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> ticket = new(){ name = "Ticket", defaultValue = null };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (adButtonBackground.TryGetValueFromBag(bag, cc, out Sprite value1))
                    ((ConfirmWindowWithAdButton)ve).AdButtonBackground = value1;
                if (ticket.TryGetValueFromBag(bag, cc, out Texture2D value2))
                    ((ConfirmWindowWithAdButton)ve).Ticket = value2;
                if (hardCurrency.TryGetValueFromBag(bag, cc, out Texture2D value3))
                    ((ConfirmWindowWithAdButton)ve).HardCurrency = value3;
            }
        }
        public Sprite AdButtonBackground { get; set; }
        public Texture2D HardCurrency { get; set; }
        public Texture2D Ticket { get; set; }
        
        #endregion
        
        private ClickableVisualElement closeButton;
        private PriceButton adButton;

        public Action ShowShopAction;
        
        public override void Init()
        {
            base.Init();
            closeButton = this.Q<ClickableVisualElement>("CloseButton");
            closeButton.SoundName = SoundKey.Interface_exitButton;
            closeButton.Init();
            closeButton.RegisterCallback<ClickEvent>(OnCloseClick);

            adButton = this.Q<PriceButton>("AdButton");
            adButton.Init();
            adButton.SetBackground(AdButtonBackground);
            adButton.RegisterCallback<ClickEvent>(OnAdButtonClick);

            int price = 5;
            confirmButton.SetText(price.ToString());//TODO: TICKET PRICE
            confirmButton.SetIcon(HardCurrency);
        }

        public override void SetUp(Sprite icon, string desc, Action onConfirmAction)
        {
            base.SetUp(icon, desc, onConfirmAction);
            
            //TODO: Check if can show AD => set AdButton State
            AdsManager.LoadReward(AdsRewardType.GetTicket);
        }

        protected override void OnConfirmClick(ClickEvent clk)
        {
            int price = 5;//TODO: Get ticketPrice
            if (DataManager.Instance.GameData.HardCurrency < price)
                base.onConfirmAction = ShowShopAction;
            
            base.OnConfirmClick(clk);
        }

        private void OnAdButtonClick(ClickEvent clk)
        {
            AdsManager.TryShowReward(() => {adButton.SetState(AllEnums.UIState.Unavailable);},
                () =>
                {
                    DataManager.Instance.GameData.BuyTickets(1, 0);
                    Hide();
                });
        }
    }
}