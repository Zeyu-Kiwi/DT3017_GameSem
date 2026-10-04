using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Shine Settings", fileName = "ItemShineSettings")]
public sealed class ItemShineSettings : ScriptableObject
{
    [Header("Global")]
    public bool effectsEnabled = true;
    [Header("Diagonal Sheen")]
    public bool sheenEnabled = true;
    [ColorUsage(true, true)] public Color sheenColor = Color.white;
    [Min(0f)] public float sheenIntensity = 1.5f;
    [Range(.02f, .6f)] public float sheenWidth = .18f;
    [Min(.1f)] public float sheenDuration = 1.1f;
    [Min(0f)] public float sheenPause = 2f;

    [Header("Sparkles")]
    public bool sparklesEnabled = true;
    [ColorUsage(true, true)] public Color sparkleColor = Color.white;
    [Min(0f)] public float sparkleIntensity = 2f;
    [Tooltip("Maximum sparkle slots per item. Zero hides sparkles while keeping the sheen.")]
    [InspectorName("Sparkle Amount")]
    [Range(0, 64)] public int sparkleCount = 7;
    [Tooltip("Keep sparkles the same screen size at all distances. Otherwise they shrink like the item.")]
    public bool constantScreenSize;
    [Tooltip("Sparkle diameter in world units when Constant Screen Size is disabled.")]
    [Min(.001f)] public float sparkleWorldSize = .06f;
    [Range(4f, 40f)] public float sparkleSizePixels = 12f;
    [Min(.1f)] public float sparkleLifetime = .8f;
    [Tooltip("Wait between sparkles in each slot. Higher values produce fewer sparkles over time.")]
    [Min(0f)] public float sparkleRespawnDelay = .3f;
    [Min(0f)] public float sparklePadding = .1f;

    [Header("Pixel Style")]
    [Range(1f, 8f)] public float pixelSize = 2f;

    private static ItemShineSettings cached;
    public static ItemShineSettings Global
    {
        get
        {
            if (cached == null)
            {
                cached = Resources.Load<ItemShineSettings>("ItemShineSettings");
                if (cached == null)
                {
                    cached = CreateInstance<ItemShineSettings>();
                    cached.hideFlags = HideFlags.DontSave;
                }
            }
            return cached;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => cached = null;
}