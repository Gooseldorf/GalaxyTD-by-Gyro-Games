using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class SettingsWidget : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<SettingsWidget, UxmlTraits> {}

        private Label musicLabel;
        private SelectableElement musicCheckbox;
        private Label soundLabel; 
        private SelectableElement soundCheckbox;
        private Label dialogsLabel;
        private SelectableElement dialogsCheckbox;

        private UIHelper uiHelper;

        public void Init()
        {
            uiHelper = UIHelper.Instance;
            
            musicLabel = this.Q<Label>("MusicLabel");
            musicLabel.RegisterCallback<ClickEvent>(OnMusicClick);
            musicCheckbox = this.Q<SelectableElement>("MusicCheckbox");
            musicCheckbox.Init();
            musicCheckbox.SetSelected(MusicManager.EnableMusic);
            musicCheckbox.RegisterCallback<ClickEvent>(OnMusicClick);

            soundLabel = this.Q<Label>("SoundLabel");
            soundLabel.RegisterCallback<ClickEvent>(OnSoundClick);
            soundCheckbox = this.Q<SelectableElement>("SoundCheckbox");
            soundCheckbox.Init();
            soundCheckbox.SetSelected(MusicManager.EnableSound);
            soundCheckbox.RegisterCallback<ClickEvent>(OnSoundClick);
            
            dialogsLabel = this.Q<Label>("DialogsLabel");
            dialogsLabel.RegisterCallback<ClickEvent>(OnDialogsClick);
            dialogsCheckbox = this.Q<SelectableElement>("DialogsCheckbox");
            dialogsCheckbox.Init();
            dialogsCheckbox.SetSelected(PlayerPrefs.GetInt(PrefKeys.SkipOldDialogs) == 1);
            dialogsCheckbox.RegisterCallback<ClickEvent>(OnDialogsClick);
        }

        public void Dispose()
        {
            musicCheckbox.UnregisterCallback<ClickEvent>(OnMusicClick);
            musicLabel.UnregisterCallback<ClickEvent>(OnMusicClick);
            soundCheckbox.UnregisterCallback<ClickEvent>(OnSoundClick);
            dialogsCheckbox.UnregisterCallback<ClickEvent>(OnDialogsClick);
        }

        private void OnSoundClick(ClickEvent clk)
        {
            MusicManager.EnableSound = !MusicManager.EnableSound;
            soundCheckbox.SetSelected(MusicManager.EnableSound);
        }

        private void OnMusicClick(ClickEvent clk)
        {
            MusicManager.EnableMusic = !MusicManager.EnableMusic;
            musicCheckbox.SetSelected(MusicManager.EnableMusic);
        }

        private void OnDialogsClick(ClickEvent clk)
        {
            int key = PlayerPrefs.GetInt(PrefKeys.SkipOldDialogs) == 1 ? 0 : 1;
            PlayerPrefs.SetInt(PrefKeys.SkipOldDialogs, key);
            dialogsCheckbox.SetSelected(key == 1);
        }

        public void UpdateLocalization()
        {
            musicLabel.text = LocalizationManager.GetTranslation("Music");
            soundLabel.text = LocalizationManager.GetTranslation("Sound");
            dialogsLabel.text = LocalizationManager.GetTranslation("SkipDialogs");
        }
    }
}