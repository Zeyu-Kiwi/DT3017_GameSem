using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int quantity = 1;

    public bool CanInteract => item != null && quantity > 0;

    public void Interact(GameObject player)
    {
        if (InventoryManager.Instance == null || item == null)
        {
            return;
        }

        int added = InventoryManager.Instance.AddItem(item, quantity);

        if (added == 0)
        {
            ShowMessage(item.DisplayName + " is full.");
            return;
        }

        quantity -= added;

        if (quantity > 0)
        {
            ShowMessage(
                "Picked up " + added + " " + item.DisplayName +
                ". " + item.DisplayName + " is now full.");
        }
        else
        {
            ShowMessage("Picked up " + added + " " + item.DisplayName + ".");
            Destroy(gameObject);
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
