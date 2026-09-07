using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InventoryManager))]
public class InventoryManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Inventory", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode to see the current inventory quantities.",
                MessageType.Info);
            return;
        }

        InventoryManager inventory = (InventoryManager)target;
        List<ItemData> sortedItems = new List<ItemData>(inventory.RuntimeQuantities.Keys);

        sortedItems.Sort((first, second) =>
            string.Compare(first.DisplayName, second.DisplayName, System.StringComparison.Ordinal));

        if (sortedItems.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No items are registered in this InventoryManager.",
                MessageType.Warning);
            return;
        }

        EditorGUI.BeginDisabledGroup(true);

        for (int i = 0; i < sortedItems.Count; i++)
        {
            ItemData item = sortedItems[i];
            int quantity = inventory.RuntimeQuantities[item];

            EditorGUILayout.TextField(
                item.DisplayName,
                quantity + " / " + item.StackLimit);
        }

        EditorGUI.EndDisabledGroup();

        Repaint();
    }
}
