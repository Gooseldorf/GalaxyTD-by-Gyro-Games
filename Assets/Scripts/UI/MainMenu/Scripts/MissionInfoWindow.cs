using CardTD.Utilities;
using DG.Tweening;
using I2.Loc;
using Sounds.Attributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static MusicManager;
using Object = UnityEngine.Object;

namespace UI
{
    public class MissionInfoWindow : VisualElement
    {
        #region UxmlStaff

        public new class UxmlFactory : UxmlFactory<MissionInfoWindow, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlAssetAttributeDescription<Texture2D> inactiveStar = new() { name = "Inactive_Star", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> activeStar = new() { name = "Active_Star", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> activeHardStar = new() { name = "ActiveHardStar", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> softReward = new() { name = "SoftReward", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> hardReward = new() { name = "HardReward", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> scrapReward = new() { name = "ScrapReward", defaultValue = null };
            UxmlAssetAttributeDescription<Texture2D> emptyDirective = new() { name = "EmptyDirective", defaultValue = null };
            UxmlAssetAttributeDescription<Sprite> ticketIcon = new() { name = "TicketIcon", defaultValue = null };


            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (inactiveStar.TryGetValueFromBag(bag, cc, out Texture2D value1))
                    ((MissionInfoWindow)ve).InactiveStar = value1;
                if (activeStar.TryGetValueFromBag(bag, cc, out Texture2D value2))
                    ((MissionInfoWindow)ve).ActiveStar = value2;
                if (activeHardStar.TryGetValueFromBag(bag, cc, out Texture2D value3))
                    ((MissionInfoWindow)ve).ActiveHardStar = value3;
                if (softReward.TryGetValueFromBag(bag, cc, out Texture2D value4))
                    ((MissionInfoWindow)ve).SoftReward = value4;
                if (hardReward.TryGetValueFromBag(bag, cc, out Texture2D value5))
                    ((MissionInfoWindow)ve).HardReward = value5;
                if (scrapReward.TryGetValueFromBag(bag, cc, out Texture2D value6))
                    ((MissionInfoWindow)ve).ScrapReward = value6;
                if (emptyDirective.TryGetValueFromBag(bag, cc, out Texture2D value7))
                    ((MissionInfoWindow)ve).EmptyDirective = value7;
                if (ticketIcon.TryGetValueFromBag(bag, cc, out Sprite value8))
                    ((MissionInfoWindow)ve).TicketIcon = value8;
            }
        }
        public Texture2D InactiveStar { get; set; }
        public Texture2D ActiveStar { get; set; }
        public Texture2D ActiveHardStar { get; set; }
        public Texture2D SoftReward { get; set; }
        public Texture2D HardReward { get; set; }
        public Texture2D ScrapReward { get; set; }
        public Texture2D EmptyDirective { get; set; }
        public Sprite TicketIcon { get; set; }

        #endregion

        private VisualElement background;
        private List<VisualElement> windowBackgrounds;
        private VisualElement skulls;
        private VisualElement ticketsContainer;
        private Label ticketsLabel;
        private ClickableVisualElement closeButton;
        private Label titleLabel;
        //Stars
        private List<VisualElement> starIcons;
        private List<VisualElement> starBackgrounds;
        private VisualElement progressBarFilling;
        //Rewards
        private Label rewardsLabel;
        private List<SelectableElement> rewards;
        private List<Label> rewardLabels;
        private VisualElement questionMark;
        //WavesPanel
        private VisualElement wavesPanel;
        private float wavesPanelWidth;
        private ScrollView wavesScroll;
        private ClickableVisualElement wavesButton;
        private VisualTreeAsset waveLinePrefab;
        private List<WaveLine> waveLines = new();
        //MapPanel
        private VisualElement mapPanel;
        private float mapPanelWidth;
        private VisualElement map;
        private ClickableVisualElement mapButton;

        private CommonButton startButton;
        private ConfirmWindowWithAdButton confirmWindow;
        private DialogWindow dialogWindow;

        private Mission mission;
        private bool isHard;
        private bool firstTry;

        private UIHelper uiHelper;
        private IReadOnlyDictionary<int, int> stars;
        private IReadOnlyDictionary<int, int> selectedRewards;

        public void Init(VisualTreeAsset waveLinePrefab, ConfirmWindowWithAdButton confirmWindow, DialogWindow dialogWindow)
        {
            uiHelper = UIHelper.Instance;
            selectedRewards = DataManager.Instance.GameData.SelectedRewards;
            this.confirmWindow = confirmWindow;
            this.dialogWindow = dialogWindow;

            background = this.parent;
            windowBackgrounds = this.Query<VisualElement>("WindowBackground").ToList();
            skulls = this.Q<VisualElement>("Skulls");
            titleLabel = this.Q<Label>("TitleLabel");
            ticketsContainer = this.Q<VisualElement>("TicketsContainer");
            ticketsLabel = this.Q<Label>("TicketsLabel");
            //Stars
            starIcons = this.Query<VisualElement>("Star").ToList();
            starBackgrounds = this.Q<VisualElement>("StarsContainer").Query<VisualElement>("Background").ToList();
            progressBarFilling = this.Q<VisualElement>("ProgressBarFilling");
            //Rewards
            rewards = this.Query<SelectableElement>("Reward").ToList();
            rewardLabels = this.Query<Label>("RewardAmount").ToList();
            foreach (SelectableElement selectable in rewards) selectable.Init();
            rewardsLabel = this.Q<Label>("RewardsLabel");
            questionMark = this.Q<VisualElement>("QuestionMark");
            //WavesPanel
            wavesPanel = this.Q<VisualElement>("WavesPanel");
            wavesPanel.style.opacity = 0;
            wavesScroll = wavesPanel.Q<ScrollView>("WavesScroll");
            wavesButton = this.Q<ClickableVisualElement>("WavesButton");
            wavesButton.SoundName = SoundConstants.EmptyKey;
            wavesButton.Init();
            wavesButton.RegisterCallback<ClickEvent>(OnWavesButtonClick);
            this.waveLinePrefab = waveLinePrefab;
            //MapPanel
            mapPanel = this.Q<VisualElement>("MapPanel");
            mapPanel.style.opacity = 0;
            map = mapPanel.Q<VisualElement>("Map");
            mapButton = this.Q<ClickableVisualElement>("MapButton");
            mapButton.SoundName = SoundConstants.EmptyKey;
            mapButton.Init();
            mapButton.RegisterCallback<ClickEvent>(OnMapButtonClick);
            //Buttons
            closeButton = this.Q<ClickableVisualElement>("CloseButton");
            closeButton.SoundName = SoundKey.Interface_exitButton;
            closeButton.Init();
            closeButton.RegisterCallback<ClickEvent>(OnCloseClick);

            startButton = this.Q<TemplateContainer>("StartButton").Q<CommonButton>();
            startButton.Init();
            startButton.SetIcon(TicketIcon.texture);
            startButton.RegisterCallback<ClickEvent>(OnStartClick);

            style.display = DisplayStyle.None;
            background.style.display = DisplayStyle.None;

            DataManager.Instance.GameData.CurrencyUpdated += UpdateTicketsCount;
        }

        public void Dispose()
        {
            foreach (SelectableElement selectable in rewards) selectable.Dispose();
            wavesButton.Dispose();
            wavesButton.UnregisterCallback<ClickEvent>(OnWavesButtonClick);
            mapButton.Dispose();
            mapButton.UnregisterCallback<ClickEvent>(OnMapButtonClick);
            startButton.Dispose();
            startButton.UnregisterCallback<ClickEvent>(OnStartClick);
            closeButton.Dispose();
            closeButton.UnregisterCallback<ClickEvent>(OnCloseClick);

            DataManager.Instance.GameData.CurrencyUpdated -= UpdateTicketsCount;
        }

        public void Show(Mission mission, bool isHard)
        {
            this.mission = mission;
            this.isHard = isHard;
            stars = isHard ? DataManager.Instance.GameData.HardStars : DataManager.Instance.GameData.Stars;
            firstTry = !stars.ContainsKey(mission.MissionIndex);

            titleLabel.text = "";
            DOVirtual.DelayedCall(0.3f, () =>
            {
                DOTween.Kill(titleLabel);
                uiHelper.PlayTypewriter(titleLabel, $"{LocalizationManager.GetTranslation("Mission")} {mission.MissionIndex + 1}");
            });

            SetModeAndStars();
            SetRewards();
            SetMap();
            SetWaves();

            style.display = DisplayStyle.Flex;
            background.style.display = DisplayStyle.Flex;

            Tween animation = UIHelper.Instance.GetShowWindowTween(this, background);
            animation.Play();
            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void SetModeAndStars()
        {
            skulls.style.display = isHard ? DisplayStyle.Flex : DisplayStyle.None;
            ticketsContainer.style.display = isHard ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateTicketsCount();
            
            startButton.ShowIcon(isHard && !DataManager.Instance.GameData.SkipAds);

            foreach (VisualElement windowBackground in windowBackgrounds)
                windowBackground.style.unityBackgroundImageTintColor = isHard ? uiHelper.StarsBackgroundColorRed : uiHelper.StarsBackgroundColor;

            Texture2D activeStar = isHard ? ActiveHardStar : ActiveStar;
            Color starBackground = isHard ? uiHelper.StarsBackgroundColorRed : uiHelper.StarsBackgroundColor;

            if (!firstTry)
            {
                for (int i = 0; i < starIcons.Count; i++)
                {
                    if (i < stars[mission.MissionIndex])
                    {
                        starIcons[i].style.backgroundImage = new StyleBackground(activeStar);
                        starBackgrounds[i].style.unityBackgroundImageTintColor = new StyleColor(starBackground);
                    }
                    else
                    {
                        starIcons[i].style.backgroundImage = new StyleBackground(InactiveStar);
                        starBackgrounds[i].style.unityBackgroundImageTintColor = new StyleColor(uiHelper.StarsBackgroundColorGray);
                    }
                }
                progressBarFilling.style.width = new StyleLength(Length.Percent(stars[mission.MissionIndex] / 3f * 100));
            }
            else
            {
                for (int i = 0; i < starIcons.Count; i++)
                {
                    starIcons[i].style.backgroundImage = new StyleBackground(InactiveStar);
                    starBackgrounds[i].style.unityBackgroundImageTintColor = new StyleColor(uiHelper.StarsBackgroundColorGray);
                }
                progressBarFilling.style.width = new StyleLength();
            }
        }

        private void SetRewards()
        {
            rewards[0].Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(isHard ? ScrapReward : SoftReward);
            rewardLabels[0].text = isHard ? mission.Reward.Scrap.ToStringBigValue() : mission.Reward.SoftCurrency.ToStringBigValue();

            rewards[1].Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(HardReward);
            rewardLabels[1].text = mission.Reward.HardCurrency.ToStringBigValue();

            rewards[2].style.display = DisplayStyle.None;
            rewards[3].style.display = DisplayStyle.Flex;

            if (selectedRewards.ContainsKey(mission.MissionIndex))
            {
                List<WeaponPart> rewardItems = DataManager.Instance.Get<UnlockManager>().GetRewardsForChoose(mission.MissionIndex);
                rewards[3].Q<VisualElement>("Icon").style.scale = rewardItems[0].PartType == AllEnums.PartType.Ammo ? new StyleScale(new Vector2(0.7f, 0.7f)) : new StyleScale(new Vector2(1f, 1f));
                rewards[3].Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(rewardItems[isHard ? 1 - selectedRewards[mission.MissionIndex] : selectedRewards[mission.MissionIndex]].Sprite);
                questionMark.style.display = DisplayStyle.None;
            }
            else
            {
                if (isHard)
                {
                    Debug.LogError($"SelectedRewards should contain selected reward index for normal mode! HardMode directive reward displayed incorrectly!");
                    return;
                }

                rewards[3].Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground();
                questionMark.style.display = DisplayStyle.Flex;
                uiHelper.AnimateQuestionMark(questionMark);
            }

            if (firstTry) return;

            SetReplayRewards();
        }

        private void SetReplayRewards()
        {
            //TODO: this method should be changed
            return;
            //int replayReward = isHard ? mission.Reward.ScrapReplay : mission.Reward.SoftReplay;
            //for (int i = 0; i < stars[mission.MissionIndex]; i++)
            //{
            //    rewards[i].Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(isHard ? ScrapReward : SoftReward);
            //    int rewardToDisplay = (int)(replayReward * 1f / 6f * (i + 1));
            //    rewardLabels[i].text = rewardToDisplay.ToStringBigValue();
            //    if (i >= 2)
            //    {
            //        rewards[i].style.display = DisplayStyle.Flex;
            //        rewards[3].style.display = DisplayStyle.None;
            //    }
            //}
        }

        private void SetMap()
        {
            GameServices.Instance.Get<MapCreator>().DrawMapFromMission(mission);
            Object.FindFirstObjectByType<MapPreviewController>().CenterCameraToTilemap(mission);
        }

        private void SetWaves()
        {
            if (mission.WavesCount <= 0 || mission.CreepStatsPerWave.Count <= 0)
            {
                Debug.LogError($"Mission {mission.MissionIndex} has no waves");
                return;
            }

            if (mission.WavesCount != mission.CreepStatsPerWave.Count)
            {
                Debug.LogError($"Mission {mission.MissionIndex} : Unequal number of waves and creep stats");
                return;
            }

            if (waveLines.Count < mission.WavesCount)
            {
                while (waveLines.Count != mission.WavesCount)
                {
                    WaveLine waveLine = waveLinePrefab.Instantiate().Q<WaveLine>("WaveLine");
                    waveLine.Init();
                    wavesScroll.Add(waveLine);
                    waveLines.Add(waveLine);
                }
            }
            else if (waveLines.Count > mission.WavesCount)
            {
                int d = waveLines.Count - mission.WavesCount;
                for (int i = 0; i < d; i++)
                {
                    waveLines[^(i + 1)].Hide();
                }
            }

            int[] creepCounts = GetCreepCounts();

            for (int i = 0; i < creepCounts.Length; i++)
            {
                waveLines[i].SetWave(mission.CreepStatsPerWave[i], creepCounts[i]);
                waveLines[i].Show();
            }
        }

        private int[] GetCreepCounts()
        {
            int[] unitsCount = new int[mission.WavesCount];
            foreach (var spawnGroup in mission.SpawnData)
            {
                for (int i = 0; i < spawnGroup.Waves.Count; i++)
                {
                    unitsCount[spawnGroup.Waves[i].WaveNum] += spawnGroup.Waves[i].Count;
                }
            }

            return unitsCount;
        }

        private void UpdateTicketsCount() => ticketsLabel.text = DataManager.Instance.GameData.Tickets < 99 ? DataManager.Instance.GameData.Tickets.ToString() : "99+";

        private void OnWavesButtonClick(ClickEvent clk)
        {
            if (wavesPanel.style.visibility == Visibility.Hidden)
                wavesScroll.scrollOffset = Vector2.zero;
            PlaySound2D(wavesPanel.style.opacity != 0 ? SoundKey.Menu_mission_preview_off : SoundKey.Menu_mission_preview_on);

            AdditionalPanelTween(wavesPanel, wavesScroll, 0.3f).Play();
        }

        private void OnMapButtonClick(ClickEvent clk)
        {
            AdditionalPanelTween(mapPanel, map, 0.3f).Play();
            PlaySound2D(mapPanel.style.opacity != 0 ? SoundKey.Menu_mission_preview_off : SoundKey.Menu_mission_preview_on);
        }

        private Tween AdditionalPanelTween(VisualElement panel, VisualElement content, float duration)
        {
            if (wavesPanelWidth <= 0 || mapPanelWidth <= 0)
            {
                wavesPanelWidth = wavesPanel.resolvedStyle.width;
                mapPanelWidth = mapPanel.resolvedStyle.width;
            }

            float targetWidth = panel.name == mapPanel.name ? mapPanelWidth : wavesPanelWidth;
            bool isOpen = panel.style.visibility == Visibility.Visible;
            Sequence seq = DOTween.Sequence();
            if (isOpen)
            {
                seq.Append(uiHelper.GetMenuPanelFadeTween(content, false, true)
                    .OnComplete(() => content.style.display = DisplayStyle.None));
                seq.Append(uiHelper.ChangeWidth(panel, targetWidth, 0.1f, duration));
                seq.OnComplete(() => panel.style.visibility = Visibility.Hidden);
                seq.Insert(0.2f, uiHelper.FadeTween(panel, 1, 0, 0.2f));
                //PlaySound(SoundKey.Interface_pause_off);
            }
            else
            {
                panel.style.opacity = 0;
                panel.style.width = 0;
                panel.style.visibility = Visibility.Visible;
                seq.Append(uiHelper.ChangeWidth(panel, 0.1f, targetWidth, duration));
                seq.Append(uiHelper.GetMenuPanelFadeTween(content, true, true)
                    .OnStart(() => content.style.display = DisplayStyle.Flex));
                seq.Insert(0, uiHelper.FadeTween(panel, 0, 1, 0.2f));
                //PlaySound(SoundKey.Interface_pause_on);
            }

            return seq;
        }

        private void Hide()
        {
            Tween animation = UIHelper.Instance.GetHideWindowTween(this, background);
            animation.OnComplete(() =>
            {
                mapPanel.style.visibility = Visibility.Hidden;
                mapPanel.style.opacity = 0;
                wavesPanel.style.visibility = Visibility.Hidden;
                wavesPanel.style.opacity = 0;
            });
            animation.Play();

            Messenger<float>.Broadcast(UIEvents.OnUIAnimation, animation.Duration(), MessengerMode.DONT_REQUIRE_LISTENER);
        }

        private void OnStartClick(ClickEvent clk)
        {
            if (isHard && !DataManager.Instance.GameData.SkipAds)
            {
                if (DataManager.Instance.GameData.Tickets < 1)
                {
                    confirmWindow.SetUp(TicketIcon, LocalizationManager.GetTranslation("ConfirmWindow/NotEnoughTickets_desc"),
                        () => DataManager.Instance.GameData.BuyTickets(1, DataManager.TicketHardPrice));
                    confirmWindow.Show();
                    return;
                }
                DataManager.Instance.GameData.SpendTicket();
            }
            StartGame();
        }

        private async void StartGame()
        {
            bool skipDialog =
#if UNITY_EDITOR
                GameServices.Instance.SkipAllDialogs ||
#endif
                (PlayerPrefs.GetInt(PrefKeys.SkipOldDialogs) == 1 && DataManager.Instance.GameData.LastDialogBefore >= mission.MissionIndex || isHard);

            if (!skipDialog)
            {
                dialogWindow.ShowDialog(mission.MissionIndex, true);
                while (dialogWindow.IsShowing)
                    await Awaitable.NextFrameAsync();
            }

            InGameCheats.MissionStartLoadTime = Time.realtimeSinceStartupAsDouble;
            DOTween.KillAll();
            LoadingScreen.Instance.Show((() =>
            {
                GameServices.Instance.CurrentMission = mission;
                GameServices.Instance.IsHard = isHard;
                SceneManager.LoadScene(1, LoadSceneMode.Single);
            }));
        }

        private void OnCloseClick(ClickEvent clk) => Hide();

        public void UpdateLocalization()
        {
            rewardsLabel.text = LocalizationManager.GetTranslation("Menu/Rewards");
            startButton.SetText(LocalizationManager.GetTranslation("Start"));
        }
    }
}