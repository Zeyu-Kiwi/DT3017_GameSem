using System.Collections;
using UnityEngine;

/// <summary>Moves the player between the chair and the standing position.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(FirstPersonController))]
public class PlayerSeatController : MonoBehaviour
{
    [Header("Chair Positions (player root, not camera height)")]
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Transform standPoint;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.4f;
    [Tooltip("Added to the camera's original local position while seated. Use a negative Y value to lower the view.")]
    [SerializeField] private Vector3 seatedCameraOffset = new Vector3(0f, -0.4f, 0f);

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool isSeated;
    [SerializeField] private bool isTransitioning;

    private FirstPersonController controller;
    private PlayerInteractor interactor;
    private Rigidbody playerBody;
    private Transform cameraTransform;
    private Vector3 standingCameraLocalPosition;
    private Coroutine transitionRoutine;

    public static PlayerSeatController Instance { get; private set; }
    public bool IsSeated => isSeated;
    public bool IsTransitioning => isTransitioning;
    public Transform SeatPoint => seatPoint;
    public bool HasPositions => seatPoint != null && standPoint != null;

    public static bool AllowsInteraction(bool allowWhileStanding)
    {
        return allowWhileStanding || (Instance != null && Instance.IsSeated);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("More than one PlayerSeatController exists in this scene.", this);
        Instance = this;
        controller = GetComponent<FirstPersonController>();
        interactor = GetComponent<PlayerInteractor>();
        playerBody = GetComponent<Rigidbody>();
        cameraTransform = controller.cameraTransform;
        if (cameraTransform != null)
            standingCameraLocalPosition = cameraTransform.localPosition;
    }

    private void OnDisable()
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = null;
        isTransitioning = false;
        isSeated = false;
        interactor?.UnlockInteraction(this);
        controller?.UnlockMovement(this);
        if (cameraTransform != null)
            cameraTransform.localPosition = standingCameraLocalPosition;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ToggleSeat()
    {
        if (isTransitioning || !HasPositions) return;
        StartTransition(isSeated ? standPoint : seatPoint, !isSeated);
    }

    public void SnapSeatedAt(Transform point)
    {
        if (point == null) return;
        CancelTransition();
        controller.LockMovement(this, false);
        controller.TeleportTo(point);
        isSeated = true;
        ApplyCameraOffset(1f);
    }

    /// <summary>Call after an external system teleports the player.</summary>
    public void SetSeatedAfterTeleport(bool seated)
    {
        CancelTransition();
        isSeated = seated;
        if (seated) controller.LockMovement(this, false);
        else controller.UnlockMovement(this);
        ApplyCameraOffset(seated ? 1f : 0f);
    }

    private void StartTransition(Transform destination, bool willBeSeated)
    {
        controller.LockMovement(this, true);
        interactor?.LockInteraction(this);
        transitionRoutine = StartCoroutine(MoveTo(destination, willBeSeated));
    }

    private IEnumerator MoveTo(Transform destination, bool willBeSeated)
    {
        isTransitioning = true;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 startCameraPosition = cameraTransform != null
            ? cameraTransform.localPosition : Vector3.zero;
        Vector3 endCameraPosition = standingCameraLocalPosition +
            (willBeSeated ? seatedCameraOffset : Vector3.zero);
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(elapsed / Mathf.Max(0.001f, transitionDuration)));
            // Chair interactions move the player without changing their view.
            PlacePlayer(Vector3.Lerp(startPosition, destination.position, t),
                startRotation);
            if (cameraTransform != null)
                cameraTransform.localPosition = Vector3.Lerp(startCameraPosition, endCameraPosition, t);
            yield return null;
        }

        // Keep both body yaw and camera pitch when the movement ends.
        controller.TeleportTo(destination, false);
        ApplyCameraOffset(willBeSeated ? 1f : 0f);
        isSeated = willBeSeated;
        isTransitioning = false;
        transitionRoutine = null;
        interactor?.UnlockInteraction(this);
        if (willBeSeated) controller.LockMovement(this, false);
        else controller.UnlockMovement(this);
    }

    private void PlacePlayer(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        if (playerBody != null)
        {
            playerBody.position = position;
            playerBody.rotation = rotation;
        }
        Physics.SyncTransforms();
    }

    private void ApplyCameraOffset(float seatedAmount)
    {
        if (cameraTransform != null)
            cameraTransform.localPosition = standingCameraLocalPosition +
                seatedCameraOffset * seatedAmount;
    }

    private void CancelTransition()
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = null;
        isTransitioning = false;
        interactor?.UnlockInteraction(this);
    }

    private void OnValidate()
    {
        transitionDuration = Mathf.Max(0f, transitionDuration);
    }
}
