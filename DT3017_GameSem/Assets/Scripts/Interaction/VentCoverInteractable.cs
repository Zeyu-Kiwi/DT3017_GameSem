using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VentCoverInteractable : MonoBehaviour
{
    [Header("Shake Response")]
    [Tooltip("Move the cover and screws together, keeping the interaction collider still.")]
    [SerializeField] private Transform shakeTarget;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.35f;
    [SerializeField, Min(0f)] private float shakeDistance = 0.012f;
    [SerializeField, Min(0f)] private float shakeAngle = 0.8f;
    [SerializeField, Min(0f)] private float shakeFrequency = 18f;

    private Coroutine shakeRoutine;
    private Transform activeShakeTarget;
    private Vector3 restingPosition;
    private Quaternion restingRotation;

    public bool IsShaking => shakeRoutine != null;

    public void PlayShake()
    {
        if (!isActiveAndEnabled || IsShaking)
        {
            return;
        }

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
}
