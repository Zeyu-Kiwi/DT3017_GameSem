using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingEquipmentDisplay : MonoBehaviour
{
    [Serializable]
    private class EquipmentEntry
    {
        public ItemData item = null;
        public GameObject displayModel = null;
    }

    [SerializeField] private List<EquipmentEntry> equipment = new List<EquipmentEntry>();

    [Header("Scene Gizmo")]
    [SerializeField] private bool showEquipmentGizmo = true;
    [SerializeField] private bool showGizmoOnlyWhenSelected;
    [SerializeField] private Vector3 gizmoOffset = Vector3.zero;
    [SerializeField] private Vector3 gizmoSize = new Vector3(1f, 0.25f, 1f);
    [SerializeField] private Color gizmoColor = new Color(1f, 0.75f, 0.2f, 0.9f);

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

    private void OnValidate()
    {
        gizmoSize.x = Mathf.Max(0.01f, Mathf.Abs(gizmoSize.x));
        gizmoSize.y = Mathf.Max(0.01f, Mathf.Abs(gizmoSize.y));
        gizmoSize.z = Mathf.Max(0.01f, Mathf.Abs(gizmoSize.z));
    }

    private void OnDrawGizmos()
    {
        if (showEquipmentGizmo && !showGizmoOnlyWhenSelected)
        {
            DrawEquipmentGizmo();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (showEquipmentGizmo && showGizmoOnlyWhenSelected)
        {
            DrawEquipmentGizmo();
        }
    }

    private void DrawEquipmentGizmo()
    {
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.color = gizmoColor;
        // Ignore inherited scale so Gizmo Size is measured in world units.
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(gizmoOffset, gizmoSize);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
