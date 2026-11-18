using UnityEngine;
using UnityEngine.Audio;

/// <summary>Timed footsteps while the boss controller is moving.</summary>
[DisallowMultipleComponent]
public sealed class BossFootstepAudio : MonoBehaviour
{
    [SerializeField] private BossController boss;
    [SerializeField] private AudioClip[] stepClips;
    [SerializeField, Min(0.05f)] private float stepInterval = 0.45f;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [Header("Boss Footstep Output")]
    [SerializeField] private AudioMixerGroup bossFootstepsGroup;
    [Tooltip("Distance in Unity world units where footsteps begin to fade. Yellow gizmo.")]
    [SerializeField, Min(0.01f)] private float minDistance = 1f;
    [Tooltip("Distance in Unity world units where footsteps become silent. Red gizmo.")]
    [SerializeField, Min(0.02f)] private float maxDistance = 20f;

    private AudioSource stepSource;
    private float timer;

    private void Awake()
    {
        if (boss == null) boss = GetComponentInParent<BossController>();

        var audioObject = new GameObject("Boss Footsteps Audio");
        audioObject.transform.SetParent(transform, false);
        stepSource = audioObject.AddComponent<AudioSource>();
        stepSource.playOnAwake = false;
        stepSource.spatialBlend = 1f;
        stepSource.rolloffMode = AudioRolloffMode.Linear;
        stepSource.volume = 1f;
        ApplyAudioSettings();
    }

    private void Update()
    {
        if (boss == null || !boss.IsWalking)
        {
            timer = 0f;
            return;
        }
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = stepInterval;
        if (stepClips == null || stepClips.Length == 0) return;
        AudioClip clip = stepClips[Random.Range(0, stepClips.Length)];
        if (clip == null) return;
        stepSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        stepSource.PlayOneShot(clip, volume);
    }

    private void ApplyAudioSettings()
    {
        if (stepSource == null) return;
        stepSource.outputAudioMixerGroup = bossFootstepsGroup;
        stepSource.minDistance = minDistance;
        stepSource.maxDistance = maxDistance;
    }

    private void OnValidate()
    {
        minDistance = Mathf.Max(0.01f, minDistance);
        maxDistance = Mathf.Max(minDistance + 0.01f, maxDistance);
        ApplyAudioSettings();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(minDistance, maxDistance));
    }
}
