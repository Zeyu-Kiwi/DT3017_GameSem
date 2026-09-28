using UnityEngine;
using UnityEngine.Events;

public class DoorToolInteractable : MonoBehaviour, IInteractable
{
    [Header("Door")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private ItemData requiredTool;

    [Header("Responses")]
    [SerializeField] private UnityEvent onCorrectToolUsed;
    [SerializeField] private UnityEvent onWrongToolUsed;

    public bool CanInteract => canInteract;
    public ItemData RequiredTool => requiredTool;

    public void Interact(GameObject player)
    {
        if (!canInteract || player == null)
        {
            return;
        }

        ToolSelectionUI toolSelectionUI = ToolSelectionUI.Instance;
        if (toolSelectionUI == null)
        {
            toolSelectionUI = FindFirstObjectByType<ToolSelectionUI>();
        }

        if (toolSelectionUI == null)
        {
            Debug.LogError(
                "DoorToolInteractable could not find a ToolSelectionUI in the scene.",
                this);
            return;
        }

        toolSelectionUI.Open(this, player);
    }

    public bool IsCorrectTool(ItemData selectedTool)
    {
        return requiredTool != null && selectedTool == requiredTool;
    }

    internal void InvokeCorrectToolResponse()
    {
        onCorrectToolUsed?.Invoke();
    }

    internal void InvokeWrongToolResponse()
    {
        onWrongToolUsed?.Invoke();
    }

    public void SetCanInteract(bool value)
    {
        canInteract = value;
    }
}
