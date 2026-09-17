using System.Collections.Generic;
using UnityEngine;

public class CraftingMaterialSection : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField] private GameObject modelPrefab;
    [Tooltip("One model is displayed at each point, up to the available quantity.")]
    [SerializeField] private List<Transform> storageSpawnPoints = new List<Transform>();

    [Header("Scene Gizmo")]
    [SerializeField] private bool showSectionGizmo = true;
    [SerializeField] private bool showGizmoOnlyWhenSelected;
    [SerializeField] private Vector3 gizmoOffset = Vector3.zero;
    [SerializeField] private Vector3 gizmoSize = new Vector3(1f, 0.25f, 1f);
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.9f);

    private readonly List<GameObject> generatedModels = new List<GameObject>();
    private CraftingStation station;

    public ItemData Item => item;
    public GameObject ModelPrefab => modelPrefab;

    public void Initialize(CraftingStation owningStation)
    {
        station = owningStation;

        for (int i = 0; i < storageSpawnPoints.Count; i++)
        {
            Transform spawnPoint = storageSpawnPoints[i];
            if (spawnPoint == null || modelPrefab == null)
            {
                generatedModels.Add(null);
                continue;
            }

            // Instantiate without a parent first, then preserve its world transform when
            // parenting it. This prevents a scaled section hierarchy from stretching it.
            GameObject model = Instantiate(modelPrefab);
            model.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            model.transform.SetParent(spawnPoint, true);

            CraftingItemView itemView = model.GetComponent<CraftingItemView>();
            if (itemView == null)
            {
                itemView = model.AddComponent<CraftingItemView>();
            }

            itemView.Configure(station, item, false, true);
            model.SetActive(false);
            generatedModels.Add(model);
        }
    }

    public void Refresh(int visibleQuantity)
    {
        int clampedQuantity = Mathf.Clamp(visibleQuantity, 0, generatedModels.Count);

        for (int i = 0; i < generatedModels.Count; i++)
        {
            if (generatedModels[i] != null)
            {
                generatedModels[i].SetActive(i < clampedQuantity);
            }
        }
    }

    private void OnValidate()
    {
        gizmoSize.x = Mathf.Max(0.01f, Mathf.Abs(gizmoSize.x));
        gizmoSize.y = Mathf.Max(0.01f, Mathf.Abs(gizmoSize.y));
        gizmoSize.z = Mathf.Max(0.01f, Mathf.Abs(gizmoSize.z));
    }

    private void OnDrawGizmos()
    {
        if (showSectionGizmo && !showGizmoOnlyWhenSelected)
        {
            DrawSectionGizmo();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (showSectionGizmo && showGizmoOnlyWhenSelected)
        {
            DrawSectionGizmo();
        }
    }

    private void DrawSectionGizmo()
    {
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.color = gizmoColor;
        // Ignore inherited scale so Gizmo Size is measured in world units.
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(gizmoOffset, gizmoSize);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
