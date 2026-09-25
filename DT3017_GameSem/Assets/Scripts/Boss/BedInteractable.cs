using UnityEngine;

public class BedInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DailyQuotaManager quotaManager;
    [SerializeField] private DayCycleManager dayCycleManager;

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
        if (!CanInteract || quotaManager == null)
        {
            return;
        }

        if (!quotaManager.HasMetQuota)
        {
            int missing = quotaManager.MissingShirts;
            string itemName = missing == 1 ? "shirt" : "shirts";
            ShowMessage(
                "You need " + missing + " more " + itemName +
                " before you can sleep.");
            return;
        }

        dayCycleManager.TryBeginNextDay();
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
