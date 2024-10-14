using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;
using UnityEngine.Analytics;

namespace UI
{
    public class MenuSettingsWindow: VisualElement
    {
        public new class UxmlFactory: UxmlFactory<MenuSettingsWindow>{}

        private Label titleLabel;
        private VisualElement background;
        private ClickableVisualElement closeButton;
        private SettingsWidget settingsWidget;
        private LanguageWidget languageWidget;
        private CommonButton privacyButton;
        private ClickableVisualElement unityPrivacy;

        private UIHelper uiHelper;

        public void Init()
        {
            uiHelper = UIHelper.Instance;
            
            background = this.parent;
            titleLabel = this.Q<Label>("TitleLabel");
            
            closeButton = this.Q<ClickableVisualElement>("CloseButton");
            closeButton.SoundName = SoundKey.Interface_exitButton;
            closeButton.Init();
            closeButton.RegisterCallback<ClickEvent>((clk => Hide()));
            
            settingsWidget = this.Q<SettingsWidget>("SettingsWidget");
            settingsWidget.Init();
            
            languageWidget = this.Q<LanguageWidget>("LanguageWidget");
            languageWidget.Init();
            
            privacyButton = this.Q<TemplateContainer>("PrivacyButton").Q<CommonButton>();
            privacyButton.Init();
            privacyButton.RegisterCallback<ClickEvent>(OnPrivacyClick);
            
            unityPrivacy = this.Q<ClickableVisualElement>("UnityPrivacy");
            unityPrivacy.Init();
            unityPrivacy.RegisterCallback<ClickEvent>(OnUnityPrivacyClick);

            style.display = DisplayStyle.None;
            background.style.display = DisplayStyle.None;
        }

        public void Dispose()
        {
            settingsWidget.Dispose();
            
            languageWidget.Dispose();

            privacyButton.Dispose();
            privacyButton.UnregisterCallback<ClickEvent>(OnPrivacyClick);
            
            unityPrivacy.Dispose();
            unityPrivacy.UnregisterCallback<ClickEvent>(OnUnityPrivacyClick);
            
            closeButton.Dispose();
            closeButton.UnregisterCallback<ClickEvent>((clk => Hide()));
        }

        public void Show()
        {
            style.display = DisplayStyle.Flex;
            background.style.display = DisplayStyle.Flex;
            privacyButton.SetText(LocalizationManager.GetTranslation("Menu/PrivacyPolicy"));
            Tween animation = UIHelper.Instance.GetShowWindowTween(this, background);
            animation.Play();
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        public void Hide()
        {
            languageWidget.Hide();
            Tween animation = UIHelper.Instance.GetHideWindowTween(this, background);
            animation.Play();
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void OnPrivacyClick(ClickEvent clk)
        {
            OpenUrl("https://kaiserstudio.pro/PRIVACY%20POLICY_GalacticTD.txt");
            Hide();
        }

        private void OnUnityPrivacyClick(ClickEvent clk)
        {
            // DataPrivacy.FetchPrivacyUrl(OpenUrl, OnFailure);
            Hide();
        }
        
        private void OpenUrl(string url)
        {
            Application.OpenURL(url);
        }

        private void OnFailure(string reason)
        {
            Debug.LogWarning(System.String.Format("Failed to get data privacy url: {0}", reason));
        }

        public void UpdateLocalization()
        {
            titleLabel.text = LocalizationManager.GetTranslation("Settings");
            privacyButton.SetText(LocalizationManager.GetTranslation("Menu/PrivacyPolicy"));
            settingsWidget.UpdateLocalization();
            languageWidget.UpdateLocalization();
            
            uiHelper.SetLocalizationFont(settingsWidget);
            uiHelper.SetLocalizationFont(languageWidget);
        }
    }
}