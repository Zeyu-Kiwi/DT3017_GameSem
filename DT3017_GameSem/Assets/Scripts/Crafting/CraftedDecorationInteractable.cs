using UnityEngine;
using UnityEngine.Events;

/// <summary>Repeatable F interaction for a crafted table decoration.</summary>
[DisallowMultipleComponent]
public sealed class CraftedDecorationInteractable : MonoBehaviour, IInteractable, ISeatRestrictedInteractable
{
    [Header("Interaction")]
    [SerializeField] private bool canInteract = true;
    [Tooltip("When off, the player must be seated. When on, standing and seated players can interact.")]
    [SerializeField] private bool allowWhileStanding = true;

    [Header("Responses")]
    [Tooltip("Use for parameterless actions such as AudioSource.Play or Animator.SetTrigger.")]
    [SerializeField] private UnityEvent onInteracted = new UnityEvent();
    [Tooltip("Passes the player GameObject to responses that need it.")]
    [SerializeField] private UnityEvent<GameObject> onInteractedWithPlayer =
        new UnityEvent<GameObject>();

    public bool CanInteract => canInteract &&
        PlayerSeatController.AllowsInteraction(allowWhileStanding);
    public bool IsSeatRestrictionBlocking => canInteract &&
        !PlayerSeatController.AllowsInteraction(allowWhileStanding);

    public void Interact(GameObject player)
    {
        if (!CanInteract || player == null) return;

        onInteracted.Invoke();
        onInteractedWithPlayer.Invoke(player);
    }

    public void SetCanInteract(bool value)
    {
        canInteract = value;
    }
}
