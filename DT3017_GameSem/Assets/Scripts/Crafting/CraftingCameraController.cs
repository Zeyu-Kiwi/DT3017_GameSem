using System;
using System.Collections;
using UnityEngine;

public class CraftingCameraController : MonoBehaviour
{
    [SerializeField] private Camera craftingCamera;
    [SerializeField, Min(0f)] private float transitionDuration = 0.6f;

    private Camera playerCamera;
    private Coroutine transitionRoutine;
    private Vector3 craftingPosition;
    private Quaternion craftingRotation;
    private float craftingFieldOfView;

    public Camera CraftingCamera => craftingCamera;
    public bool IsTransitioning => transitionRoutine != null;

    private void Awake()
    {
        if (craftingCamera == null)
        {
            craftingCamera = GetComponentInChildren<Camera>(true);
        }

        if (craftingCamera != null)
        {
            craftingPosition = craftingCamera.transform.position;
            craftingRotation = craftingCamera.transform.rotation;
            craftingFieldOfView = craftingCamera.fieldOfView;
            craftingCamera.enabled = false;
        }
    }

    public void Open(Camera sourceCamera, Action completed)
    {
        if (craftingCamera == null || sourceCamera == null || craftingCamera == sourceCamera)
        {
            completed?.Invoke();
            return;
        }

        playerCamera = sourceCamera;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        craftingCamera.transform.SetPositionAndRotation(
            playerCamera.transform.position,
            playerCamera.transform.rotation);
        craftingCamera.fieldOfView = playerCamera.fieldOfView;
        craftingCamera.enabled = true;
        playerCamera.enabled = false;

        transitionRoutine = StartCoroutine(Transition(
            craftingPosition,
            craftingRotation,
            craftingFieldOfView,
            completed,
            false));
    }

    public void Close(Action completed)
    {
        if (craftingCamera == null || playerCamera == null)
        {
            completed?.Invoke();
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(Transition(
            playerCamera.transform.position,
            playerCamera.transform.rotation,
            playerCamera.fieldOfView,
            completed,
            true));
    }

    public void ResetImmediately()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = true;
        }

        if (craftingCamera != null)
        {
            craftingCamera.transform.SetPositionAndRotation(
                craftingPosition,
                craftingRotation);
            craftingCamera.fieldOfView = craftingFieldOfView;
            craftingCamera.enabled = false;
        }

        playerCamera = null;
    }

    private IEnumerator Transition(
        Vector3 targetPosition,
        Quaternion targetRotation,
        float targetFieldOfView,
        Action completed,
        bool returnToPlayer)
    {
        Vector3 startPosition = craftingCamera.transform.position;
        Quaternion startRotation = craftingCamera.transform.rotation;
        float startFieldOfView = craftingCamera.fieldOfView;
        float elapsed = 0f;

        while (elapsed < transitionDuration && transitionDuration > 0f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            craftingCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t));
            craftingCamera.fieldOfView =
                Mathf.Lerp(startFieldOfView, targetFieldOfView, t);

            yield return null;
        }

        craftingCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
        craftingCamera.fieldOfView = targetFieldOfView;

        if (returnToPlayer)
        {
            playerCamera.enabled = true;
            craftingCamera.enabled = false;
        }

        transitionRoutine = null;
        completed?.Invoke();
    }
}
