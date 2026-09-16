using UnityEngine;

[DisallowMultipleComponent]
public class CraftingItemView : MonoBehaviour
{
    [Tooltip("Optional child object using a white outline material. It is enabled only while hovered.")]
    [SerializeField] private GameObject hoverOutline;

    private CraftingStation station;
    private ItemData item;
    private bool isCenterItem;
    private bool canSelectIngredient;

    public ItemData Item => item;
    public bool IsCenterItem => isCenterItem;
    public bool CanSelectIngredient => canSelectIngredient;

    public void Configure(
        CraftingStation owningStation,
        ItemData itemData,
        bool centerItem,
        bool selectableIngredient)
    {
        station = owningStation;
        item = itemData;
        isCenterItem = centerItem;
        canSelectIngredient = selectableIngredient;
        SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
        if (hoverOutline != null)
        {
            hoverOutline.SetActive(hovered);
        }
    }

    public void HandleLeftClick()
    {
        if (station != null && !isCenterItem && canSelectIngredient)
        {
            station.TryAddIngredient(item);
        }
    }

    public void HandleRightClick()
    {
        if (station != null && isCenterItem)
        {
            station.TryRemoveIngredient(item);
        }
    }
}
