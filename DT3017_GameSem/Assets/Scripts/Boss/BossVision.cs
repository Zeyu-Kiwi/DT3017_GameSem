using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BossVision : MonoBehaviour
{
    [Header("Vision")]
    [SerializeField] private Transform eye;
    [SerializeField, Min(0.1f)] private float viewDistance = 8f;
    [SerializeField, Range(1f, 179f)] private float horizontalViewAngle = 80f;
    [SerializeField, Range(1f, 179f)] private float verticalViewAngle = 50f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Nearby Detection")]
    [Tooltip("Boss transform that the detection cylinder follows.")]
    [SerializeField] private Transform proximityOrigin;
    [SerializeField] private Vector3 proximityCenterOffset = new Vector3(0f, 1f, 0f);
    [SerializeField, Min(0.1f)] private float proximityRadius = 1.5f;
    [SerializeField, Min(0.1f)] private float proximityHeight = 2.5f;
    [SerializeField] private Color proximityGizmoColor = new Color(1f, 0.5f, 0f, 0.9f);
    [Tooltip("Material for the cylinder shown to players. Uses the cone material when unassigned.")]
    [SerializeField] private Material proximityMaterial;

    [Header("Visible Cone")]
    [SerializeField, Range(4, 128)] private int horizontalSamples = 48;
    [SerializeField, Min(0.01f)] private float meshUpdateInterval = 0.08f;

    private MeshFilter meshFilter;
    private MeshRenderer coneRenderer;
    private Mesh viewMesh;
    private Mesh proximityMesh;
    private MeshRenderer proximityRenderer;
    private Transform proximityVisual;
    private float visualRadius;
    private float visualHeight;
    private float updateTimer;

    public float ViewDistance => viewDistance;
    public float HorizontalViewAngle => horizontalViewAngle;
    public float VerticalViewAngle => verticalViewAngle;

    private void Awake()
    {
        if (eye == null)
        {
            eye = transform;
        }

        meshFilter = GetComponent<MeshFilter>();
        coneRenderer = GetComponent<MeshRenderer>();
        viewMesh = new Mesh
        {
            name = "Boss Vision Cone"
        };
        viewMesh.MarkDynamic();
        meshFilter.sharedMesh = viewMesh;
        CreateProximityVisual();
    }

    private void OnEnable()
    {
        updateTimer = 0f;
    }

    private void Update()
    {
        UpdateProximityVisual();
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0f)
        {
            updateTimer = meshUpdateInterval;
            RebuildVisibleCone();
        }
    }

    public bool CanSee(Transform target)
    {
        return target != null && CanSeePoint(target.position);
    }

    public bool CanSee(BossVisionTargetArea targetArea)
    {
        if (targetArea == null)
        {
            return false;
        }

        for (int i = 0; i < targetArea.SampleCount; i++)
        {
            if (CanSeePoint(targetArea.GetSampleWorldPosition(i)))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanSeePoint(Vector3 targetPosition)
    {
        if (eye == null)
        {
            return false;
        }

        Vector3 toTarget = targetPosition - eye.position;
        float distance = toTarget.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            return false;
        }

        if (!IsInsideCone(toTarget, distance) && !IsInsideProximity(targetPosition))
        {
            return false;
        }

        return !Physics.Raycast(
            eye.position,
            toTarget / distance,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);
    }

    private bool IsInsideCone(Vector3 toTarget, float distance)
    {
        if (distance > viewDistance) return false;

        Vector3 localDirection = eye.InverseTransformDirection(toTarget / distance);
        if (localDirection.z <= 0f) return false;

        float horizontalAngle = Mathf.Abs(
            Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg);
        float horizontalDistance = new Vector2(localDirection.x, localDirection.z).magnitude;
        float verticalAngle = Mathf.Abs(
            Mathf.Atan2(localDirection.y, horizontalDistance) * Mathf.Rad2Deg);

        return horizontalAngle <= horizontalViewAngle * 0.5f &&
               verticalAngle <= verticalViewAngle * 0.5f;
    }

    private bool IsInsideProximity(Vector3 targetPosition)
    {
        if (proximityOrigin == null) return false;

        Vector3 center = GetProximityCenter();
        Vector3 offset = targetPosition - center;
        return Mathf.Abs(offset.y) <= proximityHeight * 0.5f &&
               offset.x * offset.x + offset.z * offset.z <=
               proximityRadius * proximityRadius;
    }

    private Vector3 GetProximityCenter()
    {
        return proximityOrigin.position +
               proximityOrigin.TransformDirection(proximityCenterOffset);
    }

    private void CreateProximityVisual()
    {
        GameObject visual = new GameObject("Nearby Detection Visual");
        visual.layer = gameObject.layer;
        proximityVisual = visual.transform;
        proximityVisual.SetParent(transform, false);

        MeshFilter filter = visual.AddComponent<MeshFilter>();
        proximityRenderer = visual.AddComponent<MeshRenderer>();
        proximityRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        proximityRenderer.receiveShadows = false;
        proximityRenderer.sharedMaterial = proximityMaterial != null
            ? proximityMaterial
            : coneRenderer.sharedMaterial;

        proximityMesh = new Mesh { name = "Boss Nearby Detection Cylinder" };
        filter.sharedMesh = proximityMesh;
        RebuildProximityMesh();
        UpdateProximityVisual();
    }

    private void UpdateProximityVisual()
    {
        if (proximityVisual == null) return;

        bool hasOrigin = proximityOrigin != null;
        if (proximityVisual.gameObject.activeSelf != hasOrigin)
            proximityVisual.gameObject.SetActive(hasOrigin);
        if (!hasOrigin) return;

        proximityVisual.position = GetProximityCenter();
        proximityVisual.rotation = Quaternion.identity;

        if (!Mathf.Approximately(visualRadius, proximityRadius) ||
            !Mathf.Approximately(visualHeight, proximityHeight))
            RebuildProximityMesh();

        Material material = proximityMaterial != null
            ? proximityMaterial
            : coneRenderer.sharedMaterial;
        if (proximityRenderer.sharedMaterial != material)
            proximityRenderer.sharedMaterial = material;
    }

    private void RebuildProximityMesh()
    {
        if (proximityMesh == null) return;

        const int segments = 32;
        float halfHeight = proximityHeight * 0.5f;
        List<Vector3> vertices = new List<Vector3>(2 + segments * 2)
        {
            Vector3.up * halfHeight,
            Vector3.down * halfHeight
        };
        List<int> triangles = new List<int>(segments * 12);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 edge = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * proximityRadius;
            vertices.Add(edge + Vector3.up * halfHeight);
            vertices.Add(edge - Vector3.up * halfHeight);
        }

        for (int i = 0; i < segments; i++)
        {
            int top = 2 + i * 2;
            int bottom = top + 1;
            int nextTop = 2 + ((i + 1) % segments) * 2;
            int nextBottom = nextTop + 1;
            AddTriangle(triangles, bottom, top, nextBottom);
            AddTriangle(triangles, top, nextTop, nextBottom);
            AddTriangle(triangles, 0, nextTop, top);
            AddTriangle(triangles, 1, bottom, nextBottom);
        }

        proximityMesh.Clear();
        proximityMesh.SetVertices(vertices);
        proximityMesh.SetTriangles(triangles, 0);
        proximityMesh.RecalculateNormals();
        proximityMesh.RecalculateBounds();
        visualRadius = proximityRadius;
        visualHeight = proximityHeight;
    }

    private void RebuildVisibleCone()
    {
        if (viewMesh == null || eye == null)
        {
            return;
        }

        int sampleCount = Mathf.Max(4, horizontalSamples);
        List<Vector3> vertices = new List<Vector3>(2 + (sampleCount + 1) * 2);
        List<int> triangles = new List<int>(sampleCount * 18);

        Vector3 localOrigin = transform.InverseTransformPoint(eye.position);
        vertices.Add(localOrigin);
        vertices.Add(localOrigin);

        float halfHorizontalAngle = horizontalViewAngle * 0.5f;
        float verticalTangent = Mathf.Tan(verticalViewAngle * 0.5f * Mathf.Deg2Rad);

        for (int i = 0; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float angle = Mathf.Lerp(-halfHorizontalAngle, halfHorizontalAngle, t);
            Vector3 worldDirection =
                Quaternion.AngleAxis(angle, eye.up) * eye.forward;

            float distance = viewDistance;
            if (Physics.Raycast(
                    eye.position,
                    worldDirection,
                    out RaycastHit hit,
                    viewDistance,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore))
            {
                distance = hit.distance;
            }

            Vector3 worldCenter = eye.position + worldDirection * distance;
            float halfHeight = verticalTangent * distance;
            Vector3 worldTop = worldCenter + eye.up * halfHeight;
            Vector3 worldBottom = worldCenter - eye.up * halfHeight;

            vertices.Add(transform.InverseTransformPoint(worldTop));
            vertices.Add(transform.InverseTransformPoint(worldBottom));
        }

        for (int i = 0; i < sampleCount; i++)
        {
            int top = 2 + i * 2;
            int bottom = top + 1;
            int nextTop = top + 2;
            int nextBottom = top + 3;

            AddTriangle(triangles, 0, top, nextTop);
            AddTriangle(triangles, 1, nextBottom, bottom);
            AddTriangle(triangles, top, bottom, nextBottom);
            AddTriangle(triangles, top, nextBottom, nextTop);
        }

        int firstTop = 2;
        int firstBottom = 3;
        int lastTop = 2 + sampleCount * 2;
        int lastBottom = lastTop + 1;
        AddTriangle(triangles, 0, firstBottom, firstTop);
        AddTriangle(triangles, 0, lastTop, lastBottom);

        viewMesh.Clear();
        viewMesh.SetVertices(vertices);
        viewMesh.SetTriangles(triangles, 0);
        viewMesh.RecalculateNormals();
        viewMesh.RecalculateBounds();
    }

    private static void AddTriangle(List<int> triangles, int first, int second, int third)
    {
        triangles.Add(first);
        triangles.Add(second);
        triangles.Add(third);
    }

    private void OnValidate()
    {
        viewDistance = Mathf.Max(0.1f, viewDistance);
        horizontalSamples = Mathf.Clamp(horizontalSamples, 4, 128);
        meshUpdateInterval = Mathf.Max(0.01f, meshUpdateInterval);
        proximityRadius = Mathf.Max(0.1f, proximityRadius);
        proximityHeight = Mathf.Max(0.1f, proximityHeight);
    }

    private void OnDrawGizmos()
    {
        if (proximityOrigin == null) return;

        Color previousColor = Gizmos.color;
        Gizmos.color = proximityGizmoColor;

        Vector3 center = GetProximityCenter();
        float halfHeight = proximityHeight * 0.5f;
        const int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float nextAngle = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 current = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * proximityRadius;
            Vector3 next = new Vector3(Mathf.Cos(nextAngle), 0f, Mathf.Sin(nextAngle)) * proximityRadius;
            Gizmos.DrawLine(center + current + Vector3.up * halfHeight,
                            center + next + Vector3.up * halfHeight);
            Gizmos.DrawLine(center + current - Vector3.up * halfHeight,
                            center + next - Vector3.up * halfHeight);
            if (i % 8 == 0)
                Gizmos.DrawLine(center + current - Vector3.up * halfHeight,
                                center + current + Vector3.up * halfHeight);
        }

        Gizmos.color = previousColor;
    }

    private void OnDestroy()
    {
        if (viewMesh != null) Destroy(viewMesh);
        if (proximityMesh != null) Destroy(proximityMesh);
    }
}
