using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Shows authored table decorations while their matching items are owned.</summary>
[DisallowMultipleComponent]
public sealed class CraftingDecorationDisplay : MonoBehaviour
{
    [Serializable]
    private sealed class DecorationEntry
    {
        public ItemData item;
        public GameObject displayModel;
    }

    [SerializeField] private List<DecorationEntry> decorations = new List<DecorationEntry>();

    private InventoryManager inventory;
    private bool subscribed;

    private void OnEnable()
    {
        SubscribeToInventory();
        Refresh();
    }

    private void Start()
    {
        // InventoryManager may not have completed Awake when this component enables.
        SubscribeToInventory();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromInventory();
    }

    public void Refresh()
    {
        SubscribeToInventory();

        for (int i = 0; i < decorations.Count; i++)
        {
            DecorationEntry entry = decorations[i];
            if (entry == null || entry.displayModel == null) continue;

            // Keep this controller active so it can react when ownership changes.
            if (entry.displayModel == gameObject ||
                transform.IsChildOf(entry.displayModel.transform))
            {
                Debug.LogError("A decoration display model cannot contain CraftingDecorationDisplay.", this);
                continue;
            }

            bool owned = inventory != null && entry.item != null &&
                         inventory.GetQuantity(entry.item) > 0;
            if (entry.displayModel.activeSelf != owned)
                entry.displayModel.SetActive(owned);
        }
    }

    private void SubscribeToInventory()
    {
        InventoryManager current = InventoryManager.Instance;
        if (subscribed && inventory == current) return;

        UnsubscribeFromInventory();
        inventory = current;
        if (inventory == null) return;

        inventory.QuantityChanged += OnQuantityChanged;
        subscribed = true;
    }

    private void UnsubscribeFromInventory()
    {
        if (subscribed && inventory != null)
            inventory.QuantityChanged -= OnQuantityChanged;

        subscribed = false;
        inventory = null;
    }

    private void OnQuantityChanged(ItemData item, int quantity)
    {
        Refresh();
    }
}
