using UnityEngine;

[DisallowMultipleComponent]
public sealed class VentScrew : MonoBehaviour
{
    [SerializeField, Min(0.25f)] private float turnsToRemove = 3f;
    [SerializeField, Min(0f)] private float extractionDistance = 0.08f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 removalAxis;
    private float turns;
    public bool IsRemoved { get; private set; }
    public float Progress => Mathf.Clamp01(turns / Mathf.Max(0.25f, turnsToRemove));

    private void Awake()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        removalAxis = initialRotation * Vector3.forward;
    }

    public bool Turn(float degrees)
    {
        if (IsRemoved) return false;
        turns = Mathf.Clamp(turns + degrees / 360f, 0f, Mathf.Max(0.25f, turnsToRemove));
        transform.localPosition = initialPosition + removalAxis * (extractionDistance * Progress);
        transform.localRotation = initialRotation * Quaternion.AngleAxis(turns * 360f, Vector3.forward);
        if (Progress < 1f) return false;
        IsRemoved = true;
        return true;
    }

    public void Drop()
    {
        transform.SetParent(null, true);
        var body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.mass = 0.02f;
        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.AddForce(transform.forward * 0.25f, ForceMode.VelocityChange);
        body.AddTorque(transform.right * 3f, ForceMode.VelocityChange);
        Destroy(gameObject, 12f);
    }
}