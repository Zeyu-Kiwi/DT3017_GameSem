using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingEquipmentDisplay : MonoBehaviour
{
    [Serializable]
    private class EquipmentEntry
    {
        public ItemData item;
        public GameObject displayModel;
    }

    [SerializeField] private List<EquipmentEntry> equipment = new List<EquipmentEntry>();

    public void Initialize(CraftingStation station)
    {
        for (int i = 0; i < equipment.Count; i++)
        {
            EquipmentEntry entry = equipment[i];
            if (entry.item == null || entry.displayModel == null)
            {
                continue;
            }

            CraftingItemView view = entry.displayModel.GetComponent<CraftingItemView>();

            if (view == null)
            {
                view = entry.displayModel.AddComponent<CraftingItemView>();
            }

            view.Configure(station, entry.item, false, false);
        }

        Refresh();
    }

    public void Refresh()
    {
        InventoryManager inventory = InventoryManager.Instance;

        for (int i = 0; i < equipment.Count; i++)
        {
            EquipmentEntry entry = equipment[i];
            if (entry.displayModel != null)
            {
                bool owned = inventory != null &&
                             entry.item != null &&
                             inventory.GetQuantity(entry.item) > 0;

                entry.displayModel.SetActive(owned);
            }
        }
    }
}
