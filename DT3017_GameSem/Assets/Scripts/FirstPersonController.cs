using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using DialogueEditor;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    private Rigidbody rb;

    [Header("Jump")]
    public KeyCode jumpKey = KeyCode.Space;
    [Min(0f)] public float jumpHeight = 1.25f;
    [Tooltip("Layers that can support the player. Triggers and the player's own colliders are ignored.")]
    public LayerMask groundLayers = Physics.DefaultRaycastLayers;
    [Min(0.01f)] public float groundCheckDistance = 0.1f;
    private CapsuleCollider playerCapsule;
    private bool jumpQueued;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];
    private readonly RaycastHit[] movementHits = new RaycastHit[16];
    public bool IsGrounded => CheckGrounded();

    [Header("Sprint")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    [Min(1f)] public float sprintSpeedMultiplier = 1.2f;
    [Tooltip("Extra camera FOV in degrees while moving and holding the sprint key. Set to zero to disable the effect.")]
    [Min(0f)] public float sprintFovIncrease = 5f;
    [Tooltip("FOV transition speed in degrees per second. Zero applies the change instantly.")]
    [Min(0f)] public float sprintFovTransitionSpeed = 30f;
    public bool IsSprinting { get; private set; }
    private Camera playerCamera;
    private float normalFieldOfView;

    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 3f;
    public float maxLookAngle = 85f;
    private float rotationX = 0f;

    private bool canMove = true;
    private RigidbodyConstraints unlockedConstraints;
    private readonly Dictionary<object, bool> movementLocks = new Dictionary<object, bool>();

    public bool CanMove => canMove && movementLocks.Count == 0;
    public bool CanLook => canMove && !movementLocks.ContainsValue(true);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCapsule = GetComponent<CapsuleCollider>();
        unlockedConstraints = rb.constraints;
        playerCamera = cameraTransform != null
            ? cameraTransform.GetComponentInChildren<Camera>(true)
            : GetComponentInChildren<Camera>(true);
        if (playerCamera != null) normalFieldOfView = playerCamera.fieldOfView;
    }

    private void OnEnable()
    {
        ConversationManager.OnConversationStarted += DisableController;
        ConversationManager.OnConversationEnded += EnableController;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationStarted -= DisableController;
        ConversationManager.OnConversationEnded -= EnableController;
        ResetSprint();
        jumpQueued = false;
    }

    void Start()
    {
        
        ApplyControlState();
    }

    void FixedUpdate()
    {
        if (CanMove)
        {
            if (jumpQueued) TryJump();
            Move();
        }
        jumpQueued = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (canMove)
            {
                DisableController();
            }
            else
            {
                EnableController();
            }
        }

        if (CanMove && Input.GetKeyDown(jumpKey)) jumpQueued = true;

        if (CanLook)
        {
            Look();
        }
    }
    
    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        ApplyMovement(new Vector2(h, v), Input.GetKey(sprintKey));
    }

    private void ApplyMovement(Vector2 input, bool sprintHeld)
    {
        IsSprinting = CanMove && sprintHeld && input.sqrMagnitude > 0f;
        Vector3 inputDir = (transform.forward * input.y + transform.right * input.x).normalized;
        float speed = walkSpeed * (IsSprinting ? Mathf.Max(1f, sprintSpeedMultiplier) : 1f);
        Vector3 moveVelocity = CanMove ? inputDir * speed : Vector3.zero;
        if (!IsGrounded) moveVelocity = SlideAlongAirborneObstacles(moveVelocity);
        Vector3 currentVelocity = rb.linearVelocity;

        rb.linearVelocity = new Vector3(moveVelocity.x, currentVelocity.y, moveVelocity.z);
    }

    private Vector3 SlideAlongAirborneObstacles(Vector3 velocity)
    {
        if (playerCapsule == null || velocity.sqrMagnitude < 0.0001f) return velocity;
        Bounds bounds = playerCapsule.bounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z);
        float halfSegment = Mathf.Max(0f, bounds.extents.y - radius);
        const float skin = 0.02f;
        Vector3 direction = velocity.normalized;
        // Sweep the body before driving it into an edge. Otherwise the collision
        // solver can turn horizontal movement into additional upward velocity.
        int count = Physics.CapsuleCastNonAlloc(bounds.center + Vector3.up * halfSegment,
            bounds.center - Vector3.up * halfSegment, Mathf.Max(0.01f, radius - skin),
            direction, movementHits, velocity.magnitude * Time.fixedDeltaTime + skin * 2f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = movementHits[i];
            if (hit.collider.attachedRigidbody == rb ||
                Physics.GetIgnoreLayerCollision(gameObject.layer, hit.collider.gameObject.layer) ||
                Physics.GetIgnoreCollision(playerCapsule, hit.collider)) continue;
            Vector3 sideNormal = new Vector3(hit.normal.x, 0f, hit.normal.z);
            if (sideNormal.sqrMagnitude < 0.0001f) continue;
            sideNormal.Normalize();
            float inwardSpeed = Vector3.Dot(velocity, sideNormal);
            if (inwardSpeed < 0f) velocity -= sideNormal * inwardSpeed;
        }
        return velocity;
    }

    private bool CheckGrounded()
    {
        if (playerCapsule == null || !playerCapsule.enabled || rb == null ||
            rb.linearVelocity.y > 0.1f) return false;

        Bounds bounds = playerCapsule.bounds;
        float radius = Mathf.Max(0.01f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f);
        // Start just above the feet; a small sphere also supports the player near ledges.
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius + 0.05f, bounds.center.z);
        int hits = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits,
            Mathf.Max(0.01f, groundCheckDistance) + 0.05f, groundLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits; i++)
        {
            if (groundHits[i].collider.attachedRigidbody != rb && groundHits[i].normal.y >= 0.65f)
                return true;
        }
        return false;
    }

    private bool TryJump()
    {
        if (!CanMove || rb.isKinematic || !rb.useGravity || jumpHeight <= 0f ||
            Physics.gravity.y >= 0f || !IsGrounded) return false;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight);
        rb.linearVelocity = velocity;
        return true;
    }

    private void LateUpdate()
    {
        UpdateSprintFov(Time.deltaTime);
    }

    private void UpdateSprintFov(float deltaTime)
    {
        if (playerCamera == null) return;
        float target = Mathf.Clamp(normalFieldOfView +
            (IsSprinting && CanMove ? Mathf.Max(0f, sprintFovIncrease) : 0f), 1f, 179f);
        playerCamera.fieldOfView = sprintFovTransitionSpeed <= 0f ? target :
            Mathf.MoveTowards(playerCamera.fieldOfView, target, sprintFovTransitionSpeed * deltaTime);
    }

    private void ResetSprint()
    {
        IsSprinting = false;
        if (playerCamera != null) playerCamera.fieldOfView = normalFieldOfView;
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mouseX, 0f);

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    public void DisableController()
    {
        canMove = false;
        ApplyControlState();
    }

    public void EnableController()
    {
        canMove = true;
        ApplyControlState();
    }

    // Owners release only their own lock; dialogue, crafting, and Escape cannot
    // accidentally remove a tutorial lock. Repeated requests update that lock.
    public void LockMovement(object owner, bool blockLook = true)
    {
        if (owner == null)
            throw new System.ArgumentNullException(nameof(owner));

        movementLocks[owner] = blockLook;
        ApplyControlState();
    }

    public void UnlockMovement(object owner)
    {
        if (owner != null && movementLocks.Remove(owner))
            ApplyControlState();
    }

    public void TeleportTo(Transform destination, bool matchFacing = true)
    {
        if (destination == null)
            throw new System.ArgumentNullException(nameof(destination));

        StopMotion();
        Quaternion facing = matchFacing
            ? Quaternion.Euler(0f, destination.eulerAngles.y, 0f)
            : transform.rotation;
        transform.SetPositionAndRotation(destination.position, facing);
        rb.position = destination.position;
        rb.rotation = facing;

        if (matchFacing)
        {
            rotationX = Mathf.Clamp(
                Mathf.DeltaAngle(0f, destination.eulerAngles.x),
                -maxLookAngle, maxLookAngle);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }

        Physics.SyncTransforms();
    }

    private void ApplyControlState()
    {
        if (rb == null)
            return;

        if (!CanMove)
            StopMotion();
        rb.constraints = CanMove ? unlockedConstraints : RigidbodyConstraints.FreezeAll;
        Cursor.lockState = CanLook ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !CanLook;
    }

    private void StopMotion()
    {
        ResetSprint();
        jumpQueued = false;
        if (rb == null || rb.isKinematic)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void ResetLook()
    {
        rotationX = 0f;

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
        }
    }
}
