using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reusable interaction that opens ToolSelectionUI and responds to the selected tool.
/// Place this on any escape route or other object that requires a tool.
/// </summary>
[DisallowMultipleComponent]
public sealed class ToolSelectionInteractable : MonoBehaviour, IInteractable, IToolSelectionTarget
{
    [Header("Interaction")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private ItemData requiredTool;

    [Header("Messages")]
    [SerializeField, TextArea] private string noToolMessage =
        "You do not have any tools.";
    [SerializeField, TextArea] private string wrongToolMessage =
        "That tool will not work here.";
    [SerializeField, TextArea] private string correctToolMessage =
        "That tool should work.";

    [Header("Behaviour")]
    [Tooltip("When enabled, the tool UI stays open after a wrong tool is selected.")]
    [SerializeField] private bool keepUIOpenAfterWrongTool = true;
    [Tooltip("Use this for one-use routes. Leave it off if the player may exit and retry a longer interaction.")]
    [SerializeField] private bool disableAfterCorrectTool;

    [Header("Responses")]
    [Tooltip("Passes the player who originally interacted with this object.")]
    [SerializeField] private UnityEvent<GameObject> onCorrectToolUsed =
        new UnityEvent<GameObject>();
    [SerializeField] private UnityEvent onWrongToolUsed = new UnityEvent();
    [SerializeField] private UnityEvent onNoToolsAvailable = new UnityEvent();

    public bool CanInteract => canInteract;
    public ItemData RequiredTool => requiredTool;
    public bool KeepSelectionOpenAfterWrongTool => keepUIOpenAfterWrongTool;

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
                "ToolSelectionInteractable could not find a ToolSelectionUI in the scene.",
                this);
            return;
        }

        toolSelectionUI.Open(this, player);
    }

    public bool IsCorrectTool(ItemData selectedTool)
    {
        return requiredTool != null && selectedTool == requiredTool;
    }

    public void HandleCorrectTool(GameObject player)
    {
        if (disableAfterCorrectTool)
        {
            canInteract = false;
        }

        ShowMessage(correctToolMessage);
        onCorrectToolUsed.Invoke(player);
    }

    public void HandleWrongTool()
    {
        ShowMessage(wrongToolMessage);
        onWrongToolUsed.Invoke();
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
