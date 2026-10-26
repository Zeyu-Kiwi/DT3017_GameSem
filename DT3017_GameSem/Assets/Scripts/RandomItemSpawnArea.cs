using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RandomItemSpawnArea : MonoBehaviour
{
    [Serializable]
    public sealed class SpawnEntry
    {
        public GameObject pickupPrefab;
        [Min(0)] public int minimumCount = 1;
        [Min(0)] public int maximumCount = 1;
    }

    [Header("Day Range (Inclusive)")]
    [SerializeField, Min(1)] private int firstActiveDay = 2;
    [SerializeField, Min(1)] private int lastActiveDay = 2;
    [Tooltip("Enable to create a new set on every active day. Disable to spawn only on the first active day.")]
    [SerializeField] private bool spawnEveryActiveDay = true;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 areaSize = new Vector3(2f, 2f, 2f);
    [SerializeField] private LayerMask floorLayers = ~0;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.03f;
    [SerializeField, Min(0f)] private float clearanceRadius = 0.15f;
    [SerializeField, Min(1)] private int placementAttempts = 20;

    [Header("Items")]
    [SerializeField] private List<SpawnEntry> items = new List<SpawnEntry>();

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private DayCycleManager dayCycle;
    private Transform runtimeContainer;
    private bool hasSpawnedOnce;

    private void Awake()
    {
        dayCycle = FindFirstObjectByType<DayCycleManager>();
        GameObject container = new GameObject("Spawned Items (Runtime)");
        runtimeContainer = container.transform;
        runtimeContainer.SetParent(transform, false);
    }

    private void OnEnable()
    {
        if (dayCycle == null) dayCycle = FindFirstObjectByType<DayCycleManager>();
        if (dayCycle != null) dayCycle.DayStarted += HandleDayStarted;
    }

    private void Start()
    {
        if (dayCycle != null) SpawnForDay(dayCycle.CurrentDay);
    }

    private void OnDisable()
    {
        if (dayCycle != null) dayCycle.DayStarted -= HandleDayStarted;
    }

    private void HandleDayStarted(int day)
    {
        ClearSpawnedItems();
        SpawnForDay(day);
    }

    public void SpawnForDay(int day)
    {
        if (day < firstActiveDay || day > lastActiveDay) return;
        if (!spawnEveryActiveDay && hasSpawnedOnce) return;

        for (int i = 0; i < items.Count; i++)
        {
            SpawnEntry entry = items[i];
            if (entry == null || entry.pickupPrefab == null) continue;
            int minimum = Mathf.Max(0, entry.minimumCount);
            int maximum = Mathf.Max(minimum, entry.maximumCount);
            int count = UnityEngine.Random.Range(minimum, maximum + 1);
            for (int amount = 0; amount < count; amount++)
                TrySpawn(entry.pickupPrefab);
        }

        hasSpawnedOnce = true;
    }

    public void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
            if (spawnedObjects[i] != null) Destroy(spawnedObjects[i]);
        spawnedObjects.Clear();
    }

    private bool TrySpawn(GameObject prefab)
    {
        Vector3 half = areaSize * 0.5f;
        for (int attempt = 0; attempt < placementAttempts; attempt++)
        {
            Vector3 local = new Vector3(
                UnityEngine.Random.Range(-half.x, half.x),
                half.y,
                UnityEngine.Random.Range(-half.z, half.z));
            Vector3 origin = transform.TransformPoint(local);
            float castDistance = Mathf.Max(0.01f, areaSize.y * Mathf.Abs(transform.lossyScale.y));

            if (!Physics.Raycast(origin, -transform.up, out RaycastHit hit,
                    castDistance, floorLayers, QueryTriggerInteraction.Ignore))
                continue;

            Vector3 position = hit.point + hit.normal * surfaceOffset;
            if (clearanceRadius > 0f && Physics.CheckSphere(position, clearanceRadius,
                    obstacleLayers, QueryTriggerInteraction.Ignore))
                continue;

            GameObject spawned = Instantiate(prefab);
            spawned.transform.SetPositionAndRotation(position,
                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            spawned.transform.SetParent(runtimeContainer, true);
            spawnedObjects.Add(spawned);
            return true;
        }

        Debug.LogWarning("Could not find a clear floor position for " + prefab.name + ".", this);
        return false;
    }

    private void OnValidate()
    {
        firstActiveDay = Mathf.Max(1, firstActiveDay);
        lastActiveDay = Mathf.Max(firstActiveDay, lastActiveDay);
        areaSize.x = Mathf.Max(0.01f, areaSize.x);
        areaSize.y = Mathf.Max(0.01f, areaSize.y);
        areaSize.z = Mathf.Max(0.01f, areaSize.z);
        placementAttempts = Mathf.Max(1, placementAttempts);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null) continue;
            items[i].minimumCount = Mathf.Max(0, items[i].minimumCount);
            items[i].maximumCount = Mathf.Max(items[i].minimumCount, items[i].maximumCount);
        }
    }

    private void OnDrawGizmos()
    {
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.18f);
        Gizmos.DrawCube(Vector3.zero, areaSize);
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, areaSize);
        Gizmos.matrix = previous;
    }
}
