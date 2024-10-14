using CardTD.Utilities;
using Data.Managers;
using DG.Tweening;
using I2.Loc;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class DailyRewardWindow : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<DailyRewardWindow>
        { }

        private VisualElement background;
        private VisualElement content;
        private Label title;
        private ClickableVisualElement closeButton;
        private List<DailyRewardItemWidget> dailyRewardWidgets;
        private int rewardsAvailable;
        public bool IsShowing = false;

        private UIHelper uiHelper;
        private NewItemsWindow newItemsWindow;
        private DailyRewardButton dailyRewardButton;

        public void Init(NewItemsWindow newItemsWindow, DailyRewardButton dailyRewardButton)
        {
            uiHelper = UIHelper.Instance;
            this.newItemsWindow = newItemsWindow;
            this.dailyRewardButton = dailyRewardButton;
            background = this;
            content = this.Q<VisualElement>("root");
            title = this.Q<Label>("Title");
            closeButton = this.Q<ClickableVisualElement>("CloseButton");
            closeButton.SoundName = SoundKey.Interface_exitButton;
            closeButton.Init();
            dailyRewardWidgets = this.Query<DailyRewardItemWidget>().ToList();
            List<DailyRewardItem> rewards = MainMenuDaily.GetRewards();
            
            for (int i = 0; i < dailyRewardWidgets.Count; i++)
            {
                dailyRewardWidgets[i].Init();
                dailyRewardWidgets[i].SetDay(i + 1);
                dailyRewardWidgets[i].SetState((AllEnums.UIState)rewards[i].StatusType);
                dailyRewardWidgets[i].SetReward(rewards[i].Reward);
                if (rewards[i].StatusType == DailyRewardStatusType.ReadyToTake)
                {
                    rewardsAvailable++;
                    dailyRewardWidgets[i].RegisterCallback<ClickEvent>(OnRewardClick);
                }
            }

            if (rewardsAvailable == 0)
                rewardsAvailable = -1;
            
            UpdateDirectiveRewardWidget();
            
            style.display = DisplayStyle.None;
            background.style.display = DisplayStyle.None;
            closeButton.RegisterCallback<ClickEvent>(Hide);
        }

        private void Hide(ClickEvent evt) => Hide();

        private void OnRewardClick(ClickEvent evt)
        {
            DailyRewardItemWidget widget = (DailyRewardItemWidget)evt.currentTarget;
            if(widget.State != AllEnums.UIState.Available) return;
            rewardsAvailable--;
            widget.PlayTakeAnimation();
            MainMenuDaily.TakeReward(widget.Reward, out WeaponPart directiveReward);
            MainMenuDaily.SaveTakenRewardType(widget.Reward.DailyRewardType);
            switch (widget.Reward.DailyRewardType)
            {
                case DailyRewardType.Soft:
                    PlaySound2D(SoundKey.Menu_shop_credits);
                    break;
                case DailyRewardType.Hard:
                    PlaySound2D(SoundKey.Menu_shop_crystals);
                    break;
                case DailyRewardType.Scrap:
                    PlaySound2D(SoundKey.Menu_shop_scrap);
                    break;
                case DailyRewardType.Ticket:
                    PlaySound2D(SoundKey.Menu_shop_ticket);
                    break;
                default:
                    PlaySound2D(SoundKey.Menu_workshop_directiveSet);
                    if (directiveReward != null)
                    {
                        newItemsWindow.ShowDirective(directiveReward, LocalizationManager.GetTranslation("ConfirmWindow/NewDirectiveReceived"));
                        newItemsWindow.Show();
                        dailyRewardButton.UpdateDirectiveRewardSprite();
                        UpdateDirectiveRewardWidget();
                    }
                    PlaySound2D(SoundKey.Menu_workshop_directiveSet);
                    break;
            }
        }

        public void Show()
        {
            IsShowing = true;
            style.display = DisplayStyle.Flex;
            background.style.display = DisplayStyle.Flex;

            Tween animation = UIHelper.Instance.GetShowWindowTween(content, background);
            animation.Play();
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void UpdateDirectiveRewardWidget()
        {
            if (MainMenuDaily.DirectiveRewardReceivedToday(out string directiveId))
            {
                DailyRewardItemWidget widget = dailyRewardWidgets.Find(x => x.Reward.DailyRewardType == DailyRewardType.RandomDirective);
                if(widget.State != AllEnums.UIState.Locked) 
                    widget.RewardIcon.style.backgroundImage = new StyleBackground(DataManager.Instance.Get<PartsHolder>().Directives.Find(x => x.SerializedID == directiveId).Sprite);
            }
        }

        private void Hide()
        {
            Tween animation = UIHelper.Instance.GetHideWindowTween(content, background);
            if (rewardsAvailable == 0)
            {
                Messenger.Broadcast(UIEvents.DailyRewardsReceived, MessengerMode.DONT_REQUIRE_LISTENER);
                rewardsAvailable = - 1;
            }

            animation.OnComplete(() => IsShowing = false);
            animation.Play();
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        public void Dispose()
        {
            foreach (DailyRewardItemWidget widget in dailyRewardWidgets)
                widget.Dispose();

            closeButton.UnregisterCallback<ClickEvent>(Hide);
        }

        public void UpdateLocalization()
        {
            title.text = LocalizationManager.GetTranslation("Menu/DailyRewards");
            uiHelper.SetLocalizationFont(title);
            foreach (DailyRewardItemWidget widget in dailyRewardWidgets)
            {
                widget.UpdateLocalization();
            }
        }
    }
}