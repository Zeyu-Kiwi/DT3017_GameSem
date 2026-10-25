using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(LayoutElement))]
public class DialogueOptionAutoHeight : MonoBehaviour
{
    [SerializeField] private TMP_Text optionText;
    [SerializeField, Min(1f)] private float minimumHeight = 50f;
    [SerializeField, Min(0f)] private float verticalPadding = 16f;

    private LayoutElement layoutElement;
    private string previousText;
    private float previousTextWidth = -1f;

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        if (optionText == null) optionText = GetComponentInChildren<TMP_Text>(true);
        RefreshHeight();
    }

    private void OnEnable()
    {
        RefreshHeight();
    }

    private void LateUpdate()
    {
        if (optionText == null) return;
        float width = optionText.rectTransform.rect.width;
        if (previousText != optionText.text ||
            Mathf.Abs(previousTextWidth - width) > 0.5f)
        {
            RefreshHeight();
        }
    }

    [ContextMenu("Refresh Option Height")]
    public void RefreshHeight()
    {
        if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
        if (optionText == null) optionText = GetComponentInChildren<TMP_Text>(true);
        if (layoutElement == null || optionText == null) return;

        float availableWidth = Mathf.Max(1f, optionText.rectTransform.rect.width);
        float textHeight = optionText.GetPreferredValues(
            optionText.text,
            availableWidth,
            0f).y;
        float requiredHeight = Mathf.Max(minimumHeight, textHeight + verticalPadding);

        layoutElement.minHeight = minimumHeight;
        if (Mathf.Abs(layoutElement.preferredHeight - requiredHeight) > 0.5f)
        {
            layoutElement.preferredHeight = requiredHeight;
            RectTransform parent = transform.parent as RectTransform;
            if (parent != null) LayoutRebuilder.MarkLayoutForRebuild(parent);
        }

        previousText = optionText.text;
        previousTextWidth = optionText.rectTransform.rect.width;
    }

    private void OnValidate()
    {
        minimumHeight = Mathf.Max(1f, minimumHeight);
        verticalPadding = Mathf.Max(0f, verticalPadding);
        if (optionText == null) optionText = GetComponentInChildren<TMP_Text>(true);
    }
}
