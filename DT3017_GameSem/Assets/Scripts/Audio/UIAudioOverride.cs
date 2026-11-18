using UnityEngine;

/// <summary>Optional per-button sounds; an assigned clip replaces the shared default.</summary>
[DisallowMultipleComponent]
public sealed class UIAudioOverride : MonoBehaviour
{
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private bool suppressHover;
    [SerializeField] private bool suppressClick;

    public AudioClip HoverClip => hoverClip;
    public AudioClip ClickClip => clickClip;
    public bool SuppressHover => suppressHover;
    public bool SuppressClick => suppressClick;
}
