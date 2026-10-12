using TMPro;
using UnityEngine;

/// <summary>Tutorial thought text and a screen-space marker anchored above the bed.</summary>
public class TutorialHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text thoughtText;
    [SerializeField] private TMP_Text escapeRouteProgressText;
    [SerializeField] private RectTransform bedIndicator;
    [SerializeField] private Transform bed;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Vector3 indicatorOffset = new Vector3(0f, 1.7f, 0f);
    [SerializeField, Min(0f)] private float messageDuration = 5f;
    private float hideMessageAt;
    private bool showBed;
    private RectTransform indicatorParent;

    private void Awake()
    {
        if (bedIndicator != null) indicatorParent = bedIndicator.parent as RectTransform;
        Hide();
    }

    public void ShowBedObjective()
    {
        if (thoughtText != null)
        {
            thoughtText.text = "I should call it a day.";
            thoughtText.gameObject.SetActive(true);
            hideMessageAt = Time.unscaledTime + messageDuration;
        }
        showBed = true;
    }

    public void ShowMessage(string message)
    {
        if (thoughtText == null) return;
        thoughtText.text = message;
        thoughtText.gameObject.SetActive(true);
        hideMessageAt = Time.unscaledTime + messageDuration;
    }

    public void ShowEscapeRouteProgress(int checkedCount, int totalCount)
    {
        if (escapeRouteProgressText == null) return;
        escapeRouteProgressText.text = checkedCount + "/" + totalCount + " escape routes checked";
        escapeRouteProgressText.gameObject.SetActive(checkedCount > 0);
    }

    public void Hide()
    {
        showBed = false;
        if (escapeRouteProgressText != null) escapeRouteProgressText.gameObject.SetActive(false);
        if (thoughtText != null) thoughtText.gameObject.SetActive(false);
        if (bedIndicator != null) bedIndicator.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (thoughtText != null && thoughtText.gameObject.activeSelf && Time.unscaledTime >= hideMessageAt)
            thoughtText.gameObject.SetActive(false);
        if (bedIndicator == null || bed == null || playerCamera == null || indicatorParent == null) return;
        Vector3 screen = playerCamera.WorldToScreenPoint(bed.position + indicatorOffset);
        bool visible = showBed && screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height;
        bedIndicator.gameObject.SetActive(visible);
        if (visible && RectTransformUtility.ScreenPointToLocalPointInRectangle(indicatorParent, screen, null, out Vector2 position))
            bedIndicator.anchoredPosition = position;
    }
}