 using UnityEngine;
using DialogueEditor;

public class NPC_ConversationStart : MonoBehaviour
{
    [SerializeField] private NPCConversation npcConversation;
    [SerializeField] private PlayerInteractUI playerInteractUI;
    private bool playerIsInRange;

    private void Update()
    {
        if (playerIsInRange && Input.GetKeyDown(KeyCode.F))
        {
            playerInteractUI.SetInteractable(false);
            ConversationManager.Instance.StartConversation(npcConversation);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerIsInRange = true;
        playerInteractUI.SetInteractable(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerIsInRange = false;
        playerInteractUI.SetInteractable(false);
    }
}

