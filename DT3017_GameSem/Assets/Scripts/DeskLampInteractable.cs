using UnityEngine;

/// <summary>F interaction that switches a desk lamp and its optional bulb glow together.</summary>
[DisallowMultipleComponent]
public sealed class DeskLampInteractable : MonoBehaviour, IInteractable
{
    [Header("Lamp")]
    [Tooltip("The lamp's Light component. If empty, searches this object's children at runtime.")]
    [SerializeField] private Light lampLight;
    [Tooltip("Optional child object used only for the visible bulb glow, such as an emissive plane.")]
    [SerializeField] private GameObject bulbGlow;
    [SerializeField] private bool startsOn = true;

    [Header("Audio")]
    [SerializeField] private AudioClip switchClick;
    [SerializeField, Range(0f, 1f)] private float switchVolume = 1f;

    private bool isOn;

    public bool CanInteract => isActiveAndEnabled && lampLight != null;
    public bool IsOn => isOn;

    private void Awake()
    {
        if (lampLight == null) lampLight = GetComponentInChildren<Light>(true);
        isOn = startsOn;
        ApplyVisualState();
    }

    public void Interact(GameObject player)
    {
        if (!CanInteract) return;

        isOn = !isOn;
        ApplyVisualState();
        WorldAudioOneShot.Play(switchClick, transform.position, switchVolume);
    }

    private void ApplyVisualState()
    {
        if (lampLight != null) lampLight.enabled = isOn;

        // Keep the interactable root alive so the lamp can be turned back on.
        if (bulbGlow != null && bulbGlow != gameObject &&
            !transform.IsChildOf(bulbGlow.transform))
            bulbGlow.SetActive(isOn);
    }
}
