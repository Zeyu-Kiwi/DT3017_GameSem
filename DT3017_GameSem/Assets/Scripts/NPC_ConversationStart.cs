using UnityEngine;
using DialogueEditor;

public class NPC_01_ConversationStart : MonoBehaviour
{
    [SerializeField] private NPCConversation npcConversation;
    private bool playerIsInRange;

    private void Update()
    {
        if (playerIsInRange && Input.GetKeyDown(KeyCode.F))
        {
            ConversationManager.Instance.StartConversation(npcConversation);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInRange = false;
        }
    }
}

