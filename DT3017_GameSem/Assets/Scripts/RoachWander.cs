using UnityEngine;

public class RoachWander : MonoBehaviour
{
    [Header("Wander area (centered on start position)")]
    public Vector2 areaSize = new Vector2(4f, 4f);

    [Header("Movement")]
    public Vector2 speedRange = new Vector2(0.2f, 0.8f);
    public float turnSpeed = 360f;       // degrees per second
    public float modelYawOffset = 0f;    // fix if the model faces sideways

    [Header("Pauses")]
    [Range(0f, 1f)] public float pauseChance = 0.3f;
    public Vector2 pauseTimeRange = new Vector2(0.3f, 2f);

    [Header("Optional")]
    public Animator animator;            // for a walk animation, if the FBX has one

    private Vector3 areaCenter;
    private Vector3 target;
    private float speed;
    private float pauseTimer;

    void Start()
    {
        areaCenter = transform.position;
        PickNewTarget();
    }

    void Update()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            if (animator) animator.speed = 0f;
            return;
        }
        if (animator) animator.speed = 1f;

        Vector3 dir = target - transform.position;
        dir.y = 0f;

        if (dir.magnitude < 0.05f)
        {
            if (Random.value < pauseChance)
                pauseTimer = Random.Range(pauseTimeRange.x, pauseTimeRange.y);
            PickNewTarget();
            return;
        }

        // Turn toward the target
        Quaternion look = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelYawOffset, 0f);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, look, turnSpeed * Time.deltaTime);

        // Only walk once roughly facing the target, so it turns first
        if (Quaternion.Angle(transform.rotation, look) < 30f)
            transform.position += dir.normalized * speed * Time.deltaTime;
    }

    void PickNewTarget()
    {
        target = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.y / 2f, areaSize.y / 2f));
        speed = Random.Range(speedRange.x, speedRange.y);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = Application.isPlaying ? areaCenter : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(c, new Vector3(areaSize.x, 0.05f, areaSize.y));
    }
}