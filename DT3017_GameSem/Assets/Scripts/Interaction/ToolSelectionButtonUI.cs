using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolSelectionButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image toolIconImage;
    [SerializeField] private TMP_Text toolNameText;

    [Header("Selection Colours")]
    [SerializeField] private Color normalBackgroundColor = Color.gray;
    [SerializeField] private Color selectedBackgroundColor = Color.white;

    private ToolSelectionUI owner;
    private ItemData item;

    public ItemData Item => item;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (backgroundImage == null && button != null)
        {
            backgroundImage = button.targetGraphic as Image;
        }
    }

    public void Initialize(ToolSelectionUI owningUI, ItemData toolItem)
    {
        owner = owningUI;
        item = toolItem;

        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        if (toolNameText != null)
        {
            toolNameText.text = item == null ? string.Empty : item.DisplayName;
        }

        if (toolIconImage != null)
        {
            bool hasIcon = item != null && item.Icon != null;
            toolIconImage.enabled = hasIcon;
            toolIconImage.sprite = hasIcon ? item.Icon : null;
            toolIconImage.preserveAspect = true;
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = selected
                ? selectedBackgroundColor
                : normalBackgroundColor;
        }
    }

    private void OnClicked()
    {
        if (owner != null && item != null)
        {
            owner.SelectTool(this);
        }
    }
}
