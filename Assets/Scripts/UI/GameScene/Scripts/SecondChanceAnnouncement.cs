using DG.Tweening;
using I2.Loc;
using UnityEngine.UIElements;

namespace UI
{
    public class SecondChanceAnnouncement : VisualElement
    {
        public new class UxmlFactory: UxmlFactory<SecondChanceAnnouncement>{}

        private VisualElement content;
        private Label label;

        private UIHelper uiHelper;
        private float resolvedWidth;
        
        public void Init()
        {
            uiHelper = UIHelper.Instance;
            
            content = this.Q<VisualElement>("ContentContainer");
            label = this.Q<Label>();
            label.text = LocalizationManager.GetTranslation("SecondChance");
            RegisterCallback<GeometryChangedEvent>(ResolveWidth);
        }

        private void ResolveWidth(GeometryChangedEvent geom)
        {
            if(float.IsNaN(resolvedStyle.width) || resolvedStyle.width == resolvedStyle.minWidth) 
                return;
            UnregisterCallback<GeometryChangedEvent>(ResolveWidth);
            resolvedWidth = this.resolvedStyle.width;
        }

        public void Show()
        {
            DOTween.Kill(this);
            style.display = DisplayStyle.Flex;
            style.width = 0;
            label.text = "";
            Sequence contentSeq = DOTween.Sequence();
            contentSeq.Join(uiHelper.GetTypewriterTween(label, LocalizationManager.GetTranslation("SecondChance")));
            contentSeq.AppendInterval(uiHelper.NextWaveAnnouncementData.IdleTime).SetTarget(this).SetUpdate(true);

            Sequence showSeq = (Sequence)uiHelper.GetInGameAnnouncementTween(this, content, resolvedWidth, contentSeq);
            showSeq.SetUpdate(true).SetTarget(this).OnComplete(() => content.style.visibility = Visibility.Hidden);
            showSeq.Play();
        }
    }
}
