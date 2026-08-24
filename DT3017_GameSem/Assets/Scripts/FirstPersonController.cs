using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    
    void FixedUpdate()
    {
        if (canMove)
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

        if (canMove)
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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void EnableController()
    {
        canMove = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }
}
