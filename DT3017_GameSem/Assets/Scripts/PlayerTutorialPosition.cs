using UnityEngine;
using UnityEngine.Events;

/// <summary>Place the player at a tutorial marker and hold them until released.</summary>
[DisallowMultipleComponent]
public class PlayerTutorialPosition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonController playerController;
    [Tooltip("World-space marker at the player's root position, not eye height.")]
    [SerializeField] private Transform tutorialPoint;

    [Header("Tutorial")]
    [SerializeField] private bool beginOnStart;
    [Tooltip("Match the marker's yaw and pitch. Roll is ignored to keep the player upright.")]
    [SerializeField] private bool matchFacing = true;
    [Tooltip("Also freeze mouse look. Turn off to let the player look around while standing still.")]
    [SerializeField] private bool blockLook = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onTutorialStarted = new UnityEvent();
    [SerializeField] private UnityEvent onTutorialEnded = new UnityEvent();

    private FirstPersonController lockedController;
    public bool IsActive => lockedController != null;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<FirstPersonController>();
    }

    private void Start()
    {
        if (beginOnStart)
            BeginTutorial();
    }

    public void BeginTutorial()
    {
        BeginTutorialAt(tutorialPoint);
    }

    // Accepts a marker directly from code or a UnityEvent Transform argument.
    public void BeginTutorialAt(Transform point)
    {
        if (!isActiveAndEnabled)
            return;

        if (playerController == null)
            playerController = GetComponent<FirstPersonController>();
        if (playerController == null || point == null)
        {
            Debug.LogError("PlayerTutorialPosition requires a Player Controller and Tutorial Point.", this);
            return;
        }

        bool wasActive = IsActive;
        if (lockedController != null && lockedController != playerController)
            lockedController.UnlockMovement(this);

        lockedController = playerController;
        lockedController.LockMovement(this, blockLook);
        lockedController.TeleportTo(point, matchFacing);
        if (!wasActive)
            onTutorialStarted.Invoke();
    }

    public void EndTutorial()
    {
        if (lockedController == null)
            return;

        lockedController.UnlockMovement(this);
        lockedController = null;
        onTutorialEnded.Invoke();
    }

    private void OnDisable()
    {
        // Removing the component or unloading its scene cannot strand the player.
        EndTutorial();
    }
}