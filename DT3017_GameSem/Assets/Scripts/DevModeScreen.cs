#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Game.Visuals;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// This entire screen is omitted from non-development player builds.
[DefaultExecutionOrder(1000)]
public sealed class DevModeScreen : MonoBehaviour
{
    private GameObject screen;
    private TMP_Text timerText;
    private TMP_Text dayText;
    private TMP_Text timerStateText;
    private RectTransform optionsContent;
    private readonly System.Collections.Generic.List<System.Action> refreshOptions =
        new System.Collections.Generic.List<System.Action>();
    private bool optionsDirty;
    private DayCycleManager dayManager;
    private FirstPersonController player;
    private PlayerInteractor interactor;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private bool toggleKeyWasHeld;

    public bool IsOpen => screen != null && screen.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (FindFirstObjectByType<DevModeScreen>() != null) return;
        var host = new GameObject("Developer Screen");
        DontDestroyOnLoad(host);
        host.AddComponent<DevModeScreen>();
    }

    private void Awake()
    {
        BuildScreen();
        DevModeOptions.Changed += MarkOptionsDirty;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool toggleKeyHeld = UnityEngine.InputSystem.Keyboard.current != null &&
                             (UnityEngine.InputSystem.Keyboard.current.quoteKey.isPressed || UnityEngine.InputSystem.Keyboard.current.backquoteKey.isPressed);
#else
        bool toggleKeyHeld = Input.GetKey(KeyCode.Quote) || Input.GetKey(KeyCode.BackQuote);
#endif
        bool togglePressed = toggleKeyHeld && !toggleKeyWasHeld;
        toggleKeyWasHeld = toggleKeyHeld;
        if (togglePressed) SetOpen(!IsOpen);
        if (!IsOpen) return;
        // Other menus may also set the cursor; keep it usable while this screen is open.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshDetails();
    }

    public void SetOpen(bool open)
    {
        if (screen == null || open == IsOpen) return;
        if (open)
        {
            dayManager = FindFirstObjectByType<DayCycleManager>();
            player = FindFirstObjectByType<FirstPersonController>();
            interactor = player != null ? player.GetComponent<PlayerInteractor>() : null;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            player?.LockMovement(this);
            interactor?.LockInteraction(this);
            EnsureEventSystem();
            screen.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshDetails();
        }
        else
        {
            screen.SetActive(false);
            interactor?.UnlockInteraction(this);
            player?.UnlockMovement(this);
            if (player == null || player.CanLook)
            {
                Cursor.lockState = previousCursorLock;
                Cursor.visible = previousCursorVisible;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetOpen(false);
        dayManager = null;
        player = null;
        interactor = null;
    }

    private void OnDisable() => SetOpen(false);

    private void OnDestroy()
    {
        SetOpen(false);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DevModeOptions.Changed -= MarkOptionsDirty;
    }

    private void RefreshDetails()
    {
        if (optionsDirty) RebuildOptions();
        foreach (var refresh in refreshOptions) refresh();
        if (dayManager == null)
        {
            dayText.text = "DAILY TIMER";
            timerText.text = "--:--";
            timerStateText.text = "No Day Cycle Manager in this scene";
            return;
        }
        dayText.text = "DAY " + dayManager.CurrentDay + "  /  DAILY TIMER";
        int seconds = Mathf.CeilToInt(dayManager.RemainingDayTimeSeconds);
        timerText.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        timerStateText.text = !dayManager.IsDailyTimerEnabled ? "Disabled" :
            dayManager.TimerExpired ? "Expired" :
            dayManager.IsDailyTimerRunning ? "Running" : "Paused";
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var events = new GameObject("Developer EventSystem", typeof(EventSystem));
        events.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
        events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        events.AddComponent<StandaloneInputModule>();
#endif
    }

    private RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private TMP_Text Text(string name, Transform parent, string value, float size, Color color, float height)
    {
        var rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        var layout = rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        return text;
    }

    private UnityEngine.UI.Toggle Toggle(Transform parent, string name, UnityEngine.Events.UnityAction<bool> changed)
    {
        var row = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var layout = row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.minHeight = 42;
        layout.preferredHeight = 42;
        var hit = row.gameObject.AddComponent<UnityEngine.UI.Image>();
        hit.color = new Color(0.13f, 0.17f, 0.23f);
        var toggle = row.gameObject.AddComponent<UnityEngine.UI.Toggle>();
        var box = Rect("Checkbox", row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(22, 22), new Vector2(23, 0));
        var background = box.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(.32f, .38f, .46f);
        background.raycastTarget = false;
        var mark = Rect("Check", box, Vector2.zero, Vector2.one, new Vector2(-8, -8), Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
        mark.color = new Color(.35f, .88f, .8f);
        mark.raycastTarget = false;
        var label = Text("Label", row, name, 18, Color.white, 42);
        var labelRect = label.rectTransform;
        labelRect.offsetMin = new Vector2(48, 0);
        labelRect.offsetMax = new Vector2(-8, 0);
        toggle.targetGraphic = hit;
        toggle.graphic = mark;
        toggle.onValueChanged.AddListener(changed);
        return toggle;
    }

    private void MarkOptionsDirty() => optionsDirty = true;

    private void BuildOptionsList(Transform parent)
    {
        var scrollRoot = Rect("Options", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var layout = scrollRoot.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.minHeight = 120;
        layout.preferredHeight = 200;
        layout.flexibleHeight = 1;
        var scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30;
        var viewport = Rect("Viewport", scrollRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.065f, .085f, .12f);
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        optionsContent = Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
        optionsContent.pivot = new Vector2(.5f, 1);
        var rows = optionsContent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        rows.spacing = 8;
        rows.childControlHeight = true;
        rows.childControlWidth = true;
        rows.childForceExpandHeight = false;
        var fitter = optionsContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = optionsContent;
        RebuildOptions();
    }

    private void RebuildOptions()
    {
        optionsDirty = false;
        refreshOptions.Clear();
        foreach (Transform child in optionsContent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        foreach (var option in DevModeOptions.Options)
        {
            if (option.Read != null)
            {
                var toggle = Toggle(optionsContent, option.Label, value => option.Write(value));
                toggle.gameObject.name = option.Id;
                toggle.SetIsOnWithoutNotify(option.Read());
                refreshOptions.Add(() => toggle.SetIsOnWithoutNotify(option.Read()));
            }
            else
            {
                var rect = Rect(option.Id, optionsContent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var layout = rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                layout.minHeight = 42;
                layout.preferredHeight = 42;
                var background = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
                background.color = new Color(.15f, .25f, .3f);
                var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = background;
                button.onClick.AddListener(() => option.Execute());
                var label = Text("Label", rect, option.Label, 18, Color.white, 42);
                label.alignment = TextAlignmentOptions.Center;
                refreshOptions.Add(() => button.interactable = option.CanExecute == null || option.CanExecute());
            }
        }
        Canvas.ForceUpdateCanvases();
    }

    private void BuildScreen()
    {
        screen = Rect("DevModeCanvas", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        var canvas = screen.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = screen.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        screen.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var shade = Rect("Backdrop", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
        shade.color = new Color(0, 0, 0, .65f);
        var panel = Rect("Panel", screen.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(540, 580), Vector2.zero);
        panel.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.065f, .085f, .12f, 1);
        var content = panel.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        content.padding = new RectOffset(28, 28, 22, 22);
        content.spacing = 8;
        content.childControlHeight = true;
        content.childControlWidth = true;
        content.childForceExpandHeight = false;
        Text("Title", panel, "DEVELOPER MODE", 26, new Color(.35f, .88f, .8f), 38);
        dayText = Text("Day", panel, "DAILY TIMER", 15, new Color(.65f, .72f, .8f), 25);
        timerText = Text("Time", panel, "--:--", 48, Color.white, 66);
        timerStateText = Text("TimerState", panel, "", 16, new Color(.65f, .72f, .8f), 26);
        BuildOptionsList(panel);

        Text("Hint", panel, "Workstation detection still applies.\nPress apostrophe or backtick to close. Changes apply this session.", 14, new Color(.65f, .72f, .8f), 48);
        var closeRect = Rect("Close", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var closeLayout = closeRect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        closeLayout.preferredHeight = 34;
        closeLayout.minHeight = 34;
        var closeImage = closeRect.gameObject.AddComponent<UnityEngine.UI.Image>();
        closeImage.color = new Color(.15f, .25f, .3f);
        var close = closeRect.gameObject.AddComponent<UnityEngine.UI.Button>();
        close.targetGraphic = closeImage;
        close.onClick.AddListener(() => SetOpen(false));
        var closeLabel = Text("Label", closeRect, "Close", 16, Color.white, 34);
        closeLabel.alignment = TextAlignmentOptions.Center;
        screen.SetActive(false);
    }
}
#endif
