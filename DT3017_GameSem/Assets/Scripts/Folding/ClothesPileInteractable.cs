using UnityEngine;

[DisallowMultipleComponent]
public class ClothesPileInteractable : MonoBehaviour, IInteractable
{
    [Header("Shirt")]
    [SerializeField] private ItemData shirtItem;
    [SerializeField] private ShirtFoldingController shirtPrefab;

    [Header("Folding Workstation")]
    [SerializeField] private Transform shirtSpawnPoint;
    [Tooltip("Optional parent for the spawned shirt. The shirt keeps its world scale when parented.")]
    [SerializeField] private Transform spawnedShirtParent;

    [Header("Runtime State (Debug)")]
    [SerializeField] private ShirtFoldingController activeShirt;

    public bool CanInteract =>
        shirtItem != null && shirtPrefab != null && shirtSpawnPoint != null;

    public ShirtFoldingController ActiveShirt => activeShirt;

    public void Interact(GameObject player)
    {
        if (!CanInteract)
        {
            Debug.LogWarning("The clothes pile has not been fully configured.", this);
            return;
        }

        if (activeShirt != null)
        {
            ShowMessage("Finish folding the current shirt first.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("The clothes pile needs an InventoryManager in the scene.", this);
            return;
        }

        if (InventoryManager.Instance.GetRemainingCapacity(shirtItem) <= 0)
        {
            ShowMessage(shirtItem.DisplayName + " is full.");
            return;
        }

        activeShirt = Instantiate(
            shirtPrefab,
            shirtSpawnPoint.position,
            shirtSpawnPoint.rotation);

        if (spawnedShirtParent != null)
        {
            activeShirt.transform.SetParent(spawnedShirtParent, true);
        }

        activeShirt.Initialize(this, shirtItem);
    }

    public void NotifyShirtCollected(ShirtFoldingController shirt)
    {
        if (activeShirt == shirt)
        {
            activeShirt = null;
        }
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
