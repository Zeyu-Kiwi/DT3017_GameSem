using UnityEngine;
using UnityEngine.Audio;

/// <summary>Menu-only waypoint movement and positional boss footsteps.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class MenuBossWalker : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0f)] private float turnSpeed = 360f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.1f;
    [SerializeField, Min(0f)] private float waitAtWaypoint = 0.5f;
    [Tooltip("Reverse at the ends. Disable to loop from the last waypoint back to the first.")]
    [SerializeField] private bool pingPong = true;
    [Tooltip("Use when the model's forward axis does not match Unity's +Z forward axis.")]
    [SerializeField] private float facingOffsetDegrees;

    [Header("Animation")]
    [Tooltip("Animator on the boss model. If empty, searches MenuBoss children at runtime.")]
    [SerializeField] private Animator animator;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] stepClips;
    [SerializeField, Min(0.05f)] private float stepInterval = 0.75f;
    [SerializeField, Range(0f, 1f)] private float stepVolume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private AudioMixerGroup bossFootstepsGroup;
    [Tooltip("Full volume up to this distance from the menu camera's AudioListener.")]
    [SerializeField, Min(0.01f)] private float minDistance = 5f;
    [Tooltip("Silent beyond this distance. Red gizmo shows the range.")]
    [SerializeField, Min(0.02f)] private float maxDistance = 17f;

    private AudioSource stepSource;
    private int waypointIndex;
    private int direction = 1;
    private float waitTimer;
    private float stepTimer;

    private void Awake()
    {
        stepSource = GetComponent<AudioSource>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        ApplyAudioSettings();
    }

    private void Start()
    {
        SetWalking(false);
        if (waypoints == null || waypoints.Length == 0) return;
        // A first waypoint at the boss's initial position marks the start of the route.
        if (waypoints[0] != null &&
            Vector3.Distance(transform.position, waypoints[0].position) <= arrivalDistance &&
            waypoints.Length > 1)
            waypointIndex = 1;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;
        if (waypoints == null || waypoints.Length == 0 || moveSpeed <= 0f)
        {
            SetWalking(false);
            return;
        }

        if (waitTimer > 0f)
        {
            SetWalking(false);
            waitTimer -= Time.deltaTime;
            stepTimer = 0f;
            return;
        }

        Transform waypoint = waypoints[waypointIndex];
        if (waypoint == null)
        {
            SetWalking(false);
            return;
        }

        Vector3 displacement = waypoint.position - transform.position;
        displacement.y = 0f;
        if (displacement.magnitude <= arrivalDistance)
        {
            SetWalking(false);
            AdvanceWaypoint();
            waitTimer = waitAtWaypoint;
            stepTimer = 0f;
            return;
        }

        Vector3 heading = displacement.normalized;
        SetWalking(true);
        transform.position = Vector3.MoveTowards(transform.position,
            new Vector3(waypoint.position.x, transform.position.y, waypoint.position.z),
            moveSpeed * Time.deltaTime);
        Quaternion facing = Quaternion.LookRotation(heading) * Quaternion.Euler(0f, facingOffsetDegrees, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = stepInterval;
        }
    }

    private void SetWalking(bool walking)
    {
        if (animator != null) animator.SetBool(IsWalkingHash, walking);
    }

    private void AdvanceWaypoint()
    {
        if (waypoints.Length < 2) return;
        if (pingPong)
        {
            if (waypointIndex + direction >= waypoints.Length || waypointIndex + direction < 0)
                direction = -direction;
            waypointIndex += direction;
        }
        else
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
        }
    }

    private void PlayFootstep()
    {
        if (stepClips == null || stepClips.Length == 0) return;
        AudioClip clip = stepClips[Random.Range(0, stepClips.Length)];
        if (clip == null) return;
        stepSource.pitch = Random.Range(Mathf.Min(pitchRange.x, pitchRange.y),
            Mathf.Max(pitchRange.x, pitchRange.y));
        stepSource.PlayOneShot(clip, stepVolume);
    }

    private void ApplyAudioSettings()
    {
        if (stepSource == null) return;
        stepSource.playOnAwake = false;
        stepSource.spatialBlend = 1f;
        stepSource.rolloffMode = AudioRolloffMode.Linear;
        stepSource.volume = 1f;
        stepSource.outputAudioMixerGroup = bossFootstepsGroup;
        stepSource.minDistance = minDistance;
        stepSource.maxDistance = maxDistance;
    }

    private void OnValidate()
    {
        maxDistance = Mathf.Max(minDistance + 0.01f, maxDistance);
        stepInterval = Mathf.Max(0.05f, stepInterval);
        ApplyAudioSettings();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        Vector3 from = transform.position;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.15f);
            Gizmos.DrawLine(from, waypoints[i].position);
            from = waypoints[i].position;
        }
    }
}
