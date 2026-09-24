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

    [Header("Visible Cone")]
    [SerializeField, Range(4, 128)] private int horizontalSamples = 48;
    [SerializeField, Min(0.01f)] private float meshUpdateInterval = 0.08f;

    private MeshFilter meshFilter;
    private Mesh viewMesh;
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
        viewMesh = new Mesh
        {
            name = "Boss Vision Cone"
        };
        viewMesh.MarkDynamic();
        meshFilter.sharedMesh = viewMesh;
    }

    private void OnEnable()
    {
        updateTimer = 0f;
    }

    private void Update()
    {
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0f)
        {
            updateTimer = meshUpdateInterval;
            RebuildVisibleCone();
        }
    }

    public bool CanSee(Transform target)
    {
        if (eye == null || target == null)
        {
            return false;
        }

        Vector3 toTarget = target.position - eye.position;
        float distance = toTarget.magnitude;
        if (distance <= Mathf.Epsilon || distance > viewDistance)
        {
            return false;
        }

        Vector3 localDirection = eye.InverseTransformDirection(toTarget / distance);
        if (localDirection.z <= 0f)
        {
            return false;
        }

        float horizontalAngle = Mathf.Abs(
            Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg);
        float horizontalDistance = new Vector2(localDirection.x, localDirection.z).magnitude;
        float verticalAngle = Mathf.Abs(
            Mathf.Atan2(localDirection.y, horizontalDistance) * Mathf.Rad2Deg);

        if (horizontalAngle > horizontalViewAngle * 0.5f ||
            verticalAngle > verticalViewAngle * 0.5f)
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
    }
}
