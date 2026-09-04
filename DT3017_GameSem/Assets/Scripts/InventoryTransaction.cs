using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryTransaction : MonoBehaviour
{
    [Serializable]
    private class ItemAmount
    {
        public ItemData item;
        [Min(1)] public int amount = 1;
    }

    [Tooltip("Every listed amount must be available or the transaction will not happen.")]
    [SerializeField] private List<ItemAmount> costs = new List<ItemAmount>();

    [Tooltip("Rewards are added up to each item's stack limit.")]
    [SerializeField] private List<ItemAmount> rewards = new List<ItemAmount>();

    [Tooltip("Enable this for trades so costs are not removed unless every reward fits.")]
    [SerializeField] private bool requireSpaceForAllRewards;

    public void Execute()
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            Debug.LogError("No InventoryManager exists in the scene.", this);
            return;
        }

        Dictionary<ItemData, int> combinedCosts = Combine(costs);
        Dictionary<ItemData, int> combinedRewards = Combine(rewards);

        foreach (KeyValuePair<ItemData, int> cost in combinedCosts)
        {
            if (!inventory.Has(cost.Key, cost.Value))
            {
                ShowMessage("Not enough " + cost.Key.DisplayName + ".");
                return;
            }
        }

        if (requireSpaceForAllRewards)
        {
            foreach (KeyValuePair<ItemData, int> reward in combinedRewards)
            {
                if (inventory.GetRemainingCapacity(reward.Key) < reward.Value)
                {
                    ShowMessage("Not enough space for " + reward.Key.DisplayName + ".");
                    return;
                }
            }
        }

        foreach (KeyValuePair<ItemData, int> cost in combinedCosts)
        {
            inventory.TryRemoveItem(cost.Key, cost.Value);
        }

        foreach (KeyValuePair<ItemData, int> reward in combinedRewards)
        {
            int added = inventory.AddItem(reward.Key, reward.Value);

            if (added < reward.Value)
            {
                if (added == 0)
                {
                    ShowMessage(reward.Key.DisplayName + " is full.");
                }
                else
                {
                    ShowMessage(
                        "Received " + added + " " + reward.Key.DisplayName +
                        ". " + reward.Key.DisplayName + " is now full.");
                }
            }
        }
    }

    private Dictionary<ItemData, int> Combine(List<ItemAmount> entries)
    {
        Dictionary<ItemData, int> combined = new Dictionary<ItemData, int>();

        for (int i = 0; i < entries.Count; i++)
        {
            ItemAmount entry = entries[i];
            if (entry.item == null || entry.amount <= 0)
            {
                continue;
            }

            if (!combined.ContainsKey(entry.item))
            {
                combined.Add(entry.item, 0);
            }

            combined[entry.item] += entry.amount;
        }

        return combined;
    }

    private void ShowMessage(string message)
    {
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowMessage(message);
        }
        else
        {
            Debug.Log(message, this);
        }
    }
}
