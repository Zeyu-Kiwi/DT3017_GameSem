using System;
using System.Collections.Generic;
using UnityEngine;

// Item definitions and optional visuals used by the development item menu.
public sealed class DevItemCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public ItemData item;
        public GameObject worldModel;
    }
    [SerializeField] private List<Entry> entries = new List<Entry>();
    public IReadOnlyList<Entry> Entries => entries;
}