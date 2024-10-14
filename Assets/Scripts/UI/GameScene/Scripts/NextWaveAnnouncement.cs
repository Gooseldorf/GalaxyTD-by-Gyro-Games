using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class NextWaveAnnouncement: VisualElement
    {
        public new class UxmlFactory: UxmlFactory<NextWaveAnnouncement>{}

        private VisualElement content;
        private Label nextWaveLabel;
        private Label nextWaveNumberLabel;
        private WaveLine waveLine;
        private Label waveHpLabel;
        private Sequence showSeq;

        private UIHelper uiHelper;
        private int creepHp;
        private int creepCount;
        private string nextWaveText;
        private int nextWaveNumber;
        private float resolvedWidth;
        private NextWaveAnnouncementAnimationData animData;

        public void Init()
        {
            content = this.Q<VisualElement>("ContentContainer");
            waveLine = this.Q<WaveLine>("WaveLine");
            waveLine.Init();
            waveHpLabel = this.Q<Label>("WaveHpLabel");
            nextWaveLabel = this.Q<Label>("NextWaveLabel");
            nextWaveNumberLabel = this.Q<Label>("NextWaveNumber");

            uiHelper = UIHelper.Instance;
            animData = uiHelper.NextWaveAnnouncementData;
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
            style.display = DisplayStyle.Flex;
            style.opacity = 1;
            style.width = 0;
            nextWaveNumberLabel.text = "";
            Sequence contentSeq = DOTween.Sequence();
            contentSeq.AppendCallback(() => PlaySound2D(SoundKey.Creep_countIterator));
            contentSeq.Append(uiHelper.ChangeNumberInLabelTween(waveHpLabel, 0, creepHp, animData.TypewriterTime));
            contentSeq.Join(uiHelper.ChangeNumberInLabelTween(waveLine.Q<Label>("Count"), 0, creepCount, animData.TypewriterTime));
            contentSeq.Join( uiHelper.InOutScaleTween(waveLine.Q<VisualElement>("Icon"), 1, 1.2f, animData.TypewriterTime));
            contentSeq.Join(uiHelper.GetTypewriterTween(nextWaveLabel, nextWaveText).OnComplete(() =>
            {
                uiHelper.GetTypewriterTween(nextWaveNumberLabel, nextWaveNumber.ToString()).SetUpdate(true).Play();
            }));
            //contentSeq.Append(uiHelper.GetTypewriterTween(nextWaveNumberLabel, nextWaveNumber.ToString()));
            contentSeq.AppendInterval(animData.IdleTime);
            showSeq = (Sequence)uiHelper.GetInGameAnnouncementTween(this, content, resolvedWidth, contentSeq);
            showSeq.SetUpdate(true).OnComplete(() => content.style.visibility = Visibility.Hidden);
            showSeq.Play();
        }

        public void SetUp(CreepStats stats, int creepCount,int creepHp, string nextWaveText, int nextWaveNum)
        {
            this.nextWaveText = nextWaveText;
            nextWaveNumber = nextWaveNum;
            this.creepHp = creepHp;
            this.creepCount = creepCount;
            waveHpLabel.text = "0";
            nextWaveLabel.text = "";
            waveLine.SetWave(stats, creepCount);
            waveLine.Q<Label>("Count").text = "";
        }

        public void Reset()
        {
            showSeq?.Kill(true);
            style.display = DisplayStyle.None;
            style.opacity = 0;
        }
    }
}