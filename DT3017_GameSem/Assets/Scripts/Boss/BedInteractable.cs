using UnityEngine;

public class BedInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DailyQuotaManager quotaManager;
    [SerializeField] private DayCycleManager dayCycleManager;

    [Tooltip("Require the daily shirt quota before ending the day.")]
    [SerializeField] private bool requireDailyQuota = true;

    public bool CanInteract => dayCycleManager != null &&
                               !dayCycleManager.TransitionRunning;

    private void Awake()
    {
        if (quotaManager == null)
        {
            quotaManager = DailyQuotaManager.Instance;
        }

        if (dayCycleManager == null)
        {
            dayCycleManager = FindFirstObjectByType<DayCycleManager>();
        }
    }

    public void Interact(GameObject player)
    {
        bool checkQuota = requireDailyQuota && !DeveloperGameOptions.SkipDailyQuotaEnabled;
        if (!CanInteract || (checkQuota && quotaManager == null))
        {
            return;
        }

        if (checkQuota && !quotaManager.HasMetQuota)
        {
            int missing = quotaManager.MissingShirts;
            string itemName = missing == 1 ? "shirt" : "shirts";
            ShowMessage(
                "You need " + missing + " more " + itemName +
                " before you can sleep.");
            return;
        }

        var tutorial = dayCycleManager.GetComponent<DayOneTutorial>();
        if (tutorial != null && !tutorial.TryAllowSleep()) return;
        dayCycleManager.TryBeginNextDay(checkQuota);
    }

    private void ShowMessage(string message)
    {
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
