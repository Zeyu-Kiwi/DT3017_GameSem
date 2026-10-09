using UnityEngine;
using DialogueEditor;

public class NPC_ConversationStart : MonoBehaviour, IInteractable
{
    [SerializeField] private NPCConversation npcConversation;

    public bool CanInteract => npcConversation != null;

    public void Interact(GameObject player)
    {
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
