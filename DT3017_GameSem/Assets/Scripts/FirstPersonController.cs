using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using DialogueEditor;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    private Rigidbody rb;

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
        unlockedConstraints = rb.constraints;
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
    }

    void Start()
    {
        
        ApplyControlState();
    }

    void FixedUpdate()
    {
        if (CanMove)
        {
            Move();
        }
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

        if (CanLook)
        {
            Look();
        }
    }
    
    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 inputDir = (transform.forward * v + transform.right * h).normalized;
        Vector3 moveVelocity = inputDir * walkSpeed;
        Vector3 currentVelocity = rb.linearVelocity;

        rb.linearVelocity = new Vector3(moveVelocity.x, currentVelocity.y, moveVelocity.z);
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
