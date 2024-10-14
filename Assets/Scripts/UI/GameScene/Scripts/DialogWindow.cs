using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class DialogWindow : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<DialogWindow> { }

        private VisualElement characterIconLeft;
        private VisualElement characterIconRight;
        private Label skipLabel;
        private Label characterNameLabel;
        private ScrollView lineScroll;
        private Label lineLabel;
        private VisualElement bg;
        public VisualElement ClickArea;
        private VisualElement textContainer;

        private List<DialogLine> dialogLines;
        private Dictionary<int, Vector3> pointerPositions;
        private int currentLineIndex;

        private Tween carriageTween;
        private Tween hideTween;

        private string currentLine;
        private int locLineOffset;
        private string locLineKey;

        private bool isLastLeft = false;
        private bool hideAfterDialog;
        private bool clickAreaChanged = false;

        public float Delay = 0;

        public bool IsShowing { get; private set; }
        public event Action OnClickEvent;
        public event Action OnNextLineShow;
        public event Action<float> OnDelayEvent;
        public event Action OnEndDialog;
        private bool skipTypewriterTween;
        
        public void Init()
        {
            skipLabel = this.Q<Label>("SkipLabel");
            characterIconLeft = this.Q<VisualElement>("CharacterIcon1");
            characterIconRight = this.Q<VisualElement>("CharacterIcon2");
            bg = this.Q<VisualElement>("Background");
            characterNameLabel = this.Q<Label>("CharacterName");
            lineScroll = this.Q<ScrollView>("LineScroll");
            lineLabel = this.Q<Label>("Line");
            ClickArea = this.Q<VisualElement>("ClickArea");
            textContainer = this.Q<VisualElement>("Text");

            ClickArea.RegisterCallback<ClickEvent>(OnClick);
            skipLabel.RegisterCallback<ClickEvent>(OnSkipClick);
            style.display = DisplayStyle.None;
            IsShowing = false;
        }
        
        public void Dispose()
        {
            ClickArea.UnregisterCallback<ClickEvent>(OnClick);
            skipLabel.UnregisterCallback<ClickEvent>(OnSkipClick);
        }

        private void SetNextLine()
        {
            if (Delay > 0)
            {
                OnDelay();
            }
            DOVirtual.DelayedCall(Delay, () =>
            {
                DialogLine dialogLine = dialogLines[currentLineIndex];
                Delay = 0;
                SetCharacter(dialogLine);

                characterNameLabel.text = LocalizationManager.GetTranslation($"Dialogs/{dialogLine.CharacterKey}");
                DOTween.Kill(lineLabel, true);
                
                if(carriageTween!=null && carriageTween.IsActive() && !carriageTween.IsComplete())
                    carriageTween.Pause();

                currentLine = LocalizationManager.GetTranslation(locLineKey + (currentLineIndex + locLineOffset));
                UIHelper.Instance.PlayTypewriter(lineLabel, currentLine, true, lineScroll);
                OnNextLineShow?.Invoke();
            });
        }

        private void SetCharacter(DialogLine dialogLine)
        {
            StyleBackground character = new StyleBackground(UIHelper.Instance.GetCharacterSprite(dialogLine.CharacterKey.ToString()));

            switch (dialogLine.CharacterPosition)
            {
                case AllEnums.DialogPosition.Any:
                    if (isLastLeft)
                        characterIconRight.style.backgroundImage = character;
                    else
                        characterIconLeft.style.backgroundImage = character;
                    isLastLeft = !isLastLeft;
                    break;

                case AllEnums.DialogPosition.Left:
                    characterIconLeft.style.backgroundImage = character;
                    isLastLeft = true;
                    break;

                case AllEnums.DialogPosition.Right:
                    characterIconRight.style.backgroundImage = character;
                    isLastLeft = false;
                    break;

                case AllEnums.DialogPosition.AnySingle:
                    characterIconRight.style.backgroundImage = isLastLeft ? character : new StyleBackground();
                    characterIconLeft.style.backgroundImage = isLastLeft ? new StyleBackground() : character;
                    isLastLeft = !isLastLeft;
                    break;

                case AllEnums.DialogPosition.LeftSingle:
                    characterIconLeft.style.backgroundImage = character;
                    characterIconRight.style.backgroundImage = new StyleBackground();
                    isLastLeft = true;
                    break;

                case AllEnums.DialogPosition.RightSingle:
                    characterIconRight.style.backgroundImage = character;
                    characterIconLeft.style.backgroundImage = new StyleBackground();
                    isLastLeft = false;
                    break;
            }

            characterIconRight.style.unityBackgroundImageTintColor = isLastLeft ? Color.gray : Color.white;
            characterIconLeft.style.unityBackgroundImageTintColor = isLastLeft ? Color.white : Color.gray;
        }

        public void SetTextAreaPosition(float2 offset) => textContainer.style.translate = new StyleTranslate(new Translate(offset.x, offset.y));
        

        public void ShowDialog(int missionIndex, bool isBefore)
        {
            DialogHolder dialogHolder = GameServices.Instance.Get<DialogHolder>();
            GameData gameData = DataManager.Instance.GameData;
            Dialog dialog = dialogHolder.GetDialog(missionIndex + 1);
            if (isBefore)
            {
                if (gameData.LastDialogBefore < missionIndex)
                    gameData.LastDialogBefore = missionIndex;
            }
            else
            {
                if (gameData.LastDialogAfter < missionIndex)
                    gameData.LastDialogAfter = missionIndex;
                gameData.ShouldShowDialog = false;
                gameData.SaveToDisk();
            }

            string isBeforeKey = isBefore ? "BeforeMission" : "AfterMission";
            string locLineKey = $"SoftLaunch/{isBeforeKey}{missionIndex + 1}_"; //TODO: "Dialogs/" replaced with "SoftLaunch/". Return when dialogs reworked

            hideAfterDialog = DataManager.Instance.GameData.LastCompletedMissionIndex >= 0 && !isBefore;
            
            List<DialogLine> dialogLines = isBefore ? dialog.BeforeMission : dialog.AfterMission;
            Sprite dialogSprite = isBefore ? dialog.BeforeBg : dialog.AfterBg;

            bg.style.backgroundImage = dialogSprite == null ? new StyleBackground() : new StyleBackground(dialogSprite);

            ShowDialog(dialogLines, locLineKey);
        }

        public void ShowDialog(List<DialogLine> dialogLines, string locLineKey, bool showSkipLabel = true, int dialogLineOffset = 0, bool isTutorial = false)
        {
            if (dialogLines == null || dialogLines.Count == 0)
                return;
            skipTypewriterTween = isTutorial;
            skipLabel.text = $"<u>{LocalizationManager.GetTranslation("Dialogs/SkipDialog")}</u>";
            skipLabel.style.display = showSkipLabel ? DisplayStyle.Flex : DisplayStyle.None;

            IsShowing = true;
            this.dialogLines = dialogLines;
            this.locLineKey = locLineKey;
            isLastLeft = false;
            currentLineIndex = 0;
            locLineOffset = dialogLineOffset;

            characterIconLeft.style.backgroundImage =
            characterIconRight.style.backgroundImage = new StyleBackground();

            style.display = DisplayStyle.Flex;
            style.opacity = 1;

            carriageTween = UIHelper.Instance.GetBlinkingCarriageTween(lineLabel);

            SetNextLine();
        }

        private void OnDelay()
        {
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, Delay, MessengerMode.DONT_REQUIRE_LISTENER);
            this.style.visibility = Visibility.Hidden;
            OnDelayEvent?.Invoke(Delay);
            DOVirtual.DelayedCall(Delay, () => this.style.visibility = Visibility.Visible);
        }

        public void Hide()
        {
            hideTween = UIHelper.Instance.FadeTween(this, 1f, 0.01f, 1)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    GameServices.Instance.SetPause(false);
                    if (TouchCamera.Instance != null)
                        TouchCamera.Instance.CanDrag = true;
                    style.display = DisplayStyle.None;
                    IsShowing = false;
                    hideTween = null;
                });
            hideTween.Play();
        }

        private void OnSkipClick(ClickEvent clk) => EndDialog();

        public void OnClick(ClickEvent clk)
        {
            if (!skipTypewriterTween && !clickAreaChanged && DOTween.IsTweening(lineLabel, true))
            {
                DOTween.Complete(lineLabel, true);
                return;
            }
            
            OnClickEvent?.Invoke();
            
            if(clk.currentTarget == lineScroll) return;
            
            DOTween.Kill(lineLabel, true);
            currentLineIndex++;

            if (currentLineIndex >= dialogLines.Count)
            {
                EndDialog();
                OnEndDialog?.Invoke();
                return;
            }
            SetNextLine();
        }

        public void SetClickAreaSize(float2 position, float2 size, bool isGlobalPosition)
        {
            ClickArea.style.width = size.x;
            ClickArea.style.height = size.y;
            ClickArea.transform.position = isGlobalPosition ? ClickArea.WorldToLocal(position) : new Vector2(position.x, position.y);
            clickAreaChanged = true;
        }

        public void RestoreClickAreaSize(GeometryChangedEvent geom = null)
        {
            this.UnregisterCallback<GeometryChangedEvent>(RestoreClickAreaSize);
            if (this.resolvedStyle.width == 0)
            {
                this.RegisterCallback<GeometryChangedEvent>(RestoreClickAreaSize);
                return;
            }
            ClickArea.style.width = this.resolvedStyle.width;
            ClickArea.style.height = this.resolvedStyle.height;
            ClickArea.transform.position = this.transform.position;
            clickAreaChanged = false;
        }

        private void EndDialog()
        {
            if (hideTween != null)
            {
                hideTween.Kill(true);
                return;
            }
            if (hideAfterDialog)
                Hide();
            else
                IsShowing = false;
        }
    }
}