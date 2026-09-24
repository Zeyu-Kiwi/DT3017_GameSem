using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    private enum BossState
    {
        WaitingOutside,
        Entering,
        Roaming,
        Pausing,
        Inspecting,
        Leaving,
        MovingOutside
    }

    [Header("Movement Objects")]
    [SerializeField] private Transform movementRoot;
    [Tooltip("A child containing the boss model/colliders. It is hidden while the boss waits outside.")]
    [SerializeField] private GameObject bodyRoot;
    [SerializeField] private Transform outsidePoint;

    [Header("Locator Network")]
    [SerializeField] private BossLocatorNetwork locatorNetwork;
    [SerializeField] private BossPatrolLocator entranceLocator;
    [SerializeField] private BossPatrolLocator exitLocator;

    [Header("Visit Timing")]
    [SerializeField, Min(0f)] private float minimumEntryInterval = 20f;
    [SerializeField, Min(0f)] private float maximumEntryInterval = 45f;
    [SerializeField, Min(1)] private int minimumRoamingLocators = 4;
    [SerializeField, Min(1)] private int maximumRoamingLocators = 8;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float movementSpeed = 2f;
    [SerializeField, Min(0.01f)] private float turningSpeed = 180f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.08f;
    [SerializeField, Min(0f)] private float locatorPauseDuration = 0.4f;

    [Header("Scripted Inspection")]
    [SerializeField, Min(1)] private int inspectEveryLocators = 3;
    [SerializeField, Range(0f, 180f)] private float inspectionAngle = 55f;
    [SerializeField, Min(0.01f)] private float inspectionDuration = 2.5f;

    [Header("Detection")]
    [SerializeField] private BossVision vision;
    [SerializeField] private Transform playerVisionTarget;
    [SerializeField] private Transform workstationVisionTarget;
    [SerializeField] private CraftingStation workstation;
    [SerializeField] private CaughtSequenceController caughtSequence;

    [Header("Optional Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingBoolParameter = "IsWalking";

    [Header("Runtime State (Debug)")]
    [SerializeField] private BossState state;
    [SerializeField] private bool sequencePaused;
    [SerializeField] private bool isInRoom;
    [SerializeField] private int visitedRoamingLocators;
    [SerializeField] private int targetRoamingLocatorCount;

    private readonly Queue<BossPatrolLocator> exitRoute =
        new Queue<BossPatrolLocator>();

    private BossPatrolLocator currentLocator;
    private BossPatrolLocator previousLocator;
    private BossPatrolLocator targetLocator;
    private Vector3 targetPosition;
    private float stateTimer;
    private float inspectionElapsed;
    private Quaternion inspectionStartRotation;
    private bool caughtThisVisit;

    public bool IsInRoom => isInRoom;
    public bool IsSequencePaused => sequencePaused;

    private void Awake()
    {
        if (movementRoot == null)
        {
            movementRoot = transform;
        }
    }

    private void Start()
    {
        if (locatorNetwork != null)
        {
            locatorNetwork.BuildConnections();
        }

        ResetOutsideAndRestartTimer();
    }

    private void Update()
    {
        if (sequencePaused)
        {
            return;
        }

        if (isInRoom && !caughtThisVisit)
        {
            CheckForCaughtPlayer();
        }

        switch (state)
        {
            case BossState.WaitingOutside:
                UpdateWaitingOutside();
                break;

            case BossState.Entering:
            case BossState.Roaming:
            case BossState.Leaving:
            case BossState.MovingOutside:
                UpdateMovement();
                break;

            case BossState.Pausing:
                UpdatePause();
                break;

            case BossState.Inspecting:
                UpdateInspection();
                break;
        }
    }

    public void SetSequencePaused(bool paused)
    {
        sequencePaused = paused;
        SetWalking(!paused && IsMovementState(state));
    }

    public void ResetOutsideAndRestartTimer()
    {
        exitRoute.Clear();
        currentLocator = null;
        previousLocator = null;
        targetLocator = null;
        visitedRoamingLocators = 0;
        targetRoamingLocatorCount = 0;
        caughtThisVisit = false;
        isInRoom = false;
        state = BossState.WaitingOutside;
        stateTimer = Random.Range(
            Mathf.Min(minimumEntryInterval, maximumEntryInterval),
            Mathf.Max(minimumEntryInterval, maximumEntryInterval));

        if (movementRoot != null && outsidePoint != null)
        {
            movementRoot.SetPositionAndRotation(
                outsidePoint.position,
                outsidePoint.rotation);
        }

        SetWalking(false);
        SetBodyVisible(false);
    }

    [ContextMenu("Start Visit Now")]
    public void StartVisitNow()
    {
        if (sequencePaused)
        {
            return;
        }

        BeginVisit();
    }

    private void UpdateWaitingOutside()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            BeginVisit();
        }
    }

    private void BeginVisit()
    {
        if (entranceLocator == null || exitLocator == null || outsidePoint == null)
        {
            Debug.LogError(
                "BossController needs Outside, Entrance and Exit references.",
                this);
            ResetOutsideAndRestartTimer();
            return;
        }

        caughtThisVisit = false;
        visitedRoamingLocators = 0;
        targetRoamingLocatorCount = Random.Range(
            Mathf.Min(minimumRoamingLocators, maximumRoamingLocators),
            Mathf.Max(minimumRoamingLocators, maximumRoamingLocators) + 1);

        SetBodyVisible(true);
        SetMovementTarget(entranceLocator, BossState.Entering);
    }

    private void UpdateMovement()
    {
        if (movementRoot == null)
        {
            return;
        }

        Vector3 offset = targetPosition - movementRoot.position;
        Vector3 flatDirection = Vector3.ProjectOnPlane(offset, Vector3.up);

        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                flatDirection.normalized,
                Vector3.up);
            movementRoot.rotation = Quaternion.RotateTowards(
                movementRoot.rotation,
                targetRotation,
                turningSpeed * Time.deltaTime);
        }

        movementRoot.position = Vector3.MoveTowards(
            movementRoot.position,
            targetPosition,
            movementSpeed * Time.deltaTime);

        if (Vector3.Distance(movementRoot.position, targetPosition) <= arrivalDistance)
        {
            movementRoot.position = targetPosition;
            HandleArrival();
        }
    }

    private void HandleArrival()
    {
        SetWalking(false);

        if (state == BossState.Entering)
        {
            currentLocator = entranceLocator;
            previousLocator = null;
            isInRoom = true;
            ChooseNextRoamingLocator();
            return;
        }

        if (state == BossState.Roaming)
        {
            previousLocator = currentLocator;
            currentLocator = targetLocator;
            visitedRoamingLocators++;

            if (visitedRoamingLocators >= targetRoamingLocatorCount)
            {
                BeginLeaving();
            }
            else if (inspectEveryLocators > 0 &&
                     visitedRoamingLocators % inspectEveryLocators == 0)
            {
                BeginInspection();
            }
            else
            {
                BeginLocatorPause();
            }

            return;
        }

        if (state == BossState.Leaving)
        {
            previousLocator = currentLocator;
            currentLocator = targetLocator;

            if (exitRoute.Count > 0)
            {
                SetMovementTarget(exitRoute.Dequeue(), BossState.Leaving);
            }
            else
            {
                isInRoom = false;
                SetPointTarget(outsidePoint.position, BossState.MovingOutside);
            }

            return;
        }

        if (state == BossState.MovingOutside)
        {
            ResetOutsideAndRestartTimer();
        }
    }

    private void BeginLocatorPause()
    {
        state = BossState.Pausing;
        stateTimer = locatorPauseDuration;
        SetWalking(false);
    }

    private void UpdatePause()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChooseNextRoamingLocator();
        }
    }

    private void ChooseNextRoamingLocator()
    {
        if (locatorNetwork == null || currentLocator == null)
        {
            BeginLeaving();
            return;
        }

        BossPatrolLocator next = locatorNetwork.GetRandomRoamingNeighbour(
            currentLocator,
            previousLocator);

        if (next == null)
        {
            BeginLeaving();
            return;
        }

        SetMovementTarget(next, BossState.Roaming);
    }

    private void BeginInspection()
    {
        state = BossState.Inspecting;
        inspectionElapsed = 0f;
        inspectionStartRotation = movementRoot.rotation;
        SetWalking(false);
    }

    private void UpdateInspection()
    {
        inspectionElapsed += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(inspectionElapsed / inspectionDuration);
        float yawOffset = Mathf.Sin(normalizedTime * Mathf.PI * 2f) * inspectionAngle;
        movementRoot.rotation = inspectionStartRotation * Quaternion.Euler(0f, yawOffset, 0f);

        if (normalizedTime >= 1f)
        {
            movementRoot.rotation = inspectionStartRotation;
            BeginLocatorPause();
        }
    }

    private void BeginLeaving()
    {
        if (locatorNetwork == null || currentLocator == null || exitLocator == null)
        {
            ResetOutsideAndRestartTimer();
            return;
        }

        List<BossPatrolLocator> path = locatorNetwork.FindPath(
            currentLocator,
            exitLocator);

        if (path.Count == 0)
        {
            Debug.LogWarning(
                "No locator route to the boss exit was found. Boss was reset outside.",
                this);
            ResetOutsideAndRestartTimer();
            return;
        }

        exitRoute.Clear();
        for (int i = 1; i < path.Count; i++)
        {
            exitRoute.Enqueue(path[i]);
        }

        if (exitRoute.Count == 0)
        {
            isInRoom = false;
            SetPointTarget(outsidePoint.position, BossState.MovingOutside);
        }
        else
        {
            SetMovementTarget(exitRoute.Dequeue(), BossState.Leaving);
        }
    }

    private void SetMovementTarget(BossPatrolLocator locator, BossState movementState)
    {
        if (locator == null)
        {
            ResetOutsideAndRestartTimer();
            return;
        }

        targetLocator = locator;
        SetPointTarget(locator.transform.position, movementState);
    }

    private void SetPointTarget(Vector3 position, BossState movementState)
    {
        targetPosition = position;
        state = movementState;
        SetWalking(true);
    }

    private void CheckForCaughtPlayer()
    {
        if (vision == null || workstation == null)
        {
            return;
        }

        bool playerOutside = !workstation.IsPlayerInsideWorkstation;
        bool playerVisible = playerVisionTarget != null &&
                             vision.CanSee(playerVisionTarget);
        bool workstationVisible = workstationVisionTarget != null &&
                                  vision.CanSee(workstationVisionTarget);

        string reason = null;
        if (playerOutside && playerVisible)
        {
            reason = "Player was visible outside the workstation.";
        }
        else if (playerOutside && workstationVisible)
        {
            reason = "Boss saw that the workstation was empty.";
        }
        else if (!playerOutside && workstationVisible && workstation.IsDrawerOpen)
        {
            reason = "Boss saw the open workstation drawer.";
        }

        if (reason == null)
        {
            return;
        }

        caughtThisVisit = true;
        Debug.Log("Player caught: " + reason, this);

        if (caughtSequence != null)
        {
            caughtSequence.PlayCaughtSequence(this);
        }
        else
        {
            Debug.LogError("BossController has no CaughtSequenceController assigned.", this);
        }
    }

    private void SetBodyVisible(bool visible)
    {
        if (bodyRoot != null && bodyRoot != gameObject)
        {
            bodyRoot.SetActive(visible);
        }
    }

    private void SetWalking(bool walking)
    {
        if (animator != null && !string.IsNullOrEmpty(walkingBoolParameter))
        {
            animator.SetBool(walkingBoolParameter, walking);
        }
    }

    private static bool IsMovementState(BossState value)
    {
        return value == BossState.Entering ||
               value == BossState.Roaming ||
               value == BossState.Leaving ||
               value == BossState.MovingOutside;
    }

    private void OnValidate()
    {
        maximumEntryInterval = Mathf.Max(minimumEntryInterval, maximumEntryInterval);
        maximumRoamingLocators = Mathf.Max(
            minimumRoamingLocators,
            maximumRoamingLocators);
        inspectionDuration = Mathf.Max(0.01f, inspectionDuration);
    }
}
