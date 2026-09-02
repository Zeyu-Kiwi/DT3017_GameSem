using UnityEngine;
using UnityEngine.Events;

public class EventInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private bool canInteract = true;
    [SerializeField] private UnityEvent onInteract;

    public bool CanInteract => canInteract;

    public void Interact(GameObject player)
    {
        if (canInteract)
        {
            onInteract.Invoke();
        }
    }

    public void SetCanInteract(bool value)
    {
        canInteract = value;
    }
}