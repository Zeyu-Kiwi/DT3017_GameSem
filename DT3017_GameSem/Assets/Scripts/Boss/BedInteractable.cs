using UnityEngine;

public class BedInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DailyQuotaManager quotaManager;
    [SerializeField] private DayCycleManager dayCycleManager;

    [Tooltip("Require the daily shirt quota before ending the day.")]
    [SerializeField] private bool requireDailyQuota = true;
    [Tooltip("Allow using the bed without sitting in the chair.")]
    [SerializeField] private bool allowWhileStanding;

    [Header("Audio")]
    [SerializeField] private AudioClip bedInteractClip;
    [SerializeField, Range(0f, 1f)] private float bedInteractVolume = 1f;

    public bool CanInteract => PlayerSeatController.AllowsInteraction(allowWhileStanding) &&
                               dayCycleManager != null &&
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

        if (dayCycleManager.TryBeginNextDay(checkQuota))
            WorldAudioOneShot.Play(bedInteractClip, transform.position, bedInteractVolume);
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
