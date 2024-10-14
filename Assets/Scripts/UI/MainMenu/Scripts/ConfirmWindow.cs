using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using Sounds.Attributes;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class ConfirmWindow : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ConfirmWindow>{}

        private VisualElement background;
        private VisualElement icon;
        private Label desc;
        private string descText;
        protected CommonButton confirmButton;
        private CommonButton cancelButton;
        private float typewriterSpeedMultiplier = 1.5f;
        protected Action onConfirmAction;

        public virtual void Init()
        {
            background = this.parent;
            
            icon = this.Q<VisualElement>("Icon");
            desc = this.Q<Label>("Desc");
            
            confirmButton = this.Q<TemplateContainer>("ConfirmButton").Q<CommonButton>();
            confirmButton.SoundName = SoundConstants.EmptyKey;
            confirmButton.Init();
            confirmButton.RegisterCallback<ClickEvent>(OnConfirmClick);
            
            cancelButton = this.Q<TemplateContainer>("CancelButton").Q<CommonButton>();
            cancelButton.SoundName = SoundKey.Interface_exitButton;
            cancelButton.Init();
            cancelButton.RegisterCallback<ClickEvent>(OnCloseClick);
            
            style.display = DisplayStyle.None;
            background.style.display = DisplayStyle.None;
        }

        public virtual void SetUp(Sprite icon, string desc, Action onConfirmAction)
        {
            this.icon.style.backgroundImage = new StyleBackground(icon);
            this.desc.text = "";
            descText = desc;
            this.onConfirmAction = onConfirmAction;
        }
        
        public virtual void SetUp(Texture2D icon, string desc, Action onConfirmAction, float typewriterSpeedMultiplier = 1.5f)
        {
            this.icon.style.backgroundImage = new StyleBackground(icon);
            this.desc.text = "";
            descText = desc;
            this.onConfirmAction = onConfirmAction;
        }

        public virtual void Dispose()
        {
            confirmButton.Dispose();
            confirmButton.UnregisterCallback<ClickEvent>(OnConfirmClick);
            
            cancelButton.Dispose();
            cancelButton.UnregisterCallback<ClickEvent>(OnCloseClick);
        }

        public void Show()
        {
            UpdateLocalization();
            style.display = DisplayStyle.Flex;
            background.style.display = DisplayStyle.Flex;

            Tween animation = UIHelper.Instance.GetShowWindowTween(this, background);
            animation.SetUpdate(true).Play();
            animation.OnComplete(() =>
            {
                DOTween.Kill(desc);
                UIHelper.Instance.PlayTypewriter(desc, descText, true, null, typewriterSpeedMultiplier);
            });
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        public virtual void Hide()
        {
            Tween animation = UIHelper.Instance.GetHideWindowTween(this, background);
            animation.Play();
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        protected virtual void OnConfirmClick(ClickEvent clk)
        {
            onConfirmAction?.Invoke();
            Hide();
        }

        protected void OnCloseClick(ClickEvent clk)
        {
            onConfirmAction = null;
            Hide();
        }

        public virtual void UpdateLocalization()
        {
            cancelButton.SetText(LocalizationManager.GetTranslation("Cancel"));
            confirmButton.SetText(LocalizationManager.GetTranslation("Confirm"));
        }
    }
}