using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Tooltip("Stable name used by code and Dialogue Editor parameters. Avoid renaming it after use.")]
    [SerializeField] private string itemId = "new_item";

    [SerializeField] private string displayName = "New Item";
    [SerializeField, Min(1)] private int stackLimit = 1;
    [SerializeField] private Sprite icon;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public int StackLimit => stackLimit;
    public Sprite Icon => icon;

    private void OnValidate()
    {
        itemId = itemId == null ? string.Empty : itemId.Trim();
        stackLimit = Mathf.Max(1, stackLimit);
    }
}
