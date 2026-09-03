using UnityEngine;
using DialogueEditor;

//[RequireComponent(typeof(PlayerInteractUI))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayers = ~0;

    [SerializeField] private PlayerInteractUI interactUI;
    private IInteractable currentInteractable;
    private bool interactionEnabled = true;

    private void Awake()
    {
        interactUI = GetComponent<PlayerInteractUI>();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        ConversationManager.OnConversationStarted += DisableInteraction;
        ConversationManager.OnConversationEnded += EnableInteraction;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationStarted -= DisableInteraction;
        ConversationManager.OnConversationEnded -= EnableInteraction;
    }

    private void Update()
    {
        if (!interactionEnabled)
            return;

        FindInteractable();

        if (currentInteractable != null &&
            Input.GetKeyDown(KeyCode.F))
        {
            currentInteractable.Interact(gameObject);
        }
    }

    private void FindInteractable()
    {
        IInteractable detectedInteractable = null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance,
                interactableLayers,
                QueryTriggerInteraction.Collide))
        {
            detectedInteractable =
                hit.collider.GetComponentInParent<IInteractable>();

            if (detectedInteractable != null &&
                !detectedInteractable.CanInteract)
            {
                detectedInteractable = null;
            }
        }

        SetCurrentInteractable(detectedInteractable);
    }

    private void SetCurrentInteractable(IInteractable interactable)
    {
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
        SetCurrentInteractable(null);
    }

    private void EnableInteraction()
    {
        interactionEnabled = true;
    }
}