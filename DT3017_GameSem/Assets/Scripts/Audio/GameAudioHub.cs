using System.Collections;
using DialogueEditor;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>Shared ambience and non-positional UI sounds for one gameplay scene.</summary>
[DisallowMultipleComponent]
public sealed class GameAudioHub : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource ambienceSourceA;
    [SerializeField] private AudioSource ambienceSourceB;
    [SerializeField] private AudioSource uiSource;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup ambienceGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup worldSfxGroup;

    [Header("Ambience")]
    [SerializeField] private AudioClip factoryInterior;
    [SerializeField] private AudioClip insideVent;
    [SerializeField] private AudioClip exteriorNight;
    [SerializeField, Min(0f)] private float ambienceFadeSeconds = 1f;
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 1f;

    [Header("UI Defaults")]
    [SerializeField] private AudioClip defaultClick;
    [SerializeField] private AudioClip defaultHover;
    [SerializeField] private AudioClip dialogueOptionHover;
    [SerializeField] private AudioClip dialogueSpeechAppear;
    [SerializeField] private AudioClip caughtWarning;
    [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;

    [Header("World SFX Defaults")]
    [SerializeField, Min(0.01f)] private float worldMinDistance = 1f;
    [SerializeField, Min(0.1f)] private float worldMaxDistance = 20f;

    private AudioSource activeAmbience;
    private Coroutine fadeRoutine;

    public static GameAudioHub Instance { get; private set; }
    public AudioMixerGroup WorldSfxGroup => worldSfxGroup;
    public float WorldMinDistance => worldMinDistance;
    public float WorldMaxDistance => Mathf.Max(worldMinDistance, worldMaxDistance);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Only one GameAudioHub should be active in a scene.", this);
            enabled = false;
            return;
        }
        Instance = this;
        ConfigureAmbienceSource(ambienceSourceA);
        ConfigureAmbienceSource(ambienceSourceB);
        if (uiSource != null)
        {
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0f;
            uiSource.ignoreListenerPause = true;
            uiSource.outputAudioMixerGroup = uiGroup;
        }
    }

    private void OnEnable() => ConversationManager.OnSpeechShown += PlayDialogueSpeech;

    private void Start() => PlayFactoryAmbience();

    private void OnDisable()
    {
        ConversationManager.OnSpeechShown -= PlayDialogueSpeech;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = null;
        if (Instance == this) Instance = null;
    }

    private void ConfigureAmbienceSource(AudioSource source)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = ambienceGroup;
        source.volume = 0f;
    }

    public void PlayFactoryAmbience() => SwitchAmbience(factoryInterior);
    public void PlayVentAmbience() => SwitchAmbience(insideVent);
    public void PlayExteriorAmbience() => SwitchAmbience(exteriorNight);

    private void SwitchAmbience(AudioClip clip)
    {
        if (clip == null || ambienceSourceA == null || ambienceSourceB == null) return;
        if (activeAmbience != null && activeAmbience.clip == clip && activeAmbience.isPlaying) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        AudioSource incoming = activeAmbience == ambienceSourceA ? ambienceSourceB : ambienceSourceA;
        AudioSource outgoing = activeAmbience;
        incoming.Stop();
        incoming.clip = clip;
        incoming.volume = 0f;
        incoming.Play();
        activeAmbience = incoming;
        fadeRoutine = StartCoroutine(FadeAmbience(outgoing, incoming));
    }

    private IEnumerator FadeAmbience(AudioSource outgoing, AudioSource incoming)
    {
        float startOutgoing = outgoing != null ? outgoing.volume : 0f;
        float elapsed = 0f;
        while (elapsed < ambienceFadeSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, ambienceFadeSeconds));
            incoming.volume = Mathf.Lerp(0f, ambienceVolume, t);
            if (outgoing != null) outgoing.volume = Mathf.Lerp(startOutgoing, 0f, t);
            yield return null;
        }
        incoming.volume = ambienceVolume;
        if (outgoing != null) outgoing.Stop();
        fadeRoutine = null;
    }

    public void PlayDefaultClick(AudioClip overrideClip = null) => PlayUi(overrideClip != null ? overrideClip : defaultClick);
    public void PlayDefaultHover(AudioClip overrideClip = null) => PlayUi(overrideClip != null ? overrideClip : defaultHover);
    public void PlayDialogueOptionHover() => PlayUi(dialogueOptionHover != null ? dialogueOptionHover : defaultHover);
    public void PlayCaughtWarning() => PlayUi(caughtWarning);
    public void PlayUiCue(AudioClip clip, float volume = 1f) => PlayUi(clip, volume);
    private void PlayDialogueSpeech() => PlayUi(dialogueSpeechAppear);

    private void PlayUi(AudioClip clip, float volume = 1f)
    {
        if (clip != null && uiSource != null)
            uiSource.PlayOneShot(clip, uiVolume * Mathf.Clamp01(volume));
    }

    private void OnValidate()
    {
        worldMinDistance = Mathf.Max(0.01f, worldMinDistance);
        worldMaxDistance = Mathf.Max(worldMinDistance, worldMaxDistance);
    }
}
