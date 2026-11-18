using UnityEngine;

public class RoachWander : MonoBehaviour
{
    [Header("Wander area (centered on start position)")]
    public Vector2 areaSize = new Vector2(4f, 4f);

    [Header("Movement")]
    public Vector2 speedRange = new Vector2(0.2f, 0.8f);
    public float turnSpeed = 360f;       // degrees per second
    public float modelYawOffset = 0f;    // fix if the model faces sideways

    [Header("Visual crawl wobble")]
    [Min(0f)] public float maxWobbleAngle = 5f;
    [Min(0f)] public float wobbleCyclesPerSecond = 2.5f;

    [Header("Pauses")]
    [Range(0f, 1f)] public float pauseChance = 0.3f;
    public Vector2 pauseTimeRange = new Vector2(0.3f, 2f);

    [Header("Optional")]
    public Animator animator;            // for a walk animation, if the FBX has one

    private Vector3 areaCenter;
    private Vector3 target;
    private float speed;
    private float pauseTimer;
    private Quaternion travelRotation;
    private float wobblePhase;
    private float wobbleAmplitude;
    private bool returningToArea;

    void Start()
    {
        areaCenter = transform.position;
        ResetMovement();
        PickNewTarget();
    }

    // Called after the roach is moved to its off-screen spawn point.
    // The wander area remains centered on its original scene position.
    public void ReturnToWanderArea()
    {
        ResetMovement();

        float halfWidth = Mathf.Max(0f, areaSize.x * 0.5f);
        float halfDepth = Mathf.Max(0f, areaSize.y * 0.5f);
        Vector3 position = transform.position;
        bool outside = position.x < areaCenter.x - halfWidth || position.x > areaCenter.x + halfWidth ||
                       position.z < areaCenter.z - halfDepth || position.z > areaCenter.z + halfDepth;

        if (!outside)
        {
            returningToArea = false;
            PickNewTarget();
            return;
        }

        // The nearest point just inside the rectangle makes the roach crawl in
        // through the closest edge (or corner) before normal wandering resumes.
        float insetX = Mathf.Min(0.05f, halfWidth);
        float insetZ = Mathf.Min(0.05f, halfDepth);
        target = new Vector3(
            Mathf.Clamp(position.x, areaCenter.x - halfWidth + insetX, areaCenter.x + halfWidth - insetX),
            position.y,
            Mathf.Clamp(position.z, areaCenter.z - halfDepth + insetZ, areaCenter.z + halfDepth - insetZ));
        speed = Random.Range(speedRange.x, speedRange.y);
        wobbleAmplitude = Random.Range(0.6f, 1f) * maxWobbleAngle;
        returningToArea = true;
    }

    private void ResetMovement()
    {
        travelRotation = transform.rotation;
        pauseTimer = 0f;
        wobblePhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            if (animator) animator.speed = 0f;
            transform.rotation = travelRotation;
            return;
        }
        if (animator) animator.speed = 1f;

        Vector3 dir = target - transform.position;
        dir.y = 0f;

        if (dir.magnitude < 0.05f)
        {
            if (!returningToArea && Random.value < pauseChance)
                pauseTimer = Random.Range(pauseTimeRange.x, pauseTimeRange.y);
            returningToArea = false;
            PickNewTarget();
            transform.rotation = travelRotation;
            return;
        }

        // Turn toward the target
        Quaternion look = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelYawOffset, 0f);
        travelRotation = Quaternion.RotateTowards(
            travelRotation, look, turnSpeed * Time.deltaTime);

        // Only walk once roughly facing the target, so it turns first
        if (Quaternion.Angle(travelRotation, look) < 30f)
        {
            transform.position += dir.normalized * speed * Time.deltaTime;
            float wobble = Mathf.Sin(Time.time * wobbleCyclesPerSecond * Mathf.PI * 2f + wobblePhase) * wobbleAmplitude;
            transform.rotation = travelRotation * Quaternion.Euler(0f, wobble, 0f);
        }
        else
        {
            transform.rotation = travelRotation;
        }
    }

    void PickNewTarget()
    {
        target = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.y / 2f, areaSize.y / 2f));
        speed = Random.Range(speedRange.x, speedRange.y);
        wobbleAmplitude = Random.Range(0.6f, 1f) * maxWobbleAngle;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = Application.isPlaying ? areaCenter : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(c, new Vector3(areaSize.x, 0.05f, areaSize.y));
    }
}
