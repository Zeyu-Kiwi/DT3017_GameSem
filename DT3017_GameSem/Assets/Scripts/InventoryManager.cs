using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Serializable]
    private class StartingItem
    {
        public ItemData item;
        [Min(0)] public int quantity;
    }

    public static InventoryManager Instance { get; private set; }

    [Tooltip("Add every item that can exist in this game, even when its starting quantity is zero.")]
    [SerializeField] private List<StartingItem> items = new List<StartingItem>();

    public event Action<ItemData, int> QuantityChanged;

    private readonly Dictionary<ItemData, int> quantities = new Dictionary<ItemData, int>();
    private readonly Dictionary<string, ItemData> itemsById = new Dictionary<string, ItemData>();

    // Exposes a read-only view for debugging tools without allowing them to change the inventory.
    public IReadOnlyDictionary<ItemData, int> RuntimeQuantities => quantities;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one InventoryManager exists in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        InitializeInventory();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeInventory()
    {
        quantities.Clear();
        itemsById.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            StartingItem entry = items[i];

            if (entry.item == null)
            {
                Debug.LogWarning("Inventory contains an empty item entry.", this);
                continue;
            }

            string id = entry.item.ItemId;
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("An inventory item has an empty ID.", entry.item);
                continue;
            }

            if (itemsById.ContainsKey(id))
            {
                Debug.LogError("Duplicate inventory item ID: " + id, entry.item);
                continue;
            }

            itemsById.Add(id, entry.item);
            quantities.Add(entry.item, Mathf.Clamp(entry.quantity, 0, entry.item.StackLimit));
        }
    }

    public int GetQuantity(ItemData item)
    {
        if (item != null && quantities.TryGetValue(item, out int quantity))
        {
            return quantity;
        }

        return 0;
    }

    public bool Has(ItemData item, int amount)
    {
        return amount >= 0 && GetQuantity(item) >= amount;
    }

    public int GetRemainingCapacity(ItemData item)
    {
        if (!IsRegistered(item))
        {
            return 0;
        }

        return item.StackLimit - quantities[item];
    }

    public int AddItem(ItemData item, int amount)
    {
        if (!IsRegistered(item) || amount <= 0)
        {
            return 0;
        }

        int currentQuantity = quantities[item];
        int amountThatFits = Mathf.Min(amount, item.StackLimit - currentQuantity);

        if (amountThatFits <= 0)
        {
            return 0;
        }

        int newQuantity = currentQuantity + amountThatFits;
        quantities[item] = newQuantity;
        QuantityChanged?.Invoke(item, newQuantity);
        return amountThatFits;
    }

    public bool TryRemoveItem(ItemData item, int amount)
    {
        if (!IsRegistered(item) || amount < 0 || !Has(item, amount))
        {
            return false;
        }

        int newQuantity = quantities[item] - amount;
        quantities[item] = newQuantity;
        QuantityChanged?.Invoke(item, newQuantity);
        return true;
    }

    public bool TryGetItemById(string itemId, out ItemData item)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            item = null;
            return false;
        }

        return itemsById.TryGetValue(itemId, out item);
    }

    private bool IsRegistered(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("Tried to use an empty item reference.", this);
            return false;
        }

        if (!quantities.ContainsKey(item))
        {
            Debug.LogWarning(item.DisplayName + " is not registered in InventoryManager.", item);
            return false;
        }

        return true;
    }
}
