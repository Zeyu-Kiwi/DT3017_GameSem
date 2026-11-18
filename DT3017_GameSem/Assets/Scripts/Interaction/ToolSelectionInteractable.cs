using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reusable interaction that checks whether the player owns the required tool.
/// Place this on any escape route or other object that requires a tool.
/// </summary>
[DisallowMultipleComponent]
public sealed class ToolSelectionInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private ItemData requiredTool;

    [Header("Messages")]
    [SerializeField, TextArea] private string noToolMessage =
        "You need a tool for this.";

    [Header("Behaviour")]
    [Tooltip("Use this for one-use routes. Leave it off if the player may exit and retry a longer interaction.")]
    [SerializeField] private bool disableAfterCorrectTool;

    [Header("Responses")]
    [Tooltip("Passes the player who originally interacted with this object.")]
    [SerializeField] private UnityEvent<GameObject> onCorrectToolUsed =
        new UnityEvent<GameObject>();
    [SerializeField] private UnityEvent onNoToolsAvailable = new UnityEvent();

    [Tooltip("Optional follow-up interaction, used when it becomes available after completing this tool task.")]
    [SerializeField] private MonoBehaviour interactionAfterCompletion;

    public bool CanInteract => canInteract || (interactionAfterCompletion is IInteractable followUp && followUp.CanInteract);
    public ItemData RequiredTool => requiredTool;

    public void Interact(GameObject player)
    {
        if (player != null && interactionAfterCompletion is IInteractable followUp && followUp.CanInteract)
        {
            followUp.Interact(player);
            return;
        }
        if (!canInteract || player == null)
        {
            return;
        }

        if (requiredTool == null)
        {
            Debug.LogError("ToolSelectionInteractable needs a Required Tool.", this);
            return;
        }

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            Debug.LogError("ToolSelectionInteractable could not find an InventoryManager.", this);
            return;
        }

        if (inventory.Has(requiredTool, 1))
        {
            HandleCorrectTool(player);
        }
        else
        {
            HandleNoToolsAvailable();
        }
    }

    public void HandleCorrectTool(GameObject player)
    {
        if (disableAfterCorrectTool)
        {
            canInteract = false;
        }

        onCorrectToolUsed.Invoke(player);
    }

    public void HandleNoToolsAvailable()
    {
        ShowMessage(noToolMessage);
        onNoToolsAvailable.Invoke();
    }

    public void SetCanInteract(bool value)
    {
        canInteract = value;
    }

    private void ShowMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

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
