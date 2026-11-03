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

    private CanvasGroup canvasGroup;
    private NotificationManager owner;
    private Coroutine lifetimeRoutine;
    private Coroutine heightRefreshRoutine;

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

            if (heightRefreshRoutine != null)
            {
                StopCoroutine(heightRefreshRoutine);
            }

            heightRefreshRoutine = StartCoroutine(
                RefreshPreferredHeightNextFrame(message));
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

        // Ensure the parent layout has assigned the entry width before TMP
        // generates the final wrapped text mesh.
        Canvas.ForceUpdateCanvases();
        messageText.ForceMeshUpdate(true, true);

        RectTransform textRect = messageText.rectTransform;
        float renderedTextHeight = messageText.GetRenderedValues(false).y;

        // MessageText uses vertical stretch anchors. Its Top and Bottom offsets
        // are the real visual padding, so they are the single source of truth.
        float topPadding = Mathf.Max(0f, -textRect.offsetMax.y);
        float bottomPadding = Mathf.Max(0f, textRect.offsetMin.y);
        float requiredHeight = renderedTextHeight + topPadding + bottomPadding;

        layoutElement.preferredHeight = Mathf.Ceil(Mathf.Max(
            minimumHeight,
            requiredHeight));

        if (transform.parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }

    private IEnumerator RefreshPreferredHeightNextFrame(string message)
    {
        // A newly-instantiated entry may not receive its final width from the
        // parent VerticalLayoutGroup until the following Canvas layout pass.
        yield return null;
        UpdatePreferredHeight(message);
        heightRefreshRoutine = null;
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
