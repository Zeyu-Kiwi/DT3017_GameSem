using UnityEngine;
using DialogueEditor;

//[RequireComponent(typeof(PlayerInteractUI))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode interactionKey = KeyCode.F;

    [Header("Detection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [Tooltip("Solid layers that stop interaction rays. Trigger colliders on these layers are ignored.")]
    [SerializeField] private LayerMask interactionBlockingLayers = 1 << 7;

    [Header("Seat feedback")]
    [SerializeField] private string seatRequiredMessage = "Sit in the chair to interact with this.";

    [SerializeField] private PlayerInteractUI interactUI;
    private IInteractable currentInteractable;
    private bool seatRestrictedTarget;
    private InteractionOutlineHighlighter outlineHighlighter;
    private bool interactionEnabled = true;
    private readonly System.Collections.Generic.HashSet<object> interactionLocks =
        new System.Collections.Generic.HashSet<object>();

    public KeyCode InteractionKey => PlayerOptions.GetKey(GameplayAction.Interact, interactionKey);

    public void LockInteraction(object owner)
    {
        if (owner == null) throw new System.ArgumentNullException(nameof(owner));
        interactionLocks.Add(owner);
        seatRestrictedTarget = false;
        SetCurrentInteractable(null);
    }

    public void UnlockInteraction(object owner)
    {
        if (owner != null) interactionLocks.Remove(owner);
    }

    private void Awake()
    {
        interactUI = GetComponent<PlayerInteractUI>();
        interactUI?.SetInteractionKey(InteractionKey);
        outlineHighlighter = GetComponent<InteractionOutlineHighlighter>();
        if (outlineHighlighter == null) outlineHighlighter = gameObject.AddComponent<InteractionOutlineHighlighter>();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        PlayerOptions.Changed += RefreshInteractionKey;
        RefreshInteractionKey();
        ConversationManager.OnConversationStarted += DisableInteraction;
        ConversationManager.OnConversationEnded += EnableInteraction;
    }

    private void OnDisable()
    {
        SetCurrentInteractable(null);
        PlayerOptions.Changed -= RefreshInteractionKey;
        ConversationManager.OnConversationStarted -= DisableInteraction;
        ConversationManager.OnConversationEnded -= EnableInteraction;
    }

    private void RefreshInteractionKey()
    {
        if (interactUI != null) interactUI.SetInteractionKey(InteractionKey);
    }

    private void Update()
    {
        if (!interactionEnabled || interactionLocks.Count > 0)
            return;

        FindInteractable();

        if (Input.GetKeyDown(InteractionKey))
        {
            if (currentInteractable != null)
                InteractCurrent();
            else if (seatRestrictedTarget)
                NotificationManager.Instance?.ShowMessage(seatRequiredMessage);
        }
    }

    private void InteractCurrent()
    {
        if (currentInteractable == null || !currentInteractable.CanInteract) return;
        // Keep the inspected object even if its interaction clears the current target.
        Component target = currentInteractable as Component;
        currentInteractable.Interact(gameObject);
        if (target != null)
        {
            GameVariableAction[] variableActions =
                target.GetComponents<GameVariableAction>();
            for (int i = 0; i < variableActions.Length; i++)
                variableActions[i].ExecuteFromInteraction();
        }
    }

    private void FindInteractable()
    {
        IInteractable detectedInteractable = null;
        seatRestrictedTarget = false;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        // A layer selected in both masks acts as a blocker, never as an interactable.
        int effectiveInteractableLayers =
            interactableLayers.value & ~interactionBlockingLayers.value;

        if (Physics.Raycast(
                ray,
                out RaycastHit interactableHit,
                interactionDistance,
                effectiveInteractableLayers,
                QueryTriggerInteraction.Collide))
        {
            bool blocked = Physics.Raycast(
                ray,
                out RaycastHit blockingHit,
                interactableHit.distance,
                interactionBlockingLayers,
                QueryTriggerInteraction.Ignore) &&
                blockingHit.distance < interactableHit.distance - 0.001f;

            if (!blocked)
            {
                detectedInteractable =
                    interactableHit.collider.GetComponentInParent<IInteractable>();
            }

            if (detectedInteractable != null &&
                !detectedInteractable.CanInteract)
            {
                seatRestrictedTarget = detectedInteractable is ISeatRestrictedInteractable restricted &&
                                       restricted.IsSeatRestrictionBlocking;
                detectedInteractable = null;
            }
        }

        SetCurrentInteractable(detectedInteractable);
    }

    private void SetCurrentInteractable(IInteractable interactable)
    {
        outlineHighlighter?.SetTarget(interactable as Component);
        if (ReferenceEquals(currentInteractable, interactable))
            return;

        currentInteractable = interactable;


        if (currentInteractable != null)
        {
            interactUI.Show();
        }
        else
        {
            interactUI.Hide();
        }
    }

    private void DisableInteraction()
    {
        interactionEnabled = false;
        seatRestrictedTarget = false;
        SetCurrentInteractable(null);
    }

    private void EnableInteraction()
    {
        interactionEnabled = true;
    }

    private void OnValidate()
    {
        if (interactUI == null)
        {
            interactUI = GetComponent<PlayerInteractUI>();
        }

        interactUI?.SetInteractionKey(InteractionKey);
    }
}
