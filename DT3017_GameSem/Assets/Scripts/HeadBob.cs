using System;
using UnityEngine;

public class HeadBob : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's Rigidbody used to detect movement speed.")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Head Bob")]
    [Tooltip("How fast the camera completes each bob cycle.")]
    [Range(0.1f, 20f)]
    [SerializeField] private float bobFrequency = 8f;

    [Tooltip("Maximum vertical movement of the camera in local space.")]
    [Range(0f, 0.2f)]
    [SerializeField] private float verticalAmplitude = 0.04f;

    [Tooltip("Maximum horizontal movement of the camera in local space.")]
    [Range(0f, 0.2f)]
    [SerializeField] private float horizontalAmplitude = 0.025f;

    [Tooltip("Minimum horizontal speed required before head bobbing begins.")]
    [Range(0f, 2f)]
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera follows the calculated bob position.")]
    [Range(1f, 30f)]
    [SerializeField] private float bobSmoothing = 12f;

    [Tooltip("How quickly the camera returns to its starting position when movement stops.")]
    [Range(1f, 30f)]
    [SerializeField] private float resetSmoothing = 10f;

    /*[Header("Footsteps")]
    [Tooltip("Invoked once per step. You can subscribe to this later for footstep audio.")]*/
    public event Action OnStep;

    private Vector3 startLocalPosition;
    private float bobTimer;
    private bool stepTriggered;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponentInParent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (playerRigidbody == null)
            return;

        Vector3 velocity = playerRigidbody.linearVelocity;

        // Ignore vertical velocity so falling doesn't cause head bob.
        Vector3 horizontalVelocity = new Vector3(
            velocity.x,
            0f,
            velocity.z
        );

        float speed = horizontalVelocity.magnitude;
        bool isMoving = speed > movementThreshold;

        if (isMoving)
        {
            ApplyBob(speed);
        }
        else
        {
            ResetBob();
        }
    }

    private void ApplyBob(float speed)
    {
        bobTimer += Time.deltaTime * bobFrequency;

        float horizontalBob = Mathf.Cos(bobTimer * 0.5f) * horizontalAmplitude;
        float verticalBob = Mathf.Sin(bobTimer) * verticalAmplitude;

        Vector3 targetPosition = startLocalPosition +
            new Vector3(horizontalBob, verticalBob, 0f);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            bobSmoothing * Time.deltaTime
        );

        HandleStepEvent();
    }

    private void ResetBob()
    {
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            startLocalPosition,
            resetSmoothing * Time.deltaTime
        );

        bobTimer = 0f;
        stepTriggered = false;
    }

    private void HandleStepEvent()
    {
        float cycle = Mathf.Sin(bobTimer);

        // Trigger near the lowest point of the vertical bob.
        if (cycle < -0.9f)
        {
            if (!stepTriggered)
            {
                stepTriggered = true;
                OnStep?.Invoke();
            }
        }
        else
        {
            stepTriggered = false;
        }
    }
}
