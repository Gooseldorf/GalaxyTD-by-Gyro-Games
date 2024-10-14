using Sounds.Attributes;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class TopPanel : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TopPanel> {}

        public SelectableStateElement PauseButton;
        public AccelerateButton AccelerateButton;
        private ClickableVisualElement pauseWindowButton;
        private PauseWindow pauseWindow;
        private SpeedState currentSpeed;

        private bool isPaused;

        public void Init(PauseWindow pauseWindow)
        {
            this.pauseWindow = pauseWindow;
            PauseButton = this.Q<SelectableStateElement>("PauseButton");
            AccelerateButton = this.Q<AccelerateButton>("AccelerateButton");
            pauseWindowButton = this.Q<ClickableVisualElement>("PauseWindowButton");
            
            PauseButton.SoundName = SoundConstants.EmptyKey;
            PauseButton.Init();
            AccelerateButton.Init();
            pauseWindowButton.Init();
            
            PauseButton.RegisterCallback<ClickEvent>(OnPauseButtonClick);
            AccelerateButton.RegisterCallback<ClickEvent>(OnAccelerateButtonClick);
            pauseWindowButton.RegisterCallback<ClickEvent>(OnPauseWindowButtonClick);

            AccelerateButton.style.display = DataManager.Instance.GameData.Stars.ContainsKey(0) ? DisplayStyle.Flex : DisplayStyle.None;
            currentSpeed = SpeedState.Normal;
        }

        public void Dispose()
        {
            PauseButton.Dispose();
            AccelerateButton.Dispose();
            pauseWindowButton.Dispose();
            
            PauseButton.UnregisterCallback<ClickEvent>(OnPauseButtonClick);
            AccelerateButton.UnregisterCallback<ClickEvent>(OnAccelerateButtonClick);
            pauseWindowButton.UnregisterCallback<ClickEvent>(OnPauseWindowButtonClick);
        }

        public void Reset()
        {
            PauseButton.SetSelected(false);
            isPaused = false;
            currentSpeed = SpeedState.Normal;
            AccelerateButton.SetSpeed(currentSpeed);
        }

        private void OnPauseButtonClick(ClickEvent clk)
        {
            isPaused = !isPaused;
            PauseButton.SetSelected(isPaused);
            GameServices.Instance.SetPause(isPaused);
            PlaySound2D(isPaused ? SoundKey.Interface_pause_on : SoundKey.Interface_pause_off);
        }

        private void OnAccelerateButtonClick(ClickEvent clk)
        {
            currentSpeed = GetNextSpeedState();
            AccelerateButton.SetSpeed(currentSpeed);
            GameServices.Instance.SetTimeScale((int)currentSpeed); 
        }
        
        private SpeedState GetNextSpeedState()
        {
            switch (currentSpeed)
            {
                case SpeedState.Normal:
                    return SpeedState.X2;
                case SpeedState.X2:
                    return SpeedState.X4;
                case SpeedState.X4:
                    return SpeedState.Normal;
                default:
                    return SpeedState.Normal;
            }
        }

        private void OnPauseWindowButtonClick(ClickEvent clk)
        {
            pauseWindow.Show(true);
        }
    }
}