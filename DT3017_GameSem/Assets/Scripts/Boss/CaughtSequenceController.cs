using System.Collections;
using DialogueEditor;
using UnityEngine;
using UnityEngine.Events;

public class CaughtSequenceController : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private PlayerResetController playerResetController;
    [SerializeField] private DailyQuotaManager quotaManager;
    [SerializeField] private DayCycleManager dayCycleManager;

    public enum CaughtReason
    {
        PlayerSeen,
        EmptyWorkstation,
        OpenDrawer
    }

    [Header("Caught Screen")]
    [Tooltip("Parent of the three reason screens. Keep this separate from the reason roots.")]
    [SerializeField] private GameObject caughtScreenRoot;
    [SerializeField] private GameObject playerSeenRoot;
    [SerializeField] private GameObject emptyWorkstationRoot;
    [SerializeField] private GameObject openDrawerRoot;
    [SerializeField, Min(0f)] private float displayDuration = 3f;

    [Header("Optional Events")]
    [SerializeField] private UnityEvent onSequenceStarted;
    [SerializeField] private UnityEvent onSequenceFinished;

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool sequenceRunning;

    private BossController activeBoss;
    private Coroutine displayRoutine;

    public bool SequenceRunning => sequenceRunning;

    private void Awake()
    {
        if (quotaManager == null)
        {
            quotaManager = DailyQuotaManager.Instance;
        }

        if (dayCycleManager == null)
            dayCycleManager = GetComponent<DayCycleManager>() ??
                              FindFirstObjectByType<DayCycleManager>();
        if (caughtScreenRoot != null)
            caughtScreenRoot.SetActive(false);
    }

    public void PlayCaughtSequence(BossController boss, CaughtReason reason)
    {
        if (sequenceRunning)
        {
            return;
        }

        sequenceRunning = true;
        activeBoss = boss;
        dayCycleManager?.SetSequenceTimerPaused(this, true);

        if (ToolSelectionUI.Instance != null && ToolSelectionUI.Instance.IsOpen)
        {
            ToolSelectionUI.Instance.CloseForExternalSequence();
        }

        // End through the dialogue package's public API before locking controls.
        // Its OnConversationEnded event may temporarily re-enable the player.
        if (ConversationManager.Instance != null &&
            ConversationManager.Instance.IsConversationActive)
        {
            ConversationManager.Instance.EndConversation();
        }

        if (activeBoss != null)
        {
            activeBoss.SetSequencePaused(true);
            activeBoss.ResetOutsideAndRestartTimer();
            activeBoss.SetSequencePaused(true);
        }

        if (playerResetController != null)
        {
            playerResetController.LockPlayer();
            playerResetController.ResetPlayerAndStation();
        }

        if (quotaManager != null)
        {
            quotaManager.AddCaughtPenalty();
        }

        ShowCaughtScreen(reason);

        onSequenceStarted?.Invoke();
        displayRoutine = StartCoroutine(DisplayCaughtScreen());
    }

    private void ShowCaughtScreen(CaughtReason reason)
    {
        playerSeenRoot?.SetActive(false);
        emptyWorkstationRoot?.SetActive(false);
        openDrawerRoot?.SetActive(false);

        GameObject reasonRoot = reason switch
        {
            CaughtReason.PlayerSeen => playerSeenRoot,
            CaughtReason.EmptyWorkstation => emptyWorkstationRoot,
            CaughtReason.OpenDrawer => openDrawerRoot,
            _ => null
        };

        if (caughtScreenRoot == null || reasonRoot == null)
            Debug.LogWarning("Caught screen or its reason root is not assigned.", this);

        reasonRoot?.SetActive(true);
        caughtScreenRoot?.SetActive(true);
    }

    private IEnumerator DisplayCaughtScreen()
    {
        yield return new WaitForSecondsRealtime(displayDuration);
        displayRoutine = null;
        FinishSequence();
    }

    private void HideCaughtScreen()
    {
        if (caughtScreenRoot != null)
            caughtScreenRoot.SetActive(false);
        playerSeenRoot?.SetActive(false);
        emptyWorkstationRoot?.SetActive(false);
        openDrawerRoot?.SetActive(false);
    }

    private void FinishSequence()
    {
        if (!sequenceRunning)
        {
            return;
        }

        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }
        HideCaughtScreen();

        if (playerResetController != null)
        {
            playerResetController.UnlockPlayer();
        }

        if (activeBoss != null)
        {
            activeBoss.SetSequencePaused(false);
        }

        activeBoss = null;
        sequenceRunning = false;
        dayCycleManager?.SetSequenceTimerPaused(this, false);
        onSequenceFinished?.Invoke();
    }

    private void OnDisable()
    {
        if (sequenceRunning) FinishSequence();
    }
}
