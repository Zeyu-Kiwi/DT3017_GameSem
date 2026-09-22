using System;
using System.Collections;
using UnityEngine;

public class CraftingDrawerController : MonoBehaviour
{
    [Header("Drawer")]
    [Tooltip("The transform that moves. Leave empty to move this GameObject.")]
    [SerializeField] private Transform movingTransform;
    [SerializeField] private Vector3 closedLocalPosition;
    [SerializeField] private Vector3 openLocalPosition;
    [SerializeField, Min(0f)] private float movementDuration = 0.6f;
    [SerializeField] private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Contents")]
    [Tooltip("Material and equipment sections. They appear when opening begins and hide after closing finishes.")]
    [SerializeField] private GameObject sectionsContainer;
    [SerializeField] private bool startClosed = true;

    private Coroutine movementRoutine;

    public bool IsDrawerOpen { get; private set; }
    public bool IsMoving => movementRoutine != null;

    private void Awake()
    {
        if (movingTransform == null)
        {
            movingTransform = transform;
        }

        if (startClosed)
        {
            movingTransform.localPosition = closedLocalPosition;
            IsDrawerOpen = false;

            if (sectionsContainer != null)
            {
                sectionsContainer.SetActive(false);
            }
        }
        else
        {
            movingTransform.localPosition = openLocalPosition;
            IsDrawerOpen = true;

            if (sectionsContainer != null)
            {
                sectionsContainer.SetActive(true);
            }
        }
    }

    public void OpenDrawer(Action completed)
    {
        if (IsMoving)
        {
            return;
        }

        // The boss considers the drawer open from the moment opening begins.
        IsDrawerOpen = true;

        if (sectionsContainer != null)
        {
            sectionsContainer.SetActive(true);
        }

        StartMovement(openLocalPosition, false, completed);
    }

    public void CloseDrawer(Action completed)
    {
        if (IsMoving)
        {
            return;
        }

        // IsDrawerOpen deliberately remains true throughout the closing movement.
        StartMovement(closedLocalPosition, true, completed);
    }

    private void StartMovement(
        Vector3 targetLocalPosition,
        bool finishingClosed,
        Action completed)
    {
        if (movingTransform == null)
        {
            FinishMovement(targetLocalPosition, finishingClosed, completed);
            return;
        }

        if (movementDuration <= 0f)
        {
            FinishMovement(targetLocalPosition, finishingClosed, completed);
            return;
        }

        movementRoutine = StartCoroutine(MoveRoutine(
            targetLocalPosition,
            finishingClosed,
            completed));
    }

    private IEnumerator MoveRoutine(
        Vector3 targetLocalPosition,
        bool finishingClosed,
        Action completed)
    {
        Vector3 startLocalPosition = movingTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < movementDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / movementDuration);
            float curvedTime = movementCurve == null
                ? normalizedTime
                : movementCurve.Evaluate(normalizedTime);

            movingTransform.localPosition = Vector3.LerpUnclamped(
                startLocalPosition,
                targetLocalPosition,
                curvedTime);

            yield return null;
        }

        FinishMovement(targetLocalPosition, finishingClosed, completed);
    }

    private void FinishMovement(
        Vector3 targetLocalPosition,
        bool finishingClosed,
        Action completed)
    {
        if (movingTransform != null)
        {
            movingTransform.localPosition = targetLocalPosition;
        }

        movementRoutine = null;

        if (finishingClosed)
        {
            if (sectionsContainer != null)
            {
                sectionsContainer.SetActive(false);
            }

            // The boss considers it closed only after the movement has completed.
            IsDrawerOpen = false;
        }

        completed?.Invoke();
    }

    [ContextMenu("Use Current Local Position As Closed")]
    private void UseCurrentPositionAsClosed()
    {
        Transform target = movingTransform == null ? transform : movingTransform;
        closedLocalPosition = target.localPosition;
    }

    [ContextMenu("Use Current Local Position As Open")]
    private void UseCurrentPositionAsOpen()
    {
        Transform target = movingTransform == null ? transform : movingTransform;
        openLocalPosition = target.localPosition;
    }
}
