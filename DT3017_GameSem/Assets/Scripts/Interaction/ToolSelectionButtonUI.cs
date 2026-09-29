using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToolSelectionButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image toolIconImage;

    private ToolSelectionUI owner;
    private ItemData item;

    public ItemData Item => item;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

    }

    public void Initialize(
        ToolSelectionUI owningUI,
        ItemData toolItem,
        Sprite missingIconSprite)
    {
        owner = owningUI;
        item = toolItem;

        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        if (toolIconImage != null)
        {
            Sprite displayedIcon = item != null && item.Icon != null
                ? item.Icon
                : missingIconSprite;
            toolIconImage.sprite = displayedIcon;
            toolIconImage.enabled = displayedIcon != null;
            toolIconImage.preserveAspect = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null && item != null)
        {
            owner.ShowHoveredTool(item);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null && item != null)
        {
            owner.ClearHoveredTool(item);
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
