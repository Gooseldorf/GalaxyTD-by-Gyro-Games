using Data.Managers;
using UI;
using UnityEngine;
using UnityEngine.UIElements;

public class CheatPanel : VisualElement
{
    private const int cashCount = 10000;

    public new class UxmlFactory : UxmlFactory<CheatPanel>
    {
    }

    private ClickableVisualElement graphicsButton;

    private ClickableVisualElement closeButton;
    private ClickableVisualElement cashButton;
    private ClickableVisualElement win2Button;
    private ClickableVisualElement win1Button;
    private ClickableVisualElement win3Button;
    private ClickableVisualElement loseButton;
    private ClickableVisualElement increaseTimeScale;
    private ClickableVisualElement decreaseTimeScale;
    private ClickableVisualElement doubleTimeScale;
    private ClickableVisualElement halveTimeScale;
    private ClickableVisualElement timeSkip10Sec;
    private ClickableVisualElement timeSkip30Sec;
    private ClickableVisualElement timeSkip180Sec;
    private ClickableVisualElement nextWaveButton;

    private ClickableVisualElement buildTowersButton;

    private SelectableElement flowFieldToggle;
    private SelectableElement inOutToggle;
    private SelectableElement obstaclesToggle;
    private SelectableElement deathMapToggle;
    private SelectableElement towerDamageToggle;
    private SelectableElement towerKillsToggle;
    private SelectableElement totalCostToggle;
    private SelectableElement realDpsPerCostToggle;
    private SelectableElement fpsToggle;
    private Label currentTimeScale;

    private FPSCounterPanel fpsCounterPanel;

    private FlowFieldVisualizator flowField;

