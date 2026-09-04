using System.Collections;
using TMPro;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [SerializeField] private GameObject container;
    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(0.1f)] private float displayTime = 2f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one NotificationManager exists in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (container != null)
        {
            container.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowMessage(string message)
    {
        if (container == null || messageText == null)
        {
            Debug.Log(message, this);
            return;
        }

        messageText.text = message;
        container.SetActive(true);

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        container.SetActive(false);
        hideRoutine = null;
    }
}
