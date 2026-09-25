using System.Collections;
using TMPro;
using UnityEngine;

public class DayCycleManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private DailyQuotaManager quotaManager;
    [SerializeField] private PlayerResetController playerResetController;
    [SerializeField] private BossController bossController;

    [Header("Day")]
    [SerializeField, Min(1)] private int startingDay = 1;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private Transform bedWakeUpPoint;

    [Header("Black Screen")]
    [SerializeField] private CanvasGroup blackScreen;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float blackScreenDuration = 1f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;

    [Header("Runtime State (Debug)")]
    [SerializeField] private int currentDay;
    [SerializeField] private bool transitionRunning;

    public int CurrentDay => currentDay;
    public bool TransitionRunning => transitionRunning;

    private void Awake()
    {
        currentDay = Mathf.Max(1, startingDay);

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

    public bool TryBeginNextDay()
    {
        if (transitionRunning ||
            quotaManager == null ||
            !quotaManager.HasMetQuota)
        {
            return false;
        }

        StartCoroutine(NextDayRoutine());
        return true;
    }

    private IEnumerator NextDayRoutine()
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

        if (quotaManager == null ||
            !quotaManager.ConsumeRequiredShirtsForDayEnd())
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

    private void RefreshDayUI()
    {
        if (dayText != null)
        {
            dayText.text = "Day " + currentDay;
        }
    }
}
