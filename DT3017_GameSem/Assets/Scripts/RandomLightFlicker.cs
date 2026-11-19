using UnityEngine;

/// <summary>Random, visible bursts of light flicker separated by steady rests.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public sealed class RandomLightFlicker : MonoBehaviour
{
    [Header("Timing (seconds)")]
    [Tooltip("Random delay before each flicker burst.")]
    [SerializeField] private Vector2 flickerInterval = new Vector2(.5f, 3f);
    [Tooltip("How long each burst of rapid flickering lasts.")]
    [SerializeField] private Vector2 burstDuration = new Vector2(.35f, 1.1f);
    [Tooltip("Each bright/dim flash gets its own random duration.")]
    [SerializeField] private Vector2 flashDuration = new Vector2(.04f, .16f);
    [Tooltip("Extra steady-light rest after each burst.")]
    [SerializeField] private Vector2 restDuration = new Vector2(.3f, .9f);
    [Header("Brightness (multipliers of the Light's original intensity)")]
    [SerializeField] private Vector2 dimBrightness = new Vector2(.02f, .2f);
    [SerializeField] private Vector2 brightBrightness = new Vector2(.8f, 1.15f);
    [Tooltip("Chance that a dim flash goes completely dark.")]
    [SerializeField, Range(0f, 1f)] private float blackoutChance = .4f;
    [Header("Clock")]
    [Tooltip("Enable to keep flickering while the game is paused.")]
    [SerializeField] private bool useUnscaledTime;

    private enum Phase { Waiting, Flickering, Resting }
    private Phase phase;
    private Light targetLight;
    private System.Random random;
    private float originalIntensity, timer, burstRemaining;
    private bool dim, captured;

    public bool IsFlickering => isActiveAndEnabled && phase == Phase.Flickering;

    private void OnEnable()
    {
        targetLight = GetComponent<Light>();
        originalIntensity = targetLight.intensity;
        captured = true;
        // Independent random sequence per light, without changing gameplay's Unity Random state.
        random = new System.Random(System.Guid.NewGuid().GetHashCode());
        phase = Phase.Waiting;
        timer = Sample(flickerInterval);
    }

    private void Update()
    {
        if (targetLight == null) return;
        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (delta <= 0f) return;
        timer -= delta;
        if (phase == Phase.Flickering)
        {
            burstRemaining -= delta;
            if (burstRemaining <= 0f)
            {
                targetLight.intensity = originalIntensity;
                phase = Phase.Resting;
                timer = Sample(restDuration);
            }
            else if (timer <= 0f)
            {
                dim = !dim;
                ApplyFlash();
            }
        }
        else if (timer <= 0f)
        {
            if (phase == Phase.Resting)
            {
                phase = Phase.Waiting;
                timer = Sample(flickerInterval);
            }
            else
            {
                phase = Phase.Flickering;
                burstRemaining = Sample(burstDuration);
                dim = true; // Every burst starts with an obvious dip, never a nearly invisible bright flash.
                ApplyFlash();
            }
        }
    }

    private void ApplyFlash()
    {
        float multiplier = dim
            ? (random.NextDouble() < blackoutChance ? 0f : Sample(dimBrightness))
            : Sample(brightBrightness);
        targetLight.intensity = originalIntensity * multiplier;
        timer = Sample(flashDuration);
    }

    private float Sample(Vector2 range)
    {
        float minimum = Mathf.Max(0f, range.x);
        float maximum = Mathf.Max(minimum, range.y);
        return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
    }

    private void OnDisable()
    {
        if (captured && targetLight != null) targetLight.intensity = originalIntensity;
        captured = false;
    }

    private void OnValidate()
    {
        flickerInterval = Ordered(flickerInterval, .01f);
        burstDuration = Ordered(burstDuration, .01f);
        flashDuration = Ordered(flashDuration, .01f);
        restDuration = Ordered(restDuration, 0f);
        dimBrightness = Ordered(dimBrightness, 0f);
        brightBrightness = Ordered(brightBrightness, 0f);
    }

    private static Vector2 Ordered(Vector2 range, float minimum)
    {
        float low = Mathf.Max(minimum, range.x);
        return new Vector2(low, Mathf.Max(low, range.y));
    }
}