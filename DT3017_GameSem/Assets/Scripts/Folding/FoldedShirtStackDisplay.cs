using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FoldedShirtStackDisplay : MonoBehaviour
{
    [Header("Shirt")]
    [SerializeField] private ItemData shirtItem;
    [SerializeField] private GameObject foldedShirtPrefab;

    [Header("Stack Layout")]
    [SerializeField] private Transform stackOrigin;
    [Tooltip("Local-space distance added for each shirt in the pile.")]
    [SerializeField] private Vector3 spacingPerShirt = new Vector3(0f, 0.02f, 0f);
    [SerializeField, Min(0f)] private float maximumRandomYRotation = 5f;
    [SerializeField] private int randomSeed = 12345;

    [Header("Runtime State (Debug)")]
    [SerializeField] private int displayedShirtCount;

    private readonly List<GameObject> shirtViews = new List<GameObject>();
    private InventoryManager subscribedInventory;

    private void OnEnable()
    {
        TrySubscribeAndRefresh();
    }

    private void Start()
    {
        // Start is a second chance in case InventoryManager awakened after this object.
        TrySubscribeAndRefresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribeAndRefresh()
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            return;
        }

        if (subscribedInventory != inventory)
        {
            Unsubscribe();
            subscribedInventory = inventory;
            subscribedInventory.QuantityChanged += OnQuantityChanged;
        }

        RefreshDisplay(inventory.GetQuantity(shirtItem));
    }

    private void Unsubscribe()
    {
        if (subscribedInventory != null)
        {
            subscribedInventory.QuantityChanged -= OnQuantityChanged;
            subscribedInventory = null;
        }
    }

    private void OnQuantityChanged(ItemData changedItem, int newQuantity)
    {
        if (changedItem == shirtItem)
        {
            RefreshDisplay(newQuantity);
        }
    }

    private void RefreshDisplay(int targetCount)
    {
        if (shirtItem == null || foldedShirtPrefab == null || stackOrigin == null)
        {
            return;
        }

        targetCount = Mathf.Clamp(targetCount, 0, shirtItem.StackLimit);

        while (shirtViews.Count < targetCount)
        {
            AddShirtView(shirtViews.Count);
        }

        while (shirtViews.Count > targetCount)
        {
            int lastIndex = shirtViews.Count - 1;
            GameObject view = shirtViews[lastIndex];
            shirtViews.RemoveAt(lastIndex);

            if (view != null)
            {
                Destroy(view);
            }
        }

        displayedShirtCount = shirtViews.Count;
    }

    private void AddShirtView(int index)
    {
        Vector3 position = stackOrigin.TransformPoint(spacingPerShirt * index);
        Quaternion rotation = stackOrigin.rotation *
                              Quaternion.Euler(0f, GetStableYRotation(index), 0f);

        GameObject view = Instantiate(foldedShirtPrefab, position, rotation);
        view.transform.SetParent(stackOrigin, true);
        shirtViews.Add(view);
    }

    private float GetStableYRotation(int index)
    {
        unchecked
        {
            System.Random random = new System.Random(randomSeed + index * 7919);
            double zeroToOne = random.NextDouble();
            return Mathf.Lerp(
                -maximumRandomYRotation,
                maximumRandomYRotation,
                (float)zeroToOne);
        }
    }

    private void OnValidate()
    {
        maximumRandomYRotation = Mathf.Max(0f, maximumRandomYRotation);
    }
}
