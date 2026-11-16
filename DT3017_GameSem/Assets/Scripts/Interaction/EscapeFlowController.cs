using UnityEngine;

/// <summary>Stops the day and boss from an escape's first cutscene through its ending UI.</summary>
[DisallowMultipleComponent]
public sealed class EscapeFlowController : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private DayCycleManager dayCycleManager;
    [SerializeField] private BossController bossController;

    [Header("Escape HUD")]
    [Tooltip("The parent containing the day, timer and quota UI. Hidden during escape and restored if the escape is cancelled.")]
    [SerializeField] private GameObject gameplayHudRoot;

    [Header("End Game UI")]
    [Tooltip("Assign the artist's inactive end-game panel. Place it with the black screen UI; it moves to the top of its siblings when shown.")]
    [SerializeField] private GameObject endGamePanel;

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool escapeStarted;
    [SerializeField] private bool endGameShown;

    private FirstPersonController lockedPlayer;
    private PlayerInteractor lockedInteractor;
    private bool gameplayHudWasActive;

    public bool IsEscapeActive => escapeStarted;
    public bool IsEndGameShown => endGameShown;

    private void Awake()
    {
        ResolveSystems();
        if (endGamePanel != null)
            endGamePanel.SetActive(false);
    }

    /// <returns>True only when this call started a new escape.</returns>
    public bool BeginEscape()
    {
        if (escapeStarted) return false;

        ResolveSystems();
        escapeStarted = true;
        dayCycleManager?.SetSequenceTimerPaused(this, true);
        bossController?.SetEscapePaused(true);
        if (gameplayHudRoot != null)
        {
            gameplayHudWasActive = gameplayHudRoot.activeSelf;
            gameplayHudRoot.SetActive(false);
        }
        return true;
    }

    /// <summary>Use only when an escape cutscene is interrupted before completion.</summary>
    public void CancelEscape()
    {
        if (!escapeStarted || endGameShown) return;
        ReleaseEscape();
    }

    /// <summary>Show the ending UI once the final gate's screen is fully black.</summary>
    public void CompleteEscape(GameObject player)
    {
        BeginEscape();
        if (endGameShown) return;

        endGameShown = true;
        if (player != null)
        {
            lockedPlayer = player.GetComponent<FirstPersonController>();
            lockedInteractor = player.GetComponent<PlayerInteractor>();
            lockedPlayer?.LockMovement(this, true);
            lockedInteractor?.LockInteraction(this);
            player.GetComponent<PlayerInteractUI>()?.Hide();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (endGamePanel != null)
        {
            endGamePanel.transform.SetAsLastSibling();
            endGamePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Escape finished, but no End Game Panel is assigned yet.", this);
        }
    }

    private void ResolveSystems()
    {
        if (dayCycleManager == null)
            dayCycleManager = GetComponent<DayCycleManager>() ??
                              FindFirstObjectByType<DayCycleManager>();
        if (bossController == null)
            bossController = FindFirstObjectByType<BossController>();
    }

    private void ReleaseEscape()
    {
        dayCycleManager?.SetSequenceTimerPaused(this, false);
        bossController?.SetEscapePaused(false);
        if (gameplayHudRoot != null)
            gameplayHudRoot.SetActive(gameplayHudWasActive);
        lockedPlayer?.UnlockMovement(this);
        lockedInteractor?.UnlockInteraction(this);
        lockedPlayer = null;
        lockedInteractor = null;
        if (endGamePanel != null)
            endGamePanel.SetActive(false);
        endGameShown = false;
        escapeStarted = false;
    }

    private void OnDisable()
    {
        if (escapeStarted) ReleaseEscape();
    }
}
