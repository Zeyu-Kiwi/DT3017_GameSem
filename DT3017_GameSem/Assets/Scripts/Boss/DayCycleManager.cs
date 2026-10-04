using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DayCycleManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private DailyQuotaManager quotaManager;
    [SerializeField] private PlayerResetController playerResetController;
    [SerializeField] private BossController bossController;

    [Header("Boss Availability")]
    [Tooltip("Boss NPC is fully disabled on these day numbers. Other days enable normal boss behavior.")]
    [SerializeField] private List<int> bossDisabledDays = new List<int> { 1, 2 };

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

    private bool currentDayTimerEnabled = true;
    private float currentDayDurationSeconds;

    public event System.Action<int> DayStarted;

    public int CurrentDay => currentDay;
    public bool TransitionRunning => transitionRunning;
    public float RemainingDayTimeSeconds => remainingDayTimeSeconds;
    public float CurrentDayDurationSeconds => currentDayDurationSeconds;
    public bool TimerExpired => timerExpired;
    public bool IsDailyTimerEnabled => dailyTimerEnabled && currentDayTimerEnabled;
    public bool IsDailyTimerRunning => isActiveAndEnabled && IsDailyTimerEnabled &&
                                       !transitionRunning && !timerExpired;
    public UnityEvent OnTimerExpired => onTimerExpired;

    private void Awake()
    {
        currentDay = Mathf.Max(1, startingDay);
        ResetDailyTimer();

        if (GetComponent<DailyTimerHUD>() == null)
        {
            gameObject.AddComponent<DailyTimerHUD>();
        }

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
        ApplyBossDayAvailability();
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
            // Set this before invoking events so callbacks cannot expire this timer twice.
            timerExpired = true;
            onTimerExpired?.Invoke();
        }
    }

    public void SetDailyTimerEnabled(bool enabled)
    {
        dailyTimerEnabled = enabled;
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

    public bool TryBeginNextDay()
    {
        return TryBeginNextDay(true);
    }

    public bool TryBeginNextDay(bool requireDailyQuota)
    {
        var tutorial = GetComponent<DayOneTutorial>();
        if (tutorial != null && !tutorial.CanSleep) return false;
        requireDailyQuota = requireDailyQuota && !DeveloperGameOptions.SkipDailyQuotaEnabled;
        if (!isActiveAndEnabled || transitionRunning ||
            (requireDailyQuota && (quotaManager == null || !quotaManager.HasMetQuota)))
        {
            return false;
        }

        StartCoroutine(NextDayRoutine(requireDailyQuota));
        return true;
    }

    private IEnumerator NextDayRoutine(bool requireDailyQuota)
    {
        transitionRunning = true;

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

        if (requireDailyQuota && (quotaManager == null ||
            !quotaManager.ConsumeRequiredShirtsForDayEnd()))
        {
            Debug.LogError(
                "Day transition started, but the required shirts could not be removed.",
                this);
            yield return CancelTransitionRoutine();
            yield break;
        }

        if (playerResetController != null)
        {
            playerResetController.ResetStationAndReturnPlayerTo(bedWakeUpPoint);
        }

        if (bossController != null)
        {
            bossController.ResetOutsideAndRestartTimer();
            bossController.SetSequencePaused(true);
        }

        currentDay++;
        ApplyBossDayAvailability();
        ResetDailyTimer();
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
        if (!Application.isPlaying || bossController == null) return;
        bool available = bossDisabledDays == null || !bossDisabledDays.Contains(CurrentDay);
        if (!available) bossController.ResetOutsideAndRestartTimer();
        bossController.gameObject.SetActive(available);
    }

    private void RefreshDayUI()
    {
        if (dayText != null)
        {
            dayText.text = "Day " + currentDay;
        }
    }
}
