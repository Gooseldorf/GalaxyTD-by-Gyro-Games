using DG.Tweening;
using UnityEngine.UIElements;

namespace UI
{
    public class DirectiveTooltip : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<DirectiveTooltip>{}

        private Label descLabel;
        private VisualElement arrow;
        
        private float tooltipOpacity
        {
            get => this.style.opacity.value;
            set
            {
                StyleFloat opacityFloat = value;
                this.style.opacity = opacityFloat;
            }
        }
        
        public void Init()
        {
            descLabel = this.Q<Label>("DescLabel");
            arrow = this.Q<VisualElement>("Arrow");
        }

        public void Show(string descText, VisualElement directive)
        {
            DOTween.Kill(this, true);
            
            descLabel.text = descText;
            style.display = DisplayStyle.Flex;
            arrow.style.left = new StyleLength(directive.parent.resolvedStyle.left);

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(this);
            
            sequence.Append(DOTween.To(() => tooltipOpacity, x => tooltipOpacity = x,
                    1.0f, .3f)
                .SetEase(Ease.Linear)
                .Pause());
            sequence.AppendInterval(3.0f);
            sequence.Append(DOTween.To(() => tooltipOpacity, x => tooltipOpacity = x,
                    .0f, .3f)
                .SetEase(Ease.Linear)
                .Pause());
                
            sequence.OnComplete(() => style.display = DisplayStyle.None);
            sequence.SetUpdate(true).Play();
        }
    }
}