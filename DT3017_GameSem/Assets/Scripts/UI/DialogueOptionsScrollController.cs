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
    [SerializeField] private CanvasGroup verticalScrollbarCanvasGroup;

    [Header("Height")]
    [SerializeField, Min(0f)] private float minimumHeight;
    [SerializeField, Min(1f)] private float maximumHeight = 300f;

    [Header("Behaviour")]
    [SerializeField] private bool resetToTopForNewOptions = true;
    [SerializeField] private bool hideScrollbarWhenContentFits = true;

    [Header("Mouse Wheel")]
    [Tooltip("Read the mouse wheel anywhere on screen while dialogue options overflow.")]
    [SerializeField] private bool scrollWheelWorksAnywhere = true;
    [SerializeField, Min(0.01f)] private float normalizedScrollStep = 0.12f;

    private RectTransform scrollRoot;
    private Coroutine refreshRoutine;
    private int previousChildrenSignature;
    private bool previousContentActive;
    private float previousPreferredHeight = -1f;
    private bool applyingLayout;
    private bool contentOverflows;
    private float originalScrollSensitivity;

    private void Awake()
    {
        scrollRoot = transform as RectTransform;
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null)
            originalScrollSensitivity = scrollRect.scrollSensitivity;
        EnsureScrollbarCanvasGroup();
        SetScrollbarVisible(false);
        ApplyReferencesToScrollRect();
    }

    private void OnEnable()
    {
        previousChildrenSignature = CalculateChildrenSignature();
        previousContentActive = content != null && content.gameObject.activeSelf;
        QueueRefresh(true);
    }

    private void Update()
    {
        if (!scrollWheelWorksAnywhere || !contentOverflows ||
            scrollRect == null || content == null ||
            !content.gameObject.activeInHierarchy)
            return;

        float wheelDelta;
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.Mouse mouse =
            UnityEngine.InputSystem.Mouse.current;
        wheelDelta = mouse != null ? mouse.scroll.ReadValue().y : 0f;
        // Windows commonly reports 120 per wheel notch, while some devices and
        // platforms already report small normalized values.
        if (Mathf.Abs(wheelDelta) > 10f) wheelDelta /= 120f;
#else
        wheelDelta = Input.mouseScrollDelta.y;
#endif

        if (Mathf.Abs(wheelDelta) <= Mathf.Epsilon) return;
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition +
            wheelDelta * normalizedScrollStep);
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

        if (scrollRect != null)
            scrollRect.scrollSensitivity = originalScrollSensitivity;
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
        scrollRect.scrollSensitivity = scrollWheelWorksAnywhere
            ? 0f
            : originalScrollSensitivity;
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
        contentOverflows = overflow;
        scrollRect.vertical = overflow;

        bool showScrollbar = contentActive &&
                             (!hideScrollbarWhenContentFits || overflow);
        SetScrollbarVisible(showScrollbar);

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

    private void EnsureScrollbarCanvasGroup()
    {
        if (verticalScrollbarCanvasGroup != null || verticalScrollbar == null)
            return;

        verticalScrollbarCanvasGroup =
            verticalScrollbar.GetComponent<CanvasGroup>();
        if (verticalScrollbarCanvasGroup == null && Application.isPlaying)
        {
            verticalScrollbarCanvasGroup =
                verticalScrollbar.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void SetScrollbarVisible(bool visible)
    {
        EnsureScrollbarCanvasGroup();
        if (verticalScrollbarCanvasGroup == null) return;

        verticalScrollbarCanvasGroup.alpha = visible ? 1f : 0f;
        verticalScrollbarCanvasGroup.interactable = visible;
        verticalScrollbarCanvasGroup.blocksRaycasts = visible;
    }

    private void OnValidate()
    {
        maximumHeight = Mathf.Max(1f, maximumHeight);
        minimumHeight = Mathf.Clamp(minimumHeight, 0f, maximumHeight);
        normalizedScrollStep = Mathf.Max(0.01f, normalizedScrollStep);
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (verticalScrollbarCanvasGroup == null && verticalScrollbar != null)
        {
            verticalScrollbarCanvasGroup =
                verticalScrollbar.GetComponent<CanvasGroup>();
        }
    }
}
