using DialogueEditor;
using UnityEngine;

/// <summary>Shows the crosshair only while the player is free to aim.</summary>
[DisallowMultipleComponent]
public sealed class CrosshairVisibilityController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Assign the crosshair child under Default UIs. Keep this controller on an always-active parent or sibling.")]
    [SerializeField] private GameObject crosshairRoot;

    [Header("Player")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteractUI interactionPrompt;

    [Header("Escape")]
    [SerializeField] private EscapeFlowController escapeFlow;

    private bool? lastVisible;

    private void Awake()
    {
        ResolveReferences();
        if (crosshairRoot == gameObject)
        {
            Debug.LogError("Crosshair Root must be a separate child object.", this);
            crosshairRoot = null;
        }
        if (crosshairRoot != null) crosshairRoot.SetActive(false);
    }

    private void OnEnable()
    {
        lastVisible = null;
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (crosshairRoot != null) crosshairRoot.SetActive(false);
        lastVisible = null;
    }

    private void Refresh()
    {
        if (crosshairRoot == null) return;
        if (playerController == null || interactionPrompt == null || escapeFlow == null)
            ResolveReferences();

        bool visible = playerController != null &&
                       playerController.isActiveAndEnabled &&
                       playerController.CanLook &&
                       Time.timeScale > 0f &&
                       Cursor.lockState == CursorLockMode.Locked &&
                       !Cursor.visible &&
                       (interactionPrompt == null || !interactionPrompt.IsVisible) &&
                       (escapeFlow == null || !escapeFlow.IsEscapeActive) &&
                       (ConversationManager.Instance == null ||
                        !ConversationManager.Instance.IsConversationActive);

        if (lastVisible == visible) return;
        crosshairRoot.SetActive(visible);
        lastVisible = visible;
    }

    private void ResolveReferences()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<FirstPersonController>();
        if (interactionPrompt == null && playerController != null)
            interactionPrompt = playerController.GetComponent<PlayerInteractUI>();
        if (escapeFlow == null)
            escapeFlow = FindFirstObjectByType<EscapeFlowController>();
    }
}
