using UnityEngine;

public class CameraMouseLook : MonoBehaviour
{
    [Header("Rotation range (degrees)")]
    public float maxYaw = 6f;    // left/right
    public float maxPitch = 4f;  // up/down

    [Header("Smoothing")]
    public float smoothSpeed = 4f;

    private Quaternion startRotation;

    void Start()
    {
        startRotation = transform.localRotation;
    }

    void Update()
    {
        // Mouse position as -1 to 1 (center of screen = 0)
        float x = (Input.mousePosition.x / Screen.width - 0.5f) * 2f;
        float y = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;

        x = Mathf.Clamp(x, -1f, 1f);
        y = Mathf.Clamp(y, -1f, 1f);

        // Mouse right -> look right, mouse up -> look up
        Quaternion offset = Quaternion.Euler(-y * maxPitch, x * maxYaw, 0f);
        Quaternion target = startRotation * offset;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, target, Time.deltaTime * smoothSpeed);
    }
}
