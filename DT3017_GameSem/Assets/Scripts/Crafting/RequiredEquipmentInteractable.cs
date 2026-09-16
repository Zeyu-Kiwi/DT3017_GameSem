using UnityEngine;
using UnityEngine.Events;

public class RequiredEquipmentInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private bool canInteract = true;
    [SerializeField] private ItemData requiredEquipment;
    [SerializeField] private UnityEvent onEquipmentOwned;
    [SerializeField] private UnityEvent onEquipmentMissing;

    public bool CanInteract => canInteract;

    public void Interact(GameObject player)
    {
        if (!canInteract)
        {
            return;
        }

        bool ownsEquipment = InventoryManager.Instance != null &&
                             requiredEquipment != null &&
                             InventoryManager.Instance.Has(requiredEquipment, 1);

        if (ownsEquipment)
        {
            onEquipmentOwned?.Invoke();
        }
        else
        {
            onEquipmentMissing?.Invoke();
        }
    }

    public void SetCanInteract(bool value)
    {
        canInteract = value;
    }
}