    public void Init(FPSCounterPanel fpsCounterPanel)
    {
        this.fpsCounterPanel = fpsCounterPanel;

        flowField = GameObject.Find("FlowFieldVisualizator").GetComponent<FlowFieldVisualizator>();

        graphicsButton = this.Q<ClickableVisualElement>("GraphicsQualityButton");
        graphicsButton.RegisterCallback<ClickEvent>(OnGraphicsQualityClick);

        buildTowersButton = this.Q<ClickableVisualElement>("BuildButton");
        closeButton = this.Q<ClickableVisualElement>("CloseButton");
        cashButton = this.Q<ClickableVisualElement>("AddCash");
        win2Button = this.Q<ClickableVisualElement>("Win2Button");
        win1Button = this.Q<ClickableVisualElement>("Win1Button");
        win3Button = this.Q<ClickableVisualElement>("Win3Button");
        loseButton = this.Q<ClickableVisualElement>("LoseButton");
        increaseTimeScale = this.Q<ClickableVisualElement>("IncreaseTimeScale");
        decreaseTimeScale = this.Q<ClickableVisualElement>("DecreaseTimeScale");
        doubleTimeScale = this.Q<ClickableVisualElement>("DoubleTimeScale");
        halveTimeScale = this.Q<ClickableVisualElement>("HalveTimeScale");
        timeSkip10Sec = this.Q<ClickableVisualElement>("10Seconds");
        timeSkip30Sec = this.Q<ClickableVisualElement>("30Seconds");
        timeSkip180Sec = this.Q<ClickableVisualElement>("180Seconds");
        currentTimeScale = this.Q<Label>("CurrentTimeScale");
        flowFieldToggle = this.Q<SelectableElement>("FlowFieldToggle");
        inOutToggle = this.Q<SelectableElement>("InOutToggle");
        obstaclesToggle = this.Q<SelectableElement>("ObstaclesToggle");
        deathMapToggle = this.Q<SelectableElement>("DeathMapToggle");
        towerDamageToggle = this.Q<SelectableElement>("TowerDamageToggle");
        towerKillsToggle = this.Q<SelectableElement>("TowerKillsToggle");
        totalCostToggle = this.Q<SelectableElement>("TotalCostToggle");
        realDpsPerCostToggle = this.Q<SelectableElement>("RealDPSPerCostToggle");
        fpsToggle = this.Q<SelectableElement>("FpsToggle");
        nextWaveButton = this.Q<ClickableVisualElement>("NextWave");

        UpdateCurrentTimeScale();
        flowFieldToggle.Init();
        obstaclesToggle.Init();
        deathMapToggle.Init();
        towerDamageToggle.Init();
        towerKillsToggle.Init();
        totalCostToggle.Init();
        realDpsPerCostToggle.Init();
        fpsToggle.Init();
        inOutToggle.Init();

#if UNITY_EDITOR
        fpsToggle.SetSelected(true);
#else
        if (Debug.isDebugBuild)
            fpsToggle.SetSelected(true);
#endif


        buildTowersButton.RegisterCallback<ClickEvent>(OnBuildTowersClick);
        cashButton.RegisterCallback<ClickEvent>(OnCashButtonClick);
        closeButton.RegisterCallback<ClickEvent>(OnCloseButtonClick);
        win2Button.RegisterCallback<ClickEvent>(OnWin2ButtonClick);
        win1Button.RegisterCallback<ClickEvent>(OnWin1ButtonClick);
        win3Button.RegisterCallback<ClickEvent>(OnWin3ButtonClick);
        loseButton.RegisterCallback<ClickEvent>(OnLoseButtonClick);
        increaseTimeScale.RegisterCallback<ClickEvent>(OnIncreaseTimeScaleButtonClick);
        decreaseTimeScale.RegisterCallback<ClickEvent>(OnDecreaseTimeScaleButtonClick);
        doubleTimeScale.RegisterCallback<ClickEvent>(OnDoubleTimeScaleButtonClick);
        halveTimeScale.RegisterCallback<ClickEvent>(OnHalveTimeScaleButtonClick);
        timeSkip10Sec.RegisterCallback<ClickEvent>(OnTimeSkip10ButtonClick);
        timeSkip30Sec.RegisterCallback<ClickEvent>(OnTimeSkip30ButtonClick);
        timeSkip180Sec.RegisterCallback<ClickEvent>(OnTimeSkip180ButtonClick);
        flowFieldToggle.RegisterCallback<ClickEvent>(OnFlowFieldToggleClick);
        obstaclesToggle.RegisterCallback<ClickEvent>(OnObstaclesToggleClick);
        deathMapToggle.RegisterCallback<ClickEvent>(OnDeathMapToggleClick);
        towerDamageToggle.RegisterCallback<ClickEvent>(OnTowerDamageToggleClick);
        towerKillsToggle.RegisterCallback<ClickEvent>(OnTowerKillsToggleClick);
        totalCostToggle.RegisterCallback<ClickEvent>(OnTotalCostToggleClick);
        realDpsPerCostToggle.RegisterCallback<ClickEvent>(OnRealDpsPerCostToggleClick);
        fpsToggle.RegisterCallback<ClickEvent>(OnFpsToggleClick);
        nextWaveButton.RegisterCallback<ClickEvent>(OnNextWaveButtonClick);
        inOutToggle.RegisterCallback<ClickEvent>(OnInOutToggleButtonClick);

        Show(false);

        //OnObstaclesToggleClick(new ClickEvent()); //TODO: For movement testing. DELETE!!
        //OnFlowFieldToggleClick(new ClickEvent()); //TODO: For movement testing. DELETE!!
    }

    private void OnGraphicsQualityClick(ClickEvent evt)
    {
        Debug.Log("OnGraphicsQualityClick");
        int graphics = (int)ScreenResolutionManager.GraphicsQuality;

        graphics++;
        if (graphics > 3)
            graphics = 1;
        ScreenResolutionManager.GraphicsQuality = (GraphicsQuality)graphics;
    }

    private void OnBuildTowersClick(ClickEvent evt)
    {
        Debug.Log("OnBuildTowersClick");
    }

