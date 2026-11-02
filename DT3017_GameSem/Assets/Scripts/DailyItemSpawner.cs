using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DailyItemSpawner : MonoBehaviour
{
    [Serializable]
    public sealed class SpawnRecord
    {
        [SerializeField] private int day;
        [SerializeField] private string zoneId;
        [SerializeField] private ItemData item;
        [SerializeField] private int quantity;
        [SerializeField] private Vector3 position;
        public int Day => day;
        public string ZoneId => zoneId;
        public ItemData Item => item;
        public int Quantity => quantity;
        public Vector3 Position => position;
        public SpawnRecord(int day, string zoneId, ItemData item, int quantity, Vector3 position)
        { this.day = day; this.zoneId = zoneId; this.item = item; this.quantity = quantity; this.position = position; }
    }

    public sealed class SpawnContext
    {
        public int Day { get; }
        public ItemSpawnZone Zone { get; }
        public int PreviousSpawnDay { get; }
        public IReadOnlyList<SpawnRecord> PreviousSpawns { get; }
        public SpawnContext(int day, ItemSpawnZone zone, int previousDay, IReadOnlyList<SpawnRecord> previous)
        { Day = day; Zone = zone; PreviousSpawnDay = previousDay; PreviousSpawns = previous; }
    }

    [SerializeField] private DayCycleManager dayCycle;
    [Tooltip("Initial day used when no active day cycle is available, e.g. a prototype scene.")]
    [SerializeField, Min(1)] private int fallbackDay = 1;
    [SerializeField] private DailyItemSpawnTable defaultTable;
    [Tooltip("Use enabled zones in this scene automatically. Otherwise only the explicit list is used.")]
    [SerializeField] private bool discoverSceneZones = true;
    [SerializeField] private List<ItemSpawnZone> zones = new List<ItemSpawnZone>();
    [Tooltip("Same seed, day, zone IDs and geometry reproduce the same rolls without changing Unity's global random state.")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool logSpawnSummary;
    [Tooltip("Layer used by the player interaction ray. Applied only to spawned copies.")]
    [SerializeField, Range(0,31)] private int interactionLayer = 3;

    [Header("Runtime spawn history (current session)")]
    [SerializeField] private int lastSpawnedDay;
    [SerializeField] private int previousSpawnDay;
    [SerializeField] private List<SpawnRecord> currentSpawns = new List<SpawnRecord>();
    [SerializeField] private List<SpawnRecord> previousSpawns = new List<SpawnRecord>();
    private readonly List<GameObject> ownedPickups = new List<GameObject>();
    private readonly HashSet<ItemData> warnedItems = new HashSet<ItemData>();
    private Transform spawnedRoot;
    private Transform stagingRoot;
    private bool started;
    public IReadOnlyList<SpawnRecord> CurrentSpawns => currentSpawns;
    public IReadOnlyList<SpawnRecord> PreviousSpawns => previousSpawns;
    public int LastSpawnedDay => lastSpawnedDay;
    public event Action<int> SpawnCompleted;

    private void Awake()
    {
        if (dayCycle == null) dayCycle = FindFirstObjectByType<DayCycleManager>() ?? FindFirstObjectByType<DayCycleManager>(FindObjectsInactive.Include);
        // Serialized debug history is never treated as live state on a new run.
        lastSpawnedDay = previousSpawnDay = 0;
        currentSpawns.Clear(); previousSpawns.Clear();
    }

    private void OnEnable()
    {
        if (dayCycle != null) dayCycle.DayStarted += OnDayStarted;
        if (started) SpawnForDay(CurrentDayNumber);
    }

    private IEnumerator Start()
    {
        yield return null;
        started = true;
        if (dayCycle == null || !dayCycle.isActiveAndEnabled)
            Debug.LogWarning("DailyItemSpawner is using its fallback day because the day cycle is missing or inactive. Activate the assigned day cycle for automatic daily refreshes.", this);
        SpawnForDay(CurrentDayNumber);
    }

    private void OnDisable() { if (dayCycle != null) dayCycle.DayStarted -= OnDayStarted; }
    private void OnDayStarted(int day) => SpawnForDay(day);
    private int CurrentDayNumber => dayCycle != null && dayCycle.CurrentDay > 0 ? dayCycle.CurrentDay : Mathf.Max(1, fallbackDay);

    [ContextMenu("Respawn Current Day (Play Mode)")]
    public void RespawnCurrentDay()
    {
        if (Application.isPlaying) SpawnForDay(CurrentDayNumber, true);
    }

    public void SpawnForDay(int day, bool force = false)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || day < 1 || (!force && day == lastSpawnedDay)) return;
        if (InventoryManager.Instance == null) { Debug.LogWarning("Daily spawning needs an InventoryManager.", this); return; }
        if (day != lastSpawnedDay)
        {
            previousSpawnDay = lastSpawnedDay;
            previousSpawns.Clear(); previousSpawns.AddRange(currentSpawns);
        }
        ClearOwnedPickups();
        currentSpawns.Clear(); warnedItems.Clear();
        lastSpawnedDay = day;
        EnsureRoots();
        var activeZones = discoverSceneZones
            ? new List<ItemSpawnZone>(FindObjectsByType<ItemSpawnZone>(FindObjectsSortMode.None))
            : new List<ItemSpawnZone>(zones);
        activeZones.RemoveAll(z => z == null || !z.isActiveAndEnabled || z.gameObject.scene != gameObject.scene);
        activeZones.Sort((a,b) => string.CompareOrdinal(a.ZoneId, b.ZoneId));
        var visited = new HashSet<ItemSpawnZone>();
        var ids = new HashSet<string>();
        foreach (var zone in activeZones)
        {
            if (!visited.Add(zone)) continue;
            if (!ids.Add(zone.ZoneId)) Debug.LogWarning("Duplicate daily spawn zone ID: " + zone.ZoneId, zone);
            var context = new SpawnContext(day, zone, previousSpawnDay, previousSpawns.AsReadOnly());
            var table = ResolveTable(context);
            if (table == null) continue;
            var random = new System.Random(ZoneSeed(day, zone.ZoneId));
            int count = zone.RollCount(random);
            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                if (!TryChoose(table, context, random, out var entry)) break;
                int quantity = random.Next(entry.MinimumQuantity, entry.MaximumQuantity + 1);
                if (TrySpawn(entry, quantity, zone, random, out var pickup))
                {
                    ownedPickups.Add(pickup.gameObject);
                    currentSpawns.Add(new SpawnRecord(day, zone.ZoneId, entry.Item, quantity, pickup.transform.position));
                    placed++;
                }
            }
            if (placed < count) Debug.LogWarning("Daily spawning placed " + placed + "/" + count + " items in " + zone.name + ". Check its table, ground layers and free space.", zone);
        }
        if (logSpawnSummary) Debug.Log("Day " + day + ": spawned " + currentSpawns.Count + " daily pickups.", this);
        SpawnCompleted?.Invoke(day);
    }

    // Future planned-day tables can override this without changing placement or cleanup.
    protected virtual DailyItemSpawnTable ResolveTable(SpawnContext context) =>
        context.Zone.TableOverride != null ? context.Zone.TableOverride : defaultTable;

    // Future history-based rules can adjust weights using context.PreviousSpawns.
    protected virtual float GetSelectionWeight(DailyItemSpawnTable.Entry entry, SpawnContext context) => entry.Weight;

    private bool TryChoose(DailyItemSpawnTable table, SpawnContext context, System.Random random, out DailyItemSpawnTable.Entry choice)
    {
        var candidates = new List<DailyItemSpawnTable.Entry>();
        var weights = new List<double>();
        double total = 0;
        foreach (var entry in table.Entries)
        {
            if (entry == null || !entry.IsValid) continue;
            if (!InventoryManager.Instance.RuntimeQuantities.ContainsKey(entry.Item))
            {
                if (warnedItems.Add(entry.Item)) Debug.LogWarning("Daily loot item is not registered in InventoryManager: " + entry.Item.DisplayName, this);
                continue;
            }
            float weight = GetSelectionWeight(entry, context);
            if (weight <= 0f || float.IsNaN(weight) || float.IsInfinity(weight)) continue;
            candidates.Add(entry); weights.Add(weight); total += weight;
        }
        choice = null;
        if (total <= 0) return false;
        double roll = random.NextDouble() * total;
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0) { choice = candidates[i]; return true; }
        }
        choice = candidates[candidates.Count - 1];
        return true;
    }

    private bool TrySpawn(DailyItemSpawnTable.Entry entry, int quantity, ItemSpawnZone zone, System.Random random, out ItemPickup pickup)
    {
        var instance = Instantiate(entry.Prefab, stagingRoot);
        instance.SetActive(false);
        pickup = instance.GetComponent<ItemPickup>();
        if (pickup == null) pickup = instance.AddComponent<ItemPickup>();
        pickup.Configure(entry.Item, quantity);
        // Preserve the prefab's authored scale and pose; only add a random world yaw.
        pickup.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f) * entry.Prefab.transform.rotation);
        Vector3 parentScale = stagingRoot.lossyScale;
        Vector3 authoredScale = entry.Prefab.transform.lossyScale;
        pickup.transform.localScale = new Vector3(authoredScale.x / parentScale.x, authoredScale.y / parentScale.y, authoredScale.z / parentScale.z);
        Bounds bounds = CalculateBounds(pickup.gameObject);
        EnsurePickupCollider(pickup.gameObject, bounds);
        foreach (var child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = interactionLayer;
        Vector3 minimum = bounds.min;
        for (int attempt = 0; attempt < zone.PlacementAttempts; attempt++)
        {
            if (!zone.TryFindSurface(random, out Vector3 ground)) continue;
            Vector3 position = ground - Vector3.up * minimum.y + Vector3.up * 0.03f;
            Vector3 center = bounds.center + position;
            Vector3 extents = Vector3.Max(bounds.extents - Vector3.one * 0.005f, Vector3.one * 0.01f);
            if (Physics.CheckBox(center, extents, Quaternion.identity, zone.BlockingLayers, QueryTriggerInteraction.Ignore)) continue;
            pickup.transform.position = position;
            pickup.transform.SetParent(spawnedRoot, true);
            pickup.gameObject.SetActive(true);
            Physics.SyncTransforms();
            return true;
        }
        Destroy(pickup.gameObject);
        pickup = null;
        return false;
    }

    private static void EnsurePickupCollider(GameObject root, Bounds worldBounds)
    {
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            if (collider.enabled && !collider.isTrigger) return;
        // Only spawned copies get a collider when the model prefab has none.
        Bounds local = new Bounds(root.transform.InverseTransformPoint(worldBounds.center), Vector3.zero);
        for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            local.Encapsulate(root.transform.InverseTransformPoint(worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3(x,y,z))));
        var box = root.AddComponent<BoxCollider>();
        box.center = local.center;
        box.size = Vector3.Max(local.size, Vector3.one * 0.0001f);
    }
    private static Bounds CalculateBounds(GameObject root)
    {
        Bounds result = new Bounds(root.transform.position, Vector3.zero);
        bool initialized = false;
        void Include(Bounds local, Transform owner)
        {
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                Vector3 point = owner.TransformPoint(local.center + Vector3.Scale(local.extents, new Vector3(x,y,z)));
                if (!initialized) { result = new Bounds(point, Vector3.zero); initialized = true; }
                else result.Encapsulate(point);
            }
        }
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            if (renderer.enabled && renderer.GetComponent<ItemShineVisual>() == null) Include(renderer.localBounds, renderer.transform);
        foreach (var box in root.GetComponentsInChildren<BoxCollider>(true))
            if (box.enabled && !box.isTrigger) Include(new Bounds(box.center, box.size), box.transform);
        return initialized ? result : new Bounds(root.transform.position, Vector3.one * 0.2f);
    }

    private void EnsureRoots()
    {
        if (spawnedRoot == null) { spawnedRoot = new GameObject("Spawned Daily Items").transform; spawnedRoot.SetParent(transform, false); }
        if (stagingRoot == null) { stagingRoot = new GameObject("Spawn Staging").transform; stagingRoot.SetParent(transform, false); stagingRoot.gameObject.SetActive(false); }
    }

    private void ClearOwnedPickups()
    {
        foreach (var pickup in ownedPickups) if (pickup != null) { pickup.SetActive(false); Destroy(pickup); }
        ownedPickups.Clear();
    }

    private int ZoneSeed(int day, string id)
    {
        unchecked { uint hash = (uint)seed; hash = (hash ^ (uint)day) * 16777619u; foreach (char c in id) hash = (hash ^ c) * 16777619u; return (int)(hash & 0x7fffffffu); }
    }
}