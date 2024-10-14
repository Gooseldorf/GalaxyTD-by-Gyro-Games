using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class CommonButton : SelectableElement
    {
        public new class UxmlFactory : UxmlFactory<CommonButton, UxmlTraits>
        {
        }

        protected Label label;
        private VisualElement icon;
        /*private Tween colorPulse;
        private Tween scalePulse;*/

        public override void Init()
        {
            base.Init();
            label = this.Q<Label>("Label");
            icon = this.Q<VisualElement>("Icon");
            /*colorPulse = UIHelper.Instance.ChangeColorTween(this, Color.cyan, 2).SetLoops(-1, LoopType.Restart).Play();
            scalePulse = UIHelper.Instance.InOutScaleTween(this, 1, 1.005f, 2).SetLoops(-1, LoopType.Yoyo).Play();*/
        }

        public override void SetState(AllEnums.UIState state)
        {
            base.SetState(state);
            switch (state)
            {
                case AllEnums.UIState.Available:
                    SetBackground(UIHelper.Instance.AvailableCommonButtonBackground);
                    if (label != null) label.style.color = new StyleColor(Color.white);
                    if (icon != null) icon.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                    break;
                case AllEnums.UIState.Locked:
                    /*colorPulse.Kill();
                    scalePulse.Kill();*/
                    SetBackground(UIHelper.Instance.LockedCommonButtonBackground);
                    if (label != null) label.style.color = new StyleColor(Color.gray);
                    if (icon != null) icon.style.unityBackgroundImageTintColor = new StyleColor(UIHelper.Instance.LockedGrayTint);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fonSize">0 - don't change font size</param>
        public void SetText(string text, int fonSize = 0)
        {
            if (label == null) return;
            label.text = text;
            UIHelper.Instance.SetLocalizationFont(this);
            if (fonSize != 0)
            {
                label.style.fontSize = fonSize;
            }
        }

        public void SetIcon(Texture2D texture)
        {
            if (icon == null) return;
            icon.style.backgroundImage = new StyleBackground(texture);
            icon.style.display = DisplayStyle.Flex;
        }

        public void SetBackground(Sprite background) => style.backgroundImage = new StyleBackground(background);

        public void ShowIcon(bool show) => icon.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}