    public void Dispose()
    {
        graphicsButton.UnregisterCallback<ClickEvent>(OnGraphicsQualityClick);
        buildTowersButton.UnregisterCallback<ClickEvent>(OnBuildTowersClick);
        closeButton.UnregisterCallback<ClickEvent>(OnCloseButtonClick);
        cashButton.UnregisterCallback<ClickEvent>(OnCashButtonClick);
        win2Button.UnregisterCallback<ClickEvent>(OnWin2ButtonClick);
        win1Button.UnregisterCallback<ClickEvent>(OnWin1ButtonClick);
        win3Button.UnregisterCallback<ClickEvent>(OnWin3ButtonClick);
        loseButton.UnregisterCallback<ClickEvent>(OnLoseButtonClick);
        increaseTimeScale.UnregisterCallback<ClickEvent>(OnIncreaseTimeScaleButtonClick);
        decreaseTimeScale.UnregisterCallback<ClickEvent>(OnDecreaseTimeScaleButtonClick);
        doubleTimeScale.UnregisterCallback<ClickEvent>(OnDoubleTimeScaleButtonClick);
        halveTimeScale.UnregisterCallback<ClickEvent>(OnHalveTimeScaleButtonClick);
        timeSkip10Sec.UnregisterCallback<ClickEvent>(OnTimeSkip10ButtonClick);
        timeSkip30Sec.UnregisterCallback<ClickEvent>(OnTimeSkip30ButtonClick);
        timeSkip180Sec.UnregisterCallback<ClickEvent>(OnTimeSkip180ButtonClick);
        flowFieldToggle.UnregisterCallback<ClickEvent>(OnFlowFieldToggleClick);
        obstaclesToggle.UnregisterCallback<ClickEvent>(OnObstaclesToggleClick);
        deathMapToggle.UnregisterCallback<ClickEvent>(OnDeathMapToggleClick);
        towerDamageToggle.UnregisterCallback<ClickEvent>(OnTowerDamageToggleClick);
        towerKillsToggle.UnregisterCallback<ClickEvent>(OnTowerKillsToggleClick);
        totalCostToggle.UnregisterCallback<ClickEvent>(OnTotalCostToggleClick);
        realDpsPerCostToggle.UnregisterCallback<ClickEvent>(OnRealDpsPerCostToggleClick);
        fpsToggle.UnregisterCallback<ClickEvent>(OnFpsToggleClick);
        nextWaveButton.UnregisterCallback<ClickEvent>(OnNextWaveButtonClick);
        inOutToggle.UnregisterCallback<ClickEvent>(OnInOutToggleButtonClick);
    }

    private void OnCloseButtonClick(ClickEvent clk)
    {
        Show(false);
        TouchCamera.Instance.CanDrag = true;
        GameServices.Instance.SetPause(false);
    }

    private void OnCashButtonClick(ClickEvent clk)
    {
        InGameCheats.AddCash(cashCount);
    }

    private void OnWin3ButtonClick(ClickEvent clk)
    {
        InGameCheats.WinLevelStars(3);
        Show(false);
    }

    private void OnWin2ButtonClick(ClickEvent clk)
    {
        InGameCheats.WinLevelStars(2);
        Show(false);
    }

    private void OnWin1ButtonClick(ClickEvent clk)
    {
        InGameCheats.WinLevelStars(1);
        Show(false);
    }

    private void OnLoseButtonClick(ClickEvent clk)
    {
        InGameCheats.LoseLevel();
        Show(false);
    }

    private void OnIncreaseTimeScaleButtonClick(ClickEvent clk)
    {
        InGameCheats.IncreaseTimeScale(.5f);
        UpdateCurrentTimeScale();
    }

    private void OnDecreaseTimeScaleButtonClick(ClickEvent clk)
    {
        InGameCheats.DecreaseTimeScale(.5f);
        UpdateCurrentTimeScale();
    }

    private void OnDoubleTimeScaleButtonClick(ClickEvent clk)
    {
        InGameCheats.DoubleTimeScale();
        UpdateCurrentTimeScale();
    }

