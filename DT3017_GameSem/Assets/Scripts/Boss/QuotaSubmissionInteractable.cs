using UnityEngine;

public class QuotaSubmissionInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DailyQuotaManager quotaManager;

    public bool CanInteract => quotaManager != null && quotaManager.CanSubmit;

    private void Awake()
    {
        if (quotaManager == null)
        {
            quotaManager = DailyQuotaManager.Instance;
        }
    }

    public void Interact(GameObject player)
    {
        if (!CanInteract)
        {
            return;
        }

        int submitted = quotaManager.SubmitAvailableShirts();
        if (submitted <= 0)
        {
            return;
        }

        string message = "Submitted " + submitted + " " +
                         quotaManager.ShirtItem.DisplayName + ".";

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
