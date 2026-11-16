using System.Collections;
using System.Collections.Generic;
using DialogueEditor;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DayCycleManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private DailyQuotaManager quotaManager;
    [SerializeField] private PlayerResetController playerResetController;
    [SerializeField] private BossController bossController;
    [SerializeField] private DayPlanManager dayPlanManager;

    [Header("Day")]
    [SerializeField, Min(1)] private int startingDay = 1;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private Transform bedWakeUpPoint;

    [System.Serializable]
    public sealed class DayTimerOverride
    {
        [Min(1)] public int day = 1;
        public bool timerEnabled = true;
        [Min(0f)] public float durationSeconds = 600f;
    }

    [Header("Daily Timer")]
    [Tooltip("Disable to freeze the countdown on every day. Re-enabling resumes the remaining time.")]
    [SerializeField] private bool dailyTimerEnabled = true;
    [SerializeField, Min(0f)] private float defaultDayDurationSeconds = 600f;
    [Tooltip("Optional settings for specific day numbers. Days without an entry use the default duration. The first matching entry is used.")]
    [SerializeField] private List<DayTimerOverride> dayTimerOverrides = new List<DayTimerOverride>();
    [Tooltip("Invoked once when an enabled day's countdown reaches zero. Connect the desired timeout behavior here.")]
    [SerializeField] private UnityEvent onTimerExpired = new UnityEvent();

    [Header("Black Screen")]
    [SerializeField] private CanvasGroup blackScreen;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float blackScreenDuration = 1f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;

    [Header("Runtime State (Debug)")]
    [SerializeField] private int currentDay;
    [SerializeField] private bool transitionRunning;
    [SerializeField] private float remainingDayTimeSeconds;
    [SerializeField] private bool timerExpired;
    [SerializeField] private bool tutorialTimerPaused;

    private bool currentDayTimerEnabled = true;
    private float currentDayDurationSeconds;
    private string pendingDayEndNotification;

    /// <summary>Raised after the old day's quota is resolved, while the screen is black.</summary>
    public event System.Action<int> DayEnding;
    public event System.Action<int> DayStarted;

    public int CurrentDay => currentDay;
    public bool TransitionRunning => transitionRunning;
    private readonly System.Collections.Generic.HashSet<object> sequenceTimerPauses = new System.Collections.Generic.HashSet<object>();

    public void SetSequenceTimerPaused(object owner, bool paused)
    {
        if (owner == null) throw new System.ArgumentNullException(nameof(owner));
        if (paused) sequenceTimerPauses.Add(owner);
        else sequenceTimerPauses.Remove(owner);
    }

    public float RemainingDayTimeSeconds => remainingDayTimeSeconds;
    public float CurrentDayDurationSeconds => currentDayDurationSeconds;
    public bool TimerExpired => timerExpired;
    public bool IsDailyTimerEnabled => dailyTimerEnabled && currentDayTimerEnabled;
    public bool IsDailyTimerRunning => isActiveAndEnabled && IsDailyTimerEnabled &&
                                       !transitionRunning && !timerExpired &&
                                       !tutorialTimerPaused && sequenceTimerPauses.Count == 0;
    public UnityEvent OnTimerExpired => onTimerExpired;

    private void Awake()
    {
        currentDay = Mathf.Max(1, startingDay);
        if (dayPlanManager == null) dayPlanManager = GetComponent<DayPlanManager>();
        ResetDailyTimer();

        if (quotaManager == null)
        {
            quotaManager = DailyQuotaManager.Instance;
        }

        if (blackScreen != null)
        {
            blackScreen.alpha = 0f;
            blackScreen.blocksRaycasts = false;
            blackScreen.interactable = false;
        }

        RefreshDayUI();
    }

    private void Update()
    {
        TickDailyTimer(Time.deltaTime);
    }

    private void TickDailyTimer(float deltaSeconds)
    {
        if (!IsDailyTimerRunning || deltaSeconds <= 0f)
        {
            return;
        }

        remainingDayTimeSeconds = Mathf.Max(0f, remainingDayTimeSeconds - deltaSeconds);
        if (remainingDayTimeSeconds <= 0f)
        {
            // Set this before beginning the transition so this timer cannot expire twice.
            timerExpired = true;
            TryBeginNextDayFromTimer();
            onTimerExpired?.Invoke();
        }
    }

    public void SetDailyTimerEnabled(bool enabled)
    {
        dailyTimerEnabled = enabled;
    }

    public void SetTutorialTimerPaused(bool paused, bool restartFullDuration = false)
    {
        tutorialTimerPaused = paused;
        if (paused && restartFullDuration)
        {
            // Keep the duration chosen by the Day Plan, including its override.
            remainingDayTimeSeconds = currentDayDurationSeconds;
            timerExpired = false;
        }
    }

    [ContextMenu("Reset Daily Timer")]
    public void ResetDailyTimer()
    {
        int dayNumber = currentDay > 0 ? currentDay : Mathf.Max(1, startingDay);
        currentDayTimerEnabled = true;
        currentDayDurationSeconds = Mathf.Max(0f, defaultDayDurationSeconds);

        if (dayTimerOverrides != null)
        {
            foreach (DayTimerOverride settings in dayTimerOverrides)
            {
                if (settings == null || settings.day != dayNumber)
                {
                    continue;
                }

                currentDayTimerEnabled = settings.timerEnabled;
                currentDayDurationSeconds = Mathf.Max(0f, settings.durationSeconds);
                break;
            }
        }

        remainingDayTimeSeconds = currentDayDurationSeconds;
        timerExpired = false;
    }

    public void ApplyDayPlanTimer(
        bool enabled,
        bool overrideDuration,
        float durationSeconds)
    {
        ResetDailyTimer();
        currentDayTimerEnabled = enabled;
        if (overrideDuration)
        {
            currentDayDurationSeconds = Mathf.Max(0f, durationSeconds);
            remainingDayTimeSeconds = currentDayDurationSeconds;
        }
    }

    public bool TryBeginNextDay()
    {
        return TryBeginNextDay(true);
    }

    public bool TryBeginNextDay(bool requireDailyQuota)
    {
        requireDailyQuota = requireDailyQuota && !DeveloperGameOptions.SkipDailyQuotaEnabled;
        if (!isActiveAndEnabled || transitionRunning ||
            (requireDailyQuota && (quotaManager == null || !quotaManager.HasMetQuota)))
        {
            return false;
        }

        transitionRunning = true;
        StartCoroutine(NextDayRoutine(requireDailyQuota, false));
        return true;
    }

    private bool TryBeginNextDayFromTimer()
    {
        if (quotaManager == null)
        {
            quotaManager = DailyQuotaManager.Instance;
        }

        if (!isActiveAndEnabled || transitionRunning || quotaManager == null)
        {
            return false;
        }

        transitionRunning = true;
        StartCoroutine(NextDayRoutine(false, true));
        return true;
    }

    private IEnumerator NextDayRoutine(
        bool requireDailyQuota,
        bool causedByTimerExpiry)
    {
        pendingDayEndNotification = null;
        CancelActiveInterfacesForDayTransition();

        if (playerResetController != null)
        {
            playerResetController.LockPlayer();
        }

        if (bossController != null)
        {
            bossController.SetSequencePaused(true);
        }

        if (blackScreen != null)
        {
            blackScreen.blocksRaycasts = true;
            blackScreen.interactable = true;
            yield return FadeBlackScreen(0f, 1f, fadeOutDuration);
        }

        bool quotaResolved = true;
        if (causedByTimerExpiry)
        {
            int missingShirts = 0;
            bool drawerCleared = false;
            int nextDailyBaseQuota = quotaManager == null
                ? 0
                : quotaManager.CurrentDailyBaseQuota;
            quotaResolved = quotaManager != null &&
                quotaManager.ResolveTimerExpiredDay(
                    out missingShirts,
                    out nextDailyBaseQuota,
                    out drawerCleared);

            if (quotaResolved && missingShirts > 0)
            {
                pendingDayEndNotification =
                    "Yesterday's quota was not met. New quota: " +
                    nextDailyBaseQuota + "." +
                    (drawerCleared ? " Drawer emptied." : string.Empty);
            }
        }
        else if (requireDailyQuota)
        {
            quotaResolved = quotaManager != null &&
                quotaManager.ConsumeRequiredShirtsForDayEnd();
        }

        if (!quotaResolved)
        {
            Debug.LogError(
                "Day transition started, but the daily shirts could not be resolved.",
                this);
            yield return CancelTransitionRoutine();
            yield break;
        }

        DayEnding?.Invoke(currentDay);

        Transform nextSpawnPoint = dayPlanManager != null
            ? dayPlanManager.GetSpawnPointForDay(currentDay + 1, bedWakeUpPoint)
            : bedWakeUpPoint;
        if (playerResetController != null)
            playerResetController.ResetStationAndReturnPlayerTo(nextSpawnPoint);

        if (bossController != null)
        {
            bossController.ResetOutsideAndRestartTimer();
            bossController.SetSequencePaused(true);
        }

        currentDay++;
        if (dayPlanManager != null) dayPlanManager.ApplyDay(currentDay);
        else ResetDailyTimer();
        RefreshDayUI();

        if (quotaManager != null)
        {
            quotaManager.StartNewDay();
        }

        if (blackScreenDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(blackScreenDuration);
        }

        if (blackScreen != null)
        {
            yield return FadeBlackScreen(1f, 0f, fadeInDuration);
            blackScreen.blocksRaycasts = false;
            blackScreen.interactable = false;
        }

        if (playerResetController != null)
        {
            playerResetController.UnlockPlayer();
        }

        if (bossController != null)
        {
            bossController.SetSequencePaused(false);
        }

        transitionRunning = false;
        DayStarted?.Invoke(currentDay);

        if (!string.IsNullOrEmpty(pendingDayEndNotification))
        {
            string message = pendingDayEndNotification;
            pendingDayEndNotification = null;

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowMessage(message);
            }
            else
            {
                Debug.Log(message, this);
            }
        }
    }

    private void CancelActiveInterfacesForDayTransition()
    {
        if (ToolSelectionUI.Instance != null && ToolSelectionUI.Instance.IsOpen)
        {
            ToolSelectionUI.Instance.CloseForExternalSequence();
        }

        if (ConversationManager.Instance != null &&
            ConversationManager.Instance.IsConversationActive)
        {
            ConversationManager.Instance.EndConversationImmediately();
        }

        foreach (var gateExit in FindObjectsByType<GateExitCutscene>(FindObjectsSortMode.None))
            gateExit.CancelForExternalSequence();

        foreach (var traversal in FindObjectsByType<VentTraversalCutscene>(FindObjectsSortMode.None))
            traversal.CancelForExternalSequence();

        VentInspectionController ventInspection =
            FindFirstObjectByType<VentInspectionController>();
        if (ventInspection != null && ventInspection.IsInspecting)
        {
            ventInspection.CancelForExternalSequence();
        }

        CraftingStation craftingStation = FindFirstObjectByType<CraftingStation>();
        if (craftingStation != null)
        {
            craftingStation.ResetStationImmediately(false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DevModeScreen developerScreen = FindFirstObjectByType<DevModeScreen>();
        if (developerScreen != null && developerScreen.IsOpen)
        {
            developerScreen.SetOpen(false);
        }
#endif
    }

    private IEnumerator CancelTransitionRoutine()
    {
        if (blackScreen != null)
        {
            yield return FadeBlackScreen(blackScreen.alpha, 0f, fadeInDuration);
            blackScreen.blocksRaycasts = false;
            blackScreen.interactable = false;
        }

        if (playerResetController != null)
        {
            playerResetController.UnlockPlayer();
        }

        if (bossController != null)
        {
            bossController.SetSequencePaused(false);
        }

        transitionRunning = false;
    }

    private IEnumerator FadeBlackScreen(float from, float to, float duration)
    {
        if (blackScreen == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            blackScreen.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            blackScreen.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        blackScreen.alpha = to;
    }

    [ContextMenu("Refresh Boss Day Availability")]
    public void ApplyBossDayAvailability()
    {
        if (!Application.isPlaying) return;
        if (dayPlanManager != null) dayPlanManager.ApplyDay(CurrentDay);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void SetDayForDevelopment(int day)
    {
        if (!isActiveAndEnabled || transitionRunning) return;
        currentDay = Mathf.Max(1, day);
        bossController?.ResetOutsideAndRestartTimer();
        quotaManager?.StartNewDay();
        Transform spawnPoint = dayPlanManager != null
            ? dayPlanManager.GetSpawnPointForDay(currentDay, bedWakeUpPoint)
            : bedWakeUpPoint;
        playerResetController?.ResetStationAndReturnPlayerTo(spawnPoint);
        if (dayPlanManager != null) dayPlanManager.ApplyDay(currentDay);
        else ResetDailyTimer();
        RefreshDayUI();
        DayStarted?.Invoke(currentDay);
    }
#endif

    private void RefreshDayUI()
    {
        if (dayText != null)
        {
            dayText.text = "Day " + currentDay;
        }
    }
}
