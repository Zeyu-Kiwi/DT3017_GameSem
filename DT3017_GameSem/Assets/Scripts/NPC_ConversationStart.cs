using UnityEngine;
using DialogueEditor;

public class NPC_ConversationStart : MonoBehaviour, IInteractable
{
    [SerializeField] private NPCConversation npcConversation;

    private readonly System.Collections.Generic.HashSet<object> interactionLocks =
        new System.Collections.Generic.HashSet<object>();

    public bool CanInteract => isActiveAndEnabled && npcConversation != null && interactionLocks.Count == 0;

    public void LockInteraction(object owner)
    {
        if (owner != null) interactionLocks.Add(owner);
    }

    public void UnlockInteraction(object owner)
    {
        if (owner != null) interactionLocks.Remove(owner);
    }

    public void Interact(GameObject player)
    {
        if (!CanInteract) return;
        if (DialogueVariableBridge.Instance != null)
        {
            DialogueVariableBridge.Instance.StartConversation(npcConversation);
        }
        else
        {
            Debug.LogWarning(
                "No DialogueVariableBridge exists. Starting the conversation without global variables.",
                this);
            ConversationManager.Instance.StartConversation(npcConversation);
        }
    }
}
