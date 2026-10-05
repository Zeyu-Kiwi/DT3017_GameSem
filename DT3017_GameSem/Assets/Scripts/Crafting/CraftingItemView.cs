using UnityEngine;

[DisallowMultipleComponent]
public class CraftingItemView : MonoBehaviour
{
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
