using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup), typeof(LayoutElement))]
public sealed class NotificationEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;

    [Header("Automatic Height")]
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField, Min(1f)] private float minimumHeight = 50f;
    [SerializeField, Min(0f)] private float verticalPadding = 16f;

    private CanvasGroup canvasGroup;
    private NotificationManager owner;
    private Coroutine lifetimeRoutine;

    public void Initialize(
        NotificationManager owningManager,
        string message,
        float displayDuration,
        float fadeDuration)
    {
        owner = owningManager;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (messageText == null)
        {
            messageText = GetComponentInChildren<TMP_Text>(true);
        }

        if (messageText != null)
        {
            messageText.text = message;
            UpdatePreferredHeight(message);
        }
        else
        {
            Debug.LogWarning("NotificationEntryUI has no TextMeshPro text assigned.", this);
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
        }

        lifetimeRoutine = StartCoroutine(LifetimeRoutine(
            Mathf.Max(0.1f, displayDuration),
            Mathf.Max(0f, fadeDuration)));
    }

    private void UpdatePreferredHeight(string message)
    {
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }

        if (layoutElement == null || messageText == null)
        {
            return;
        }

        // Ensure the parent layout has assigned the entry width before asking TMP
        // how tall the wrapped text needs to be.
        Canvas.ForceUpdateCanvases();
        messageText.ForceMeshUpdate();

        float availableWidth = messageText.rectTransform.rect.width;
        if (availableWidth <= 0.01f && transform is RectTransform entryRect)
        {
            availableWidth = entryRect.rect.width;
        }

        availableWidth = Mathf.Max(1f, availableWidth);
        float textHeight = messageText.GetPreferredValues(
            message,
            availableWidth,
            Mathf.Infinity).y;

        layoutElement.preferredHeight = Mathf.Max(
            minimumHeight,
            textHeight + verticalPadding);

        if (transform.parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }

    public void RemoveImmediately()
    {
        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        if (owner != null)
        {
            owner.RemoveEntry(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator LifetimeRoutine(float displayDuration, float fadeDuration)
    {
        // Scaled time intentionally freezes notifications while the game is paused.
        yield return new WaitForSeconds(displayDuration);

        if (fadeDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
        }

        canvasGroup.alpha = 0f;
        lifetimeRoutine = null;

        if (owner != null)
        {
            owner.RemoveEntry(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        minimumHeight = Mathf.Max(1f, minimumHeight);
        verticalPadding = Mathf.Max(0f, verticalPadding);

        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }

        if (messageText == null)
        {
            messageText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
