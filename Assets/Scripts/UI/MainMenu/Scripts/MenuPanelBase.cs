using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class MenuPanelBase: VisualElement
    {
        public new class UxmlFactory: UxmlFactory<MenuPanelBase>{}

        private ClickableVisualElement closeButton;
        private VisualElement mainBackground;
        private VisualElement additionalBackground;

        public ClickableVisualElement CloseButton => closeButton;

        private Dictionary<string, int> panelSizes = new() { { "WorkshopPanel", 1860 }, { "ShopPanel", 1860 }, { "DirectivesShop", 1340 }, { "TowerCustomizationPanel", 1130 } };
        private string currentPanelName;
        public void Init()
        {
            closeButton = this.parent.Q<VisualElement>("CloseButton").Q<ClickableVisualElement>();
            closeButton.SoundName = SoundKey.Interface_exitButton;
            closeButton.Init();
            
            mainBackground = this.Q<VisualElement>("MainBackground");
            additionalBackground = this.Q<VisualElement>("AdditionalBackground");
            
            closeButton.style.display = DisplayStyle.None;
            style.display = DisplayStyle.None;
        }

        public void Dispose()
        {
            closeButton.Dispose();
        }

        public Tween GetShowTween(string panelName)
        {
            additionalBackground.style.scale = new StyleScale(new Vector2(1, 1));
            mainBackground.style.width = panelSizes[panelName];
            style.display = DisplayStyle.Flex;
            
            Sequence showSequence = DOTween.Sequence();
            showSequence.Append(UIHelper.Instance.GetMenuPanelFadeTween(this, true));
            showSequence.Insert(0,UIHelper.Instance.GetMenuPanelFadeTween(closeButton, true));
            currentPanelName = panelName;
            return showSequence;
        }

        public Tween GetHideTween()
        {
            Sequence hideSequence = DOTween.Sequence();

            hideSequence.Append(UIHelper.Instance.GetMenuPanelFadeTween(this, false));
            hideSequence.Insert(0, UIHelper.Instance.GetMenuPanelFadeTween(closeButton, false));
            hideSequence.OnComplete(() => style.display = DisplayStyle.None);
            return hideSequence;
        }

        public Tween GetTransitionTween(string panelName)
        {
            if (additionalBackground.resolvedStyle.width <= 0)
            {
                additionalBackground.style.scale = new StyleScale(new Vector2(1, 0));
            }

            Sequence seq = DOTween.Sequence();

            if (panelSizes[panelName] >= 1860 && additionalBackground.resolvedStyle.width > 0)
            {
                seq.Prepend(UIHelper.Instance.ScaleByYTween(additionalBackground, false, 0.2f));
            }

            if (panelSizes[currentPanelName] != panelSizes[panelName])
            {
                seq.Append(DOTween.To(() => mainBackground.resolvedStyle.width, x => mainBackground.style.width = new StyleLength(x), panelSizes[panelName], 0.3f));
            }

            if (panelSizes[panelName] <= 1860 && additionalBackground.resolvedStyle.width <= 0)
            {
                seq.Append(UIHelper.Instance.ScaleByYTween(additionalBackground, true, 0.2f));
            }

            currentPanelName = panelName;
            
            return seq;
        }
    }
}