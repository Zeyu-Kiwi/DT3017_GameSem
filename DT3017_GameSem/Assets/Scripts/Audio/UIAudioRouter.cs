using System.Collections.Generic;
using DialogueEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Mouse hover/click audio for all buttons under this canvas, including runtime options.</summary>
[DisallowMultipleComponent]
public sealed class UIAudioRouter : MonoBehaviour
{
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private PointerEventData pointer;
    private EventSystem pointerEventSystem;
    private Button hoveredButton;
    private Button pressedButton;

    private void Update()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || GameAudioHub.Instance == null) return;

        if (pointer == null || pointerEventSystem != eventSystem)
        {
            pointer = new PointerEventData(eventSystem);
            pointerEventSystem = eventSystem;
        }
        pointer.position = Input.mousePosition;
        hits.Clear();
        eventSystem.RaycastAll(pointer, hits);

        Button button = null;
        foreach (RaycastResult hit in hits)
        {
            if (hit.gameObject.transform.IsChildOf(transform))
            {
                button = hit.gameObject.GetComponentInParent<Button>();
                if (button == null || !button.IsActive() || !button.IsInteractable())
                    button = null;
            }
            break; // A higher UI or world hit blocks buttons behind it.
        }

        if (button != hoveredButton)
        {
            hoveredButton = button;
            if (button != null) PlayHover(button);
        }

        if (Input.GetMouseButtonDown(0)) pressedButton = button;
        if (Input.GetMouseButtonUp(0))
        {
            if (button != null && button == pressedButton) PlayClick(button);
            pressedButton = null;
        }
    }

    private static void PlayHover(Button button)
    {
        UIAudioOverride custom = button.GetComponent<UIAudioOverride>();
        if (custom != null)
        {
            if (!custom.SuppressHover) GameAudioHub.Instance.PlayDefaultHover(custom.HoverClip);
        }
        else if (button.GetComponent<UIConversationButton>() != null)
            GameAudioHub.Instance.PlayDialogueOptionHover();
        else
            GameAudioHub.Instance.PlayDefaultHover();
    }

    private static void PlayClick(Button button)
    {
        UIConversationButton dialogueButton = button.GetComponent<UIConversationButton>();
        if (dialogueButton != null && !ConversationManager.Instance.CanSelectOptions) return;

        UIAudioOverride custom = button.GetComponent<UIAudioOverride>();
        if (custom == null || !custom.SuppressClick)
            GameAudioHub.Instance.PlayDefaultClick(custom != null ? custom.ClickClip : null);
    }

    private void OnDisable()
    {
        hoveredButton = null;
        pressedButton = null;
    }
}
