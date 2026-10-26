using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class VentCoverInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private ItemData requiredTool;
    [SerializeField, TextArea] private string missingToolMessage =
        "I need a Phillips screwdriver to remove the screws holding this vent cover.";
    [SerializeField, TextArea] private string toolAvailableMessage =
        "My Phillips screwdriver fits these screws.";
    [Tooltip("Future cover removal can be connected here. The tool is not consumed.")]
    [SerializeField] private UnityEvent onToolAvailable = new UnityEvent();

    [Header("Missing Tool Shake")]
    [Tooltip("Move the cover and screws together, keeping the interaction collider still.")]
    [SerializeField] private Transform shakeTarget;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.35f;
    [SerializeField, Min(0f)] private float shakeDistance = 0.012f;
    [SerializeField, Min(0f)] private float shakeAngle = 0.8f;
    [SerializeField, Min(0f)] private float shakeFrequency = 18f;

    [SerializeField] private VentInspectionController inspection;

    private Coroutine shakeRoutine;
    private Transform activeShakeTarget;
    private Vector3 restingPosition;
    private Quaternion restingRotation;

    public bool CanInteract => isActiveAndEnabled && (inspection == null || (!inspection.IsInspecting && !inspection.IsComplete));
    public ItemData RequiredTool => requiredTool;
    public bool IsShaking => shakeRoutine != null;
    public UnityEvent OnToolAvailable => onToolAvailable;

    public void Interact(GameObject player)
    {
        if (!CanInteract || player == null || IsShaking) return;

        InventoryManager inventory = InventoryManager.Instance;
        if (requiredTool != null && inventory != null && inventory.Has(requiredTool, 1))
        {
            if (inspection != null) inspection.Begin(player);
            else ShowMessage(toolAvailableMessage);
            onToolAvailable.Invoke();
            return;
        }

        ShowMessage(missingToolMessage);
        shakeRoutine = StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        activeShakeTarget = shakeTarget != null ? shakeTarget : transform;
        restingPosition = activeShakeTarget.localPosition;
        restingRotation = activeShakeTarget.localRotation;
        float duration = Mathf.Max(0.01f, shakeDuration);
        float elapsed = 0f;
        while (elapsed < duration && activeShakeTarget != null)
        {
            float phase = elapsed * Mathf.Max(0f, shakeFrequency) * Mathf.PI * 2f;
            float strength = 1f - elapsed / duration;
            float wave = Mathf.Sin(phase) * strength;
            activeShakeTarget.localPosition = restingPosition +
                Vector3.up * (wave * Mathf.Max(0f, shakeDistance));
            activeShakeTarget.localRotation = restingRotation *
                Quaternion.Euler(wave * Mathf.Max(0f, shakeAngle), 0f, 0f);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        RestorePose();
        shakeRoutine = null;
    }

    private void OnDisable()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = null;
        RestorePose();
    }

    private void RestorePose()
    {
        if (activeShakeTarget == null) return;
        activeShakeTarget.localPosition = restingPosition;
        activeShakeTarget.localRotation = restingRotation;
        activeShakeTarget = null;
    }

    private void ShowMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowMessage(message);
        else
            Debug.Log(message, this);
    }
}