using UnityEngine;
using DialogueEditor;

public class NPC_ConversationStart : MonoBehaviour, IInteractable
{
    [SerializeField] private NPCConversation npcConversation;

    public bool CanInteract => npcConversation != null;

    public void Interact(GameObject player)
    {
        ConversationManager.Instance.StartConversation(npcConversation);
    }
}
