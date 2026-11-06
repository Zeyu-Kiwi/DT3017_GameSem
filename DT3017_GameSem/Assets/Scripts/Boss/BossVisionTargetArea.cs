using UnityEngine;

/// <summary>
/// Defines a stationary box of sample points that BossVision can test.
/// This is detection data only and does not require a physical Collider.
/// </summary>
[DisallowMultipleComponent]
public sealed class BossVisionTargetArea : MonoBehaviour
{
    [Header("Detection Area")]
    [SerializeField] private Vector3 areaOffset = Vector3.zero;
    [SerializeField] private Vector3 areaSize = new Vector3(1.5f, 1f, 1.5f);
    [SerializeField, Range(2, 8)] private int samplesPerAxis = 3;

    [Header("Scene Gizmo")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.25f, 0.2f, 0.9f);
    [SerializeField] private Color sampleMarkerColor = new Color(1f, 0.85f, 0.15f, 0.9f);
    [SerializeField, Min(0.005f)] private float sampleMarkerSize = 0.035f;

    public int SampleCount
    {
        get
        {
            int count = Mathf.Clamp(samplesPerAxis, 2, 8);
            return count * count * count;
        }
    }

    public Vector3 GetSampleWorldPosition(int index)
    {
        int count = Mathf.Clamp(samplesPerAxis, 2, 8);
        int total = count * count * count;
        index = Mathf.Clamp(index, 0, total - 1);

        int xIndex = index % count;
        int yIndex = (index / count) % count;
        int zIndex = index / (count * count);

        float denominator = count - 1f;
        Vector3 normalized = new Vector3(
            xIndex / denominator - 0.5f,
            yIndex / denominator - 0.5f,
            zIndex / denominator - 0.5f);

        Vector3 localPosition = areaOffset + Vector3.Scale(normalized, areaSize);
        // Area Size uses world units while still following this object's rotation.
        return transform.position + transform.rotation * localPosition;
    }

    private void OnValidate()
    {
        areaSize.x = Mathf.Max(0.01f, Mathf.Abs(areaSize.x));
        areaSize.y = Mathf.Max(0.01f, Mathf.Abs(areaSize.y));
        areaSize.z = Mathf.Max(0.01f, Mathf.Abs(areaSize.z));
        samplesPerAxis = Mathf.Clamp(samplesPerAxis, 2, 8);
        sampleMarkerSize = Mathf.Max(0.005f, sampleMarkerSize);
    }

    private void OnDrawGizmos()
    {
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(areaOffset, areaSize);

        Gizmos.color = sampleMarkerColor;
        Gizmos.matrix = Matrix4x4.identity;
        for (int i = 0; i < SampleCount; i++)
        {
            Gizmos.DrawWireSphere(GetSampleWorldPosition(i), sampleMarkerSize);
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
