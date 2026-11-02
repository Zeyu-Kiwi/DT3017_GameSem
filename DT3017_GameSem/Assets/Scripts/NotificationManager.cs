using System.Collections.Generic;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject container;
    [Tooltip("Parent with a Vertical Layout Group. Newest notifications are placed first.")]
    [SerializeField] private Transform entryContainer;
    [SerializeField] private NotificationEntryUI entryPrefab;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float displayDuration = 3f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;

    [Header("Capacity")]
    [SerializeField, Min(1)] private int maximumVisibleMessages = 5;

    private readonly List<NotificationEntryUI> activeEntries =
        new List<NotificationEntryUI>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one NotificationManager exists in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (container == gameObject)
        {
            Debug.LogError(
                "Notification container must be a separate UI object. Otherwise hiding it disables NotificationManager.",
                this);
            container = null;
        }

        if (entryContainer == null && container != null)
        {
            entryContainer = container.transform;
        }

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
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (container == null || entryContainer == null || entryPrefab == null)
        {
            Debug.Log(message, this);
            return;
        }

        container.SetActive(true);

        int allowedCount = Mathf.Max(1, maximumVisibleMessages);
        while (activeEntries.Count >= allowedCount)
        {
            NotificationEntryUI oldest = activeEntries[0];
            if (oldest != null)
            {
                oldest.RemoveImmediately();
            }
            else
            {
                activeEntries.RemoveAt(0);
            }
        }

        NotificationEntryUI entry = Instantiate(entryPrefab, entryContainer);
        entry.gameObject.SetActive(true);
        entry.transform.SetAsFirstSibling();
        activeEntries.Add(entry);
        entry.Initialize(this, message, displayDuration, fadeDuration);
    }

    public void RemoveEntry(NotificationEntryUI entry)
    {
        if (entry == null)
        {
            return;
        }

        activeEntries.Remove(entry);
        Destroy(entry.gameObject);

        if (activeEntries.Count == 0 && container != null)
        {
            container.SetActive(false);
        }
    }

    private void OnValidate()
    {
        displayDuration = Mathf.Max(0.1f, displayDuration);
        fadeDuration = Mathf.Max(0f, fadeDuration);
        maximumVisibleMessages = Mathf.Max(1, maximumVisibleMessages);
    }
}
