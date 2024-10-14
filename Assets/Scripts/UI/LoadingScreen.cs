using CardTD.Utilities;
using DarkTonic.MasterAudio;
using DG.Tweening;
using System;
using UI;
using UnityEngine;
using UnityEngine.UIElements;
using I2.Loc;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    private const float minimumShowTime = 3f;
    
    [SerializeField] private UIDocument uiDocument;
    
    public static LoadingScreen Instance;
    private VisualElement loadingScreen;
    private Label loadingLabel;
    private Label tooltipTitle;
    private Label tooltipText;
    private Tween tween;
    private float hideTime;

    public VisualElement LoadingScreenElement => loadingScreen;
    private bool isResolved => !float.IsNaN(loadingScreen.resolvedStyle.width);
    private UIHelper uiHelper;
    [NonSerialized] public bool UIDocumentsResolved = false;

    private void Awake()
    {
        UIDocumentsResolved = false;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            loadingScreen = uiDocument.rootVisualElement.Q<VisualElement>("LoadingScreen");
            loadingLabel = LoadingScreenElement.Q<Label>("LoadingLabel");
            tooltipTitle = LoadingScreenElement.Q<Label>("TooltipTitle");
            tooltipText = LoadingScreenElement.Q<Label>("TooltipText");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        uiHelper = UIHelper.Instance;
        hideTime = Time.unscaledTime + minimumShowTime;
        StartCoroutine(WaitForLoadingCompletion());
    }

    private void Start()
    {
        //hideTime = Time.unscaledTime;
        UpdateLocalization();
        Messenger.AddListener(UIEvents.LanguageChanged, UpdateLocalization);
        ChooseHint();
    }

    private void OnDestroy()
    {
        Messenger.RemoveListener(UIEvents.LanguageChanged, UpdateLocalization);
    }

    private IEnumerator WaitForLoadingCompletion()
    {
        while (!UIDocumentsResolved || !isResolved || !MusicManager.IsReady || Time.unscaledTime < hideTime)
        {
            //Debug.Log("Waiting for loading completion");
            //Debug.Log($"UI {UIDocumentsResolved}, isResolved {isResolved}, music {MusicManager.IsReady}, Time {Time.unscaledTime < hideTime}");
            yield return null;
        }

        UIDocumentsResolved = false;
        Hide();
    }

    private void ChooseHint()
    {
        loadingLabel.text = LocalizationManager.GetTranslation("Loading");

        int hintsCount = LocalizationManager.GetTermsList("Hints").Count / 2; // one key for title and one for desc

        if (hintsCount == 0)
        {
            Debug.LogError("is hintsCount 0");
            return;
        }
        
        int randomHint = UnityEngine.Random.Range(1, hintsCount + 1);

        tooltipTitle.text = LocalizationManager.GetTranslation($"Hints/Hint{randomHint}_title");
        tooltipText.text = LocalizationManager.GetTranslation($"Hints/Hint{randomHint}_desc");
    }

    public void Show(Action onComplete)
    {
        hideTime = Time.unscaledTime + minimumShowTime;
        ChooseHint();
        LoadingScreenElement.style.opacity = 0f;
        LoadingScreenElement.pickingMode = PickingMode.Position;
        tween = UIHelper.Instance.FadeTween(LoadingScreenElement, 0, 1, 0.5f);
        tween.SetUpdate(true);
        tween.OnComplete(onComplete.Invoke);
        tween.Play();
        StartCoroutine(WaitForLoadingCompletion());
    }

    private void Hide()
    {
        tween = UIHelper.Instance.FadeTween(LoadingScreenElement, 1f, 0, 0.5f);
        tween.OnComplete(() =>
        {
            LoadingScreenElement.style.opacity = 0;
            LoadingScreenElement.pickingMode = PickingMode.Ignore;
            Messenger.Broadcast(UIEvents.LoadingCompleted, MessengerMode.DONT_REQUIRE_LISTENER);
        });
        tween.SetUpdate(true);
        tween.Play();
    }

    private void KillTween()
    {
        if (tween != null)
        {
            tween.Kill(true);
            tween = null;
        }
    }

    private void UpdateLocalization()
    {
        uiHelper.SetLocalizationFont(LoadingScreenElement);
    }
}