    private void OnHalveTimeScaleButtonClick(ClickEvent clk)
    {
        InGameCheats.HalveTimeScale();
        UpdateCurrentTimeScale();
    }

    private void UpdateCurrentTimeScale()
    {
        currentTimeScale.text = GameServices.Instance.CurrentTimeScale.ToString();
    }

    private void OnTimeSkip10ButtonClick(ClickEvent clk) => SkipTime(10);
    private void OnTimeSkip30ButtonClick(ClickEvent clk) => SkipTime(30);
    private void OnTimeSkip180ButtonClick(ClickEvent clk) => SkipTime(180);

    private void SkipTime(float time)
    {
        //time skip logic
        Debug.Log($"TIME SKIPPED {time}");
    }

    private void OnNextWaveButtonClick(ClickEvent clk)
    {
        GameServices.Instance.SkipTimeToNextWave();
    }

    private void OnFlowFieldToggleClick(ClickEvent clk)
    {
        flowFieldToggle.SetSelected(!flowFieldToggle.Selected);
        if (flowFieldToggle.Selected)
        {
            flowField.ShowFlowField();
            inOutToggle.style.display = DisplayStyle.Flex;
        }
        else
        {
            flowField.HideFlowField();
            inOutToggle.style.display = DisplayStyle.None;
        }

        string text = flowFieldToggle.Selected ? "ON" : "OFF";
        Debug.Log($"Flow Field {text}");
    }

    private void OnInOutToggleButtonClick(ClickEvent clk)
    {
        inOutToggle.SetSelected(!inOutToggle.Selected);

        if (!inOutToggle.Selected)
        {
            flowField.Direction = FlowFieldVisualizator.ShowingDirection.InDirection;
        }
        else
        {
            flowField.Direction = FlowFieldVisualizator.ShowingDirection.OutDirection;
        }
    }

    private void OnObstaclesToggleClick(ClickEvent clk)
    {
        obstaclesToggle.SetSelected(!obstaclesToggle.Selected);

        if (obstaclesToggle.Selected)
            flowField.ShowObstacles();
        else
            flowField.HideObstacles();
    }

    private void OnTowerDamageToggleClick(ClickEvent clk)
    {
        towerDamageToggle.SetSelected(!towerDamageToggle.Selected);
        string text = towerDamageToggle.Selected ? "ON" : "OFF";
        Debug.Log($"Tower damage {text}");
    }

    private void OnTowerKillsToggleClick(ClickEvent clk)
    {
        towerKillsToggle.SetSelected(!towerKillsToggle.Selected);
        string text = towerKillsToggle.Selected ? "ON" : "OFF";
        Debug.Log($"Tower kills {text}");
    }

    private void OnTotalCostToggleClick(ClickEvent clk)
    {
        totalCostToggle.SetSelected(!totalCostToggle.Selected);
        string text = totalCostToggle.Selected ? "ON" : "OFF";
        Debug.Log($"Total cost {text}");
    }

    private void OnRealDpsPerCostToggleClick(ClickEvent clk)
    {
        realDpsPerCostToggle.SetSelected(!realDpsPerCostToggle.Selected);
        string text = realDpsPerCostToggle.Selected ? "ON" : "OFF";
        Debug.Log($"Real dps per cost {text}");
    }

    private void OnDeathMapToggleClick(ClickEvent clk)
    {
        deathMapToggle.SetSelected(!deathMapToggle.Selected);
        string text = deathMapToggle.Selected ? "ON" : "OFF";
        Debug.Log($"Death map {text}");
    }

    private void OnFpsToggleClick(ClickEvent clk)
    {
        fpsToggle.SetSelected(!fpsToggle.Selected);
        fpsCounterPanel.Show(fpsToggle.Selected);
    }

    public void Show(bool show)
    {
        visible = show;
        if (show)
        {
            //TouchCamera.Instance.CanDrag = false;
            UpdateCurrentTimeScale();
        }
    }
}