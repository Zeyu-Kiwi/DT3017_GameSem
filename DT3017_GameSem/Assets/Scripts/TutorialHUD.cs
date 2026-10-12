using UnityEngine;

/// <summary>Optional screen-space marker that stays above the bed while enabled.</summary>
public class TutorialHUD : MonoBehaviour
{
    [SerializeField] private RectTransform bedIndicator;
    [SerializeField] private Transform bed;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Vector3 indicatorOffset = new Vector3(0f, 1.7f, 0f);

    private RectTransform indicatorParent;
    private bool indicatorEnabled;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (bedIndicator != null) indicatorParent = bedIndicator.parent as RectTransform;
        SetBedIndicatorVisible(false);
    }

    public void SetBedIndicatorVisible(bool visible)
    {
        indicatorEnabled = visible;
        if (!visible && bedIndicator != null)
            bedIndicator.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (bedIndicator == null || bed == null || playerCamera == null ||
            indicatorParent == null) return;

        Vector3 screen = playerCamera.WorldToScreenPoint(
            bed.position + indicatorOffset);
        bool visible = indicatorEnabled && screen.z > 0f &&
                       screen.x >= 0f && screen.x <= Screen.width &&
                       screen.y >= 0f && screen.y <= Screen.height;
        bedIndicator.gameObject.SetActive(visible);
        if (visible && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                indicatorParent, screen, null, out Vector2 position))
            bedIndicator.anchoredPosition = position;
    }
}
