using CardTD.Utilities;
using DG.Tweening;
using Sounds.Attributes;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class HardModeButton: ClickableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<HardModeButton> { }

        private VisualElement frame;
        private VisualElement background;
        private VisualElement icon;
        
        private bool isHardMode = false;
        private Sequence seq;
        private UIHelper uiHelper;
        
        public override void Init()
        {
            base.Init();
            SoundName = SoundConstants.EmptyKey;
            uiHelper = UIHelper.Instance;

            frame = this.Q<VisualElement>("Frame");
            background = this.Q<VisualElement>("Background");
            icon = this.Q<VisualElement>("Icon");
            this.RegisterCallback<ClickEvent>(OnClick);
        }

        private void OnClick(ClickEvent clk)
        {
            Messenger.Broadcast(UIEvents.ModeChanged, MessengerMode.DONT_REQUIRE_LISTENER);
            isHardMode = !isHardMode;
            pickingMode = PickingMode.Ignore;
            
            DOTween.Kill(this, this);
            seq = DOTween.Sequence();
            seq.Append(uiHelper.InOutScaleTween(this, 1, 1.05f, 0.4f));
            seq.InsertCallback(0.2f, () =>
            {
                background.style.backgroundImage = new StyleBackground(isHardMode ? uiHelper.RoundButtonRedBackground : uiHelper.RoundButtonBlueBackground);
                frame.style.backgroundImage = new StyleBackground(isHardMode ? uiHelper.RoundButtonRedFrame : uiHelper.RoundButtonBlueFrame);
            });
            seq.OnComplete(() => pickingMode = PickingMode.Position);
            seq.SetTarget(this).SetUpdate(true).Play();

            if (icon.ClassListContains("RotateX"))
                icon.RemoveFromClassList("RotateX");
            else
                icon.AddToClassList("RotateX");
            
            PlaySound2D(SoundKey.Menu_mission_hardmode);
        }

        public void SetHardMode()
        {
            isHardMode = true;
            background.style.backgroundImage = new StyleBackground(uiHelper.RoundButtonRedBackground);
            frame.style.backgroundImage = new StyleBackground(uiHelper.RoundButtonRedFrame);
            icon.AddToClassList("RotateX");
        }
    }
}