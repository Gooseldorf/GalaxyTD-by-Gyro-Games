using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class LanguageWidget: VisualElement
    {
        public new class UxmlFactory: UxmlFactory<LanguageWidget, UxmlTraits>{}

        private Label languageLabel;
        private Label currentLanguageLabel;
        private ClickableVisualElement changeLanguageButton;
        private VisualElement languagesContainer;
        private List<ClickableVisualElement> languageButtons;

        private float resolvedHeight;
        private bool isAnimationPlaying;
        private UIHelper uiHelper;
        private bool isSelectionOpened;

        public void Init()
        {
            uiHelper = UIHelper.Instance;
            languageLabel = this.Q<Label>("LanguageLabel");
            currentLanguageLabel = this.Q<Label>("CurrentLanguageLabel");
            
            changeLanguageButton = this.Q<ClickableVisualElement>("ChangeLanguageButton");
            changeLanguageButton.Init();
            changeLanguageButton.RegisterCallback<ClickEvent>(OnChangeLanguageClick);
            
            languagesContainer = this.Q<VisualElement>("LanguagesContainer");
            languageButtons = languagesContainer.Query<ClickableVisualElement>().ToList();
            foreach (ClickableVisualElement button in languageButtons)
            {
                button.Init();
                button.RegisterCallback<ClickEvent>(OnLanguageClick);
            }
            
            this.RegisterCallback<GeometryChangedEvent>(ResolveLanguagesContainerHeight);
        }

        public void Dispose()
        {
            changeLanguageButton.Dispose();
            changeLanguageButton.UnregisterCallback<ClickEvent>(OnChangeLanguageClick);

            foreach (ClickableVisualElement button in languageButtons)
            {
                button.Dispose();
                button.UnregisterCallback<ClickEvent>(OnLanguageClick);
            }
        }

        private void OnChangeLanguageClick(ClickEvent clk)
        {
            if(isAnimationPlaying || isSelectionOpened) return;
            isSelectionOpened = true;
            AnimateLanguagesContainer(true);
        }

        public void Hide()
        {
            if(isSelectionOpened) AnimateLanguagesContainer(false);
        }

        private void AnimateLanguagesContainer(bool show)
        {
            DOTween.Kill(changeLanguageButton, true);
            Sequence seq = DOTween.Sequence();
            isAnimationPlaying = true;
            if (show)
            {
                languagesContainer.style.height = 0;
                seq.Append(uiHelper.FadeTween(currentLanguageLabel, 1, 0, 0.3f));
                seq.Append(uiHelper.ChangeHeight(languagesContainer, languagesContainer.resolvedStyle.minHeight.value, resolvedHeight, 0.5f));
                seq.AppendCallback(() =>
                {
                    changeLanguageButton.pickingMode = PickingMode.Ignore;
                    languagesContainer.style.display = DisplayStyle.Flex;
                });
                seq.Append(uiHelper.FadeTween(languagesContainer, 0, 1, 0.3f));
            }
            else
            {
                seq.Append(uiHelper.FadeTween(languagesContainer, 1, 0, 0.3f));
                seq.Append(uiHelper.ChangeHeight(languagesContainer, languagesContainer.resolvedStyle.height, 0, 0.5f));
                seq.AppendCallback(() =>
                {
                    languagesContainer.style.display = DisplayStyle.None;
                    changeLanguageButton.pickingMode = PickingMode.Position;
                    isSelectionOpened = false;
                });
                seq.Append(uiHelper.FadeTween(currentLanguageLabel, 0, 1, 0.3f));
            }

            seq.OnComplete(() =>
            {
                isAnimationPlaying = false;
            });
            seq.SetUpdate(true).SetTarget(changeLanguageButton).Play();
        }

        private void OnLanguageClick(ClickEvent clk)
        {
            if (isAnimationPlaying) return;
         
            ClickableVisualElement target = (ClickableVisualElement)clk.currentTarget;

            if (LocalizationManager.CurrentLanguage == target.name)
            {
                AnimateLanguagesContainer(false);
                return;
            }
            LocalizationManager.CurrentLanguage = target.name;
            
            Messenger.Broadcast(UIEvents.LanguageChanged, MessengerMode.DONT_REQUIRE_LISTENER);
            
            UpdateLocalization();
            AnimateLanguagesContainer(false);
        }
        
        private void ResolveLanguagesContainerHeight(GeometryChangedEvent geom)
        {
            if(float.IsNaN(languagesContainer.resolvedStyle.height) || languagesContainer.resolvedStyle.height == 0 || languagesContainer.resolvedStyle.height == languagesContainer.style.minHeight)
                return;
            resolvedHeight = languagesContainer.resolvedStyle.height;
            currentLanguageLabel.style.opacity = 1;
            languagesContainer.style.opacity = 0;
            languagesContainer.style.display = DisplayStyle.None;
            this.UnregisterCallback<GeometryChangedEvent>(ResolveLanguagesContainerHeight);
        }

        public void UpdateLocalization()
        {
            currentLanguageLabel.text = LocalizationManager.GetTranslation("CurrentLanguage");
            languageLabel.text = LocalizationManager.GetTranslation("Language");
        }
    }
}