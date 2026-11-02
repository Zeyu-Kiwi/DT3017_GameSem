using UnityEngine;

[DisallowMultipleComponent]
public sealed class ItemSpawnZone : MonoBehaviour
{
    [Tooltip("Stable identifier used for seeded rolls and spawn history. Keep IDs unique within a scene.")]
    [SerializeField] private string zoneId;
    [Tooltip("Leave empty to use the daily spawner's default table.")]
    [SerializeField] private DailyItemSpawnTable tableOverride;
    [SerializeField, Min(0)] private int minimumItems = 1;
    [SerializeField, Min(0)] private int maximumItems = 3;

    [Header("Area (local space)")]
    [Tooltip("Keep the zone upright. Items are placed on upward-facing surfaces inside this box.")]
    [SerializeField] private Vector3 center = Vector3.zero;
    [SerializeField] private Vector3 size = new Vector3(3f, 2f, 3f);
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private LayerMask blockingLayers = ~0;
    [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.7f;
    [SerializeField, Min(1)] private int placementAttempts = 20;

    public string ZoneId => string.IsNullOrWhiteSpace(zoneId) ? name : zoneId;
    public DailyItemSpawnTable TableOverride => tableOverride;
    public int PlacementAttempts => Mathf.Max(1, placementAttempts);
    public LayerMask BlockingLayers => blockingLayers;
    public int RollCount(System.Random random) => random.Next(Mathf.Max(0, minimumItems), Mathf.Max(Mathf.Max(0, minimumItems), maximumItems) + 1);

    public bool TryFindSurface(System.Random random, out Vector3 point)
    {
        float x = ((float)random.NextDouble() - 0.5f) * Mathf.Abs(size.x);
        float z = ((float)random.NextDouble() - 0.5f) * Mathf.Abs(size.z);
        Vector3 origin = transform.TransformPoint(center + new Vector3(x, Mathf.Abs(size.y) * 0.5f, z));
        float depth = Mathf.Abs(size.y * transform.lossyScale.y);
        var hits = Physics.RaycastAll(origin, Vector3.down, depth, groundLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.normal.y < minimumGroundNormalY || hit.collider.GetComponentInParent<ItemPickup>() != null ||
                hit.collider.GetComponentInParent<FirstPersonController>() != null) continue;
            point = hit.point;
            return true;
        }
        point = default;
        return false;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(zoneId)) zoneId = System.Guid.NewGuid().ToString("N");
        minimumItems = Mathf.Clamp(minimumItems, 0, 1000);
        maximumItems = Mathf.Clamp(maximumItems, minimumItems, 1000);
        size = new Vector3(Mathf.Max(0.1f, Mathf.Abs(size.x)), Mathf.Max(0.1f, Mathf.Abs(size.y)), Mathf.Max(0.1f, Mathf.Abs(size.z)));
        placementAttempts = Mathf.Max(1, placementAttempts);
    }

    private void OnDrawGizmos()
    {
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.85f, 0.65f, 0.7f);
        Gizmos.DrawWireCube(center, size);
        Gizmos.matrix = previous;
    }
}