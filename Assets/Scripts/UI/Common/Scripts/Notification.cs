using CardTD.Utilities;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine.UIElements;

namespace UI
{
    public class Notification : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Notification, UxmlTraits> {}

        private Label label;

        private bool isShown;
        private Sequence showSeq;
        private Sequence hideSeq;
        private Tween idleTween;
        
        private UIHelper uiHelper;
        private NextWaveAnnouncementAnimationData animData;
        
        public void Init()
        {
            label = this.Q<Label>("Label");

            isShown = false;
            uiHelper = UIHelper.Instance;
            animData = uiHelper.NextWaveAnnouncementData;
            
            Messenger<string, float2>.AddListener(UIEvents.ShowNotification, Show); 
        }

        public void Dispose()
        {
            Messenger<string, float2>.RemoveListener(UIEvents.ShowNotification, Show);
        }

        private void Show(string text, float2 globalPosition)
        {
            style.left = globalPosition.x;
            style.bottom = globalPosition.y;
            if (isShown)
            {
                idleTween.Kill();
                uiHelper.PlayTypewriter(label, text);
                idleTween = DOVirtual.DelayedCall(animData.IdleTime, () => hideSeq.Play()).Play();
            }
            else
            {
                DOTween.Kill(this);
                DOTween.Kill(label);
                showSeq = DOTween.Sequence();
                hideSeq = DOTween.Sequence();
            
                style.visibility = Visibility.Visible;
                style.opacity = 1;
                style.width = new StyleLength(0f);
                label.text = uiHelper.ReplaceWithEmptySymbol(text);
            
                showSeq.Append(uiHelper.FadeTween(this, 0, 1, animData.FadeTime));
                showSeq.Append(uiHelper.ChangeWidthByPercent(this, 0, 100, animData.OpeningTime));
                showSeq.OnComplete(() =>
                {
                    isShown = true;
                    uiHelper.PlayTypewriter(label, text);
                    idleTween = DOVirtual.DelayedCall(animData.IdleTime, () => hideSeq.Restart()).Play();
                });
                showSeq.SetUpdate(true).SetTarget(this).Play();
            
                hideSeq.OnStart(() => isShown = false);
                hideSeq.Append(uiHelper.FadeTween(label, 1, 0, animData.FadeTime));
                hideSeq.Append(uiHelper.ChangeWidthByPercent(this, 100, 0, animData.OpeningTime));
                hideSeq.Append(uiHelper.FadeTween(this, 1, 0, animData.FadeTime));

                hideSeq.OnComplete(() => style.visibility = Visibility.Hidden);
                hideSeq.SetUpdate(true).SetTarget(this).Pause();
            }
        }
    }
}
