using UnityEngine;
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

    [Header("Player")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteractor playerInteractor;

    [Header("Audio")]
    [SerializeField] private bool pauseAudio = true;

    private float previousTimeScale = 1f;
    private bool previousAudioPause;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    public bool IsPaused { get; private set; }

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
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }

        if (IsPaused)
        {
            RestoreGameplayState();
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(pauseKey))
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

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!IsPaused)
        {
            return;
        }

        RestoreGameplayState();
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
    }

    private bool CanOpenPauseMenu()
    {
        CachePlayerReferences();

        return pauseMenuRoot != null &&
               playerController != null &&
               playerController.isActiveAndEnabled &&
               playerController.CanMove &&
               playerController.CanLook &&
               Time.timeScale > 0f;
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
    }

    private void OnValidate()
    {
        if (playerInteractor == null && playerController != null)
        {
            playerInteractor = playerController.GetComponent<PlayerInteractor>();
        }
    }
}
