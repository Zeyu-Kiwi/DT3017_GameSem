using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("UI")]
    [Tooltip("Assign the child UI object that contains the pause menu. Keep that child inactive before Play mode.")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private Button resumeButton;

    [Header("Quit To Menu")]
    [SerializeField] private Button quitButton;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private int mainMenuSceneIndex = 0;

    [Header("Day Display")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timeText; // optional
    [SerializeField] private DayCycleManager dayCycle;

    [Header("Player")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private PlayerSeatController playerSeat;
    [SerializeField] private CraftingStation craftingStation;
    [SerializeField] private VentInspectionController ventInspection;

    [Header("Audio")]
    [SerializeField] private bool pauseAudio = true;

    private float previousTimeScale = 1f;
    private bool previousAudioPause;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private bool isQuitting;

    public bool IsPaused { get; private set; }
    private int lastResumeFrame = -1;
    public bool ResumedThisFrame => lastResumeFrame == Time.frameCount;

    private void Awake()
    {
        CachePlayerReferences();

        if (pauseMenuRoot == gameObject)
        {
            Debug.LogError(
                "Pause Menu Root must be a separate child object. Otherwise hiding it also disables PauseMenuController.",
                this);
            pauseMenuRoot = null;
        }

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitToMenu);
        }
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitToMenu);
        }

        if (IsPaused)
        {
            RestoreGameplayState();
        }
    }

    private void Update()
    {
        if (isQuitting || !Input.GetKeyDown(pauseKey))
        {
            return;
        }

        if (IsPaused)
        {
            ResumeGame();
        }
        else if (CanOpenPauseMenu())
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (IsPaused || !CanOpenPauseMenu())
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        IsPaused = true;
        playerController.LockMovement(this, true);
        playerInteractor?.LockInteraction(this);

        Time.timeScale = 0f;
        if (pauseAudio)
        {
            AudioListener.pause = true;
        }

        RefreshDayDisplay();

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!IsPaused || isQuitting)
        {
            return;
        }

        RestoreGameplayState();
    }

    public void QuitToMenu()
    {
        if (isQuitting || !IsPaused)
        {
            return;
        }

        isQuitting = true;
        StartCoroutine(FadeAndLoadMenu());
    }

    private void RefreshDayDisplay()
    {
        if (dayCycle == null)
        {
            return;
        }

        if (dayText != null)
        {
            dayText.text = "Day " + dayCycle.CurrentDay;
        }

        if (timeText != null)
        {
            if (dayCycle.IsDailyTimerEnabled)
            {
                int secs = Mathf.CeilToInt(dayCycle.RemainingDayTimeSeconds);
                timeText.text = string.Format("{0}:{1:00}", secs / 60, secs % 60);
            }
            else
            {
                timeText.text = "";
            }
        }
    }

    private IEnumerator FadeAndLoadMenu()
    {
        // Build a black full-screen overlay at runtime (no manual UI needed)
        GameObject canvasObj = new GameObject("RuntimeFadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; // draw above everything

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        Image fade = imageObj.AddComponent<Image>();
        fade.color = new Color(0f, 0f, 0f, 0f);
        fade.raycastTarget = true; // blocks clicks during the fade

        RectTransform rt = fade.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Keep it alive across the scene load so the menu can fade in from it
        DontDestroyOnLoad(canvasObj);

        // Fade to black using unscaled time, since timeScale is 0 while paused
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration);
            fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t));
            yield return null;
        }

        // Reset everything the pause froze
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        IsPaused = false; // stops OnDestroy restoring the gameplay cursor state

        // Hand the overlay to a helper that survives the load and fades it out
        canvasObj.AddComponent<FadeOutAfterLoad>().Begin(fade, fadeDuration);

        SceneManager.LoadScene(mainMenuSceneIndex);
    }

    private void RestoreGameplayState()
    {
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
        }

        Time.timeScale = previousTimeScale;
        if (pauseAudio)
        {
            AudioListener.pause = previousAudioPause;
        }

        playerInteractor?.UnlockInteraction(this);
        if (playerController != null)
        {
            playerController.UnlockMovement(this);
        }

        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        IsPaused = false;
        lastResumeFrame = Time.frameCount;
    }

    private bool CanOpenPauseMenu()
    {
        CachePlayerReferences();

        if (pauseMenuRoot == null || playerController == null ||
            Time.timeScale <= 0f ||
            (playerSeat != null && playerSeat.IsTransitioning))
        {
            return false;
        }

        // Crafting disables the player controller while the drawer is open.
        // Only the fully opened, input-ready drawer may be paused.
        if (craftingStation != null && craftingStation.IsDrawerReadyForInput)
        {
            return true;
        }

        // Vent inspection disables the normal player controller. Allow pausing
        // after its camera has arrived, while keeping its state intact.
        if (ventInspection != null && ventInspection.IsReadyForInput)
        {
            return true;
        }

        if (!playerController.isActiveAndEnabled || !playerController.CanLook)
        {
            return false;
        }

        return playerController.CanMove ||
               (playerSeat != null && playerSeat.IsSeated);
    }

    private void CachePlayerReferences()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FirstPersonController>();
        }

        if (playerInteractor == null && playerController != null)
        {
            playerInteractor = playerController.GetComponent<PlayerInteractor>();
        }

        if (playerSeat == null && playerController != null)
        {
            playerSeat = playerController.GetComponent<PlayerSeatController>();
        }

        if (craftingStation == null)
        {
            craftingStation = FindFirstObjectByType<CraftingStation>();
        }

        if (ventInspection == null)
        {
            ventInspection = FindFirstObjectByType<VentInspectionController>();
        }

        if (dayCycle == null)
        {
            dayCycle = FindFirstObjectByType<DayCycleManager>();
        }
    }

    private void OnValidate()
    {
        if (playerInteractor == null && playerController != null)
        {
            playerInteractor = playerController.GetComponent<PlayerInteractor>();
        }

        if (playerSeat == null && playerController != null)
        {
            playerSeat = playerController.GetComponent<PlayerSeatController>();
        }
    }
}

// Lives on the runtime fade canvas, survives the scene load, fades to clear, then removes itself.
public class FadeOutAfterLoad : MonoBehaviour
{
    private Image fade;
    private float duration;

    public void Begin(Image image, float fadeDuration)
    {
        fade = image;
        duration = fadeDuration;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return null; // wait a frame so the new scene is loaded

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            fade.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t));
            yield return null;
        }

        Destroy(gameObject);
    }
}