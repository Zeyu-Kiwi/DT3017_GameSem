using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(ScrollRect))]
public class DialogueOptionsScrollController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private Scrollbar verticalScrollbar;

    [Header("Height")]
    [SerializeField, Min(0f)] private float minimumHeight;
    [SerializeField, Min(1f)] private float maximumHeight = 300f;

    [Header("Behaviour")]
    [SerializeField] private bool resetToTopForNewOptions = true;
    [SerializeField] private bool hideScrollbarWhenContentFits = true;

    private RectTransform scrollRoot;
    private Coroutine refreshRoutine;
    private int previousChildrenSignature;
    private bool previousContentActive;
    private float previousPreferredHeight = -1f;
    private bool applyingLayout;

    private void Awake()
    {
        scrollRoot = transform as RectTransform;
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        ApplyReferencesToScrollRect();
    }

    private void OnEnable()
    {
        previousChildrenSignature = CalculateChildrenSignature();
        previousContentActive = content != null && content.gameObject.activeSelf;
        QueueRefresh(true);
    }

    private void LateUpdate()
    {
        if (content == null) return;

        bool contentActive = content.gameObject.activeSelf;
        int signature = CalculateChildrenSignature();
        float preferredHeight = contentActive
            ? LayoutUtility.GetPreferredHeight(content)
            : 0f;

        bool childrenChanged = signature != previousChildrenSignature;
        bool activeChanged = contentActive != previousContentActive;
        bool heightChanged = Mathf.Abs(preferredHeight - previousPreferredHeight) > 0.5f;

        if (childrenChanged || activeChanged || heightChanged)
        {
            previousChildrenSignature = signature;
            previousContentActive = contentActive;
            previousPreferredHeight = preferredHeight;
            QueueRefresh(resetToTopForNewOptions && (childrenChanged || activeChanged));
        }
    }

    private void OnDisable()
    {
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled && !applyingLayout) QueueRefresh(false);
    }

    private void ApplyReferencesToScrollRect()
    {
        if (scrollRect == null) return;
        if (viewport != null) scrollRect.viewport = viewport;
        if (content != null) scrollRect.content = content;
        if (verticalScrollbar != null)
            scrollRect.verticalScrollbar = verticalScrollbar;

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        if (hideScrollbarWhenContentFits)
        {
            scrollRect.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        }
    }

    private void QueueRefresh(bool resetPosition)
    {
        if (!isActiveAndEnabled) return;
        if (refreshRoutine != null) StopCoroutine(refreshRoutine);
        refreshRoutine = StartCoroutine(RefreshAfterLayout(resetPosition));
    }

    private IEnumerator RefreshAfterLayout(bool resetPosition)
    {
        // Dialogue buttons and their wrapped text finish layout on the next frame.
        yield return null;
        refreshRoutine = null;
        RefreshLayout(resetPosition);
    }

    [ContextMenu("Refresh Dialogue Options Layout")]
    public void RefreshLayout()
    {
        RefreshLayout(false);
    }

    private void RefreshLayout(bool resetPosition)
    {
        if (scrollRect == null || scrollRoot == null || content == null) return;
        ApplyReferencesToScrollRect();

        bool contentActive = content.gameObject.activeSelf;
        if (contentActive)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        float preferredHeight = contentActive
            ? Mathf.Max(0f, LayoutUtility.GetPreferredHeight(content))
            : 0f;
        float visibleHeight = Mathf.Clamp(
            preferredHeight,
            Mathf.Min(minimumHeight, maximumHeight),
            maximumHeight);
        applyingLayout = true;
        scrollRoot.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            visibleHeight);

        bool overflow = contentActive && preferredHeight > visibleHeight + 0.5f;
        scrollRect.vertical = overflow;

        if (verticalScrollbar != null)
        {
            if (!contentActive || (hideScrollbarWhenContentFits && !overflow))
                verticalScrollbar.gameObject.SetActive(false);
            else
                verticalScrollbar.gameObject.SetActive(true);
        }

        Canvas.ForceUpdateCanvases();
        if (viewport != null) LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);

        if (resetPosition)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        applyingLayout = false;

        previousPreferredHeight = preferredHeight;
    }

    private int CalculateChildrenSignature()
    {
        if (content == null) return 0;
        unchecked
        {
            int signature = content.childCount;
            for (int i = 0; i < content.childCount; i++)
                signature = signature * 31 + content.GetChild(i).GetInstanceID();
            return signature;
        }
    }

    private void OnValidate()
    {
        maximumHeight = Mathf.Max(1f, maximumHeight);
        minimumHeight = Mathf.Clamp(minimumHeight, 0f, maximumHeight);
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
    }
}
