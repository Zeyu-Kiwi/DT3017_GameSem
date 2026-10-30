using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DailySpawnTable", menuName = "Game/Spawning/Daily Item Spawn Table")]
public class DailyItemSpawnTable : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Tooltip("Existing model or pickup prefab. Its authored scale and rotation are preserved.")]
        [SerializeField] private GameObject prefab;
        [Tooltip("Inventory item represented by this prefab. Can be omitted when its root already has a configured ItemPickup.")]
        [SerializeField] private ItemData item;
        [SerializeField, HideInInspector] private ItemPickup pickupPrefab; // Legacy table compatibility.
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(1)] private int minimumQuantity = 1;
        [SerializeField, Min(1)] private int maximumQuantity = 1;
        public GameObject Prefab => prefab != null ? prefab : pickupPrefab != null ? pickupPrefab.gameObject : null;
        public ItemData Item => item != null ? item : Prefab != null && Prefab.TryGetComponent<ItemPickup>(out var pickup) ? pickup.Item : null;
        public float Weight => weight;
        public int MinimumQuantity => Mathf.Max(1, minimumQuantity);
        public int MaximumQuantity => Mathf.Max(MinimumQuantity, maximumQuantity);
        public bool IsValid => Prefab != null && Item != null;
    }

    [Tooltip("Relative weights: an entry with weight 4 is twice as likely as one with weight 2. Zero disables an entry. Rolls allow repeats.")]
    [SerializeField] private List<Entry> entries = new List<Entry>();
    public IReadOnlyList<Entry> Entries => entries;
}