using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private AudioSource factoryVoicesSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup ambienceGroup;
    [SerializeField] private AudioMixerGroup factoryVoicesGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup worldSfxGroup;

    [Header("Ambience")]
    [SerializeField] private AudioClip factoryInterior;
    [SerializeField] private AudioClip insideVent;
    [SerializeField] private AudioClip exteriorNight;
    [SerializeField, Min(0f)] private float ambienceFadeSeconds = 1f;
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 1f;

    [Header("Factory Voices")]
    [SerializeField] private List<AudioClip> factoryVoiceClips = new List<AudioClip>();
    [SerializeField, Min(0.1f)] private float minimumVoiceInterval = 90f;
    [SerializeField, Min(0.1f)] private float maximumVoiceInterval = 150f;
    [SerializeField, Range(0f, 1f)] private float factoryVoicesVolume = 1f;
    [SerializeField, Min(0f)] private float factoryVoicesFadeOutSeconds = 0.5f;

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
    private Coroutine factoryVoicesRoutine;
    private Coroutine factoryVoicesFadeRoutine;
    private readonly List<int> playableVoiceIndices = new List<int>();
    private AudioClip lastVoiceClip;
    private bool factoryVoicesActive;

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
        if (factoryVoicesSource != null)
        {
            factoryVoicesSource.playOnAwake = false;
            factoryVoicesSource.loop = false;
            factoryVoicesSource.spatialBlend = 0f;
            factoryVoicesSource.ignoreListenerPause = false;
            factoryVoicesSource.outputAudioMixerGroup = factoryVoicesGroup;
            factoryVoicesSource.volume = factoryVoicesVolume;
            factoryVoicesSource.Stop();
        }
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
        if (factoryVoicesRoutine != null) StopCoroutine(factoryVoicesRoutine);
        if (factoryVoicesFadeRoutine != null) StopCoroutine(factoryVoicesFadeRoutine);
        factoryVoicesRoutine = null;
        factoryVoicesFadeRoutine = null;
        factoryVoicesActive = false;
        if (factoryVoicesSource != null) factoryVoicesSource.Stop();
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

    public void PlayFactoryAmbience()
    {
        SwitchAmbience(factoryInterior);
        SetFactoryVoicesActive(true);
    }

    public void PlayVentAmbience()
    {
        SetFactoryVoicesActive(false);
        SwitchAmbience(insideVent);
    }

    public void PlayExteriorAmbience()
    {
        SetFactoryVoicesActive(false);
        SwitchAmbience(exteriorNight);
    }

    private void SetFactoryVoicesActive(bool active)
    {
        if (factoryVoicesActive == active) return;
        factoryVoicesActive = active;

        if (factoryVoicesRoutine != null) StopCoroutine(factoryVoicesRoutine);
        factoryVoicesRoutine = null;

        if (active)
        {
            if (factoryVoicesFadeRoutine != null) StopCoroutine(factoryVoicesFadeRoutine);
            factoryVoicesFadeRoutine = null;
            if (factoryVoicesSource == null) return;
            factoryVoicesSource.Stop();
            factoryVoicesSource.volume = factoryVoicesVolume;
            factoryVoicesRoutine = StartCoroutine(PlayFactoryVoices());
        }
        else if (factoryVoicesSource != null && factoryVoicesSource.isPlaying)
        {
            factoryVoicesFadeRoutine = StartCoroutine(FadeOutFactoryVoice());
        }
        else if (factoryVoicesSource != null)
        {
            // A paused source may report that it is not playing; do not let it
            // resume later during the vent or exterior phase.
            factoryVoicesSource.Stop();
            factoryVoicesSource.volume = factoryVoicesVolume;
        }
    }

    private IEnumerator PlayFactoryVoices()
    {
        while (factoryVoicesActive)
        {
            float delay = Random.Range(minimumVoiceInterval, maximumVoiceInterval);
            yield return new WaitForSeconds(delay);
            if (!factoryVoicesActive) break;

            AudioClip clip = PickFactoryVoiceClip();
            if (clip == null) continue;

            factoryVoicesSource.clip = clip;
            factoryVoicesSource.volume = factoryVoicesVolume;
            factoryVoicesSource.Play();
            while (factoryVoicesActive &&
                   (Time.timeScale <= 0f || factoryVoicesSource.isPlaying))
                yield return null;
        }

        factoryVoicesRoutine = null;
    }

    private AudioClip PickFactoryVoiceClip()
    {
        playableVoiceIndices.Clear();
        for (int i = 0; i < factoryVoiceClips.Count; i++)
        {
            if (factoryVoiceClips[i] != null && factoryVoiceClips[i] != lastVoiceClip)
                playableVoiceIndices.Add(i);
        }

        // One valid clip can repeat; two or more valid clips cannot repeat immediately.
        if (playableVoiceIndices.Count == 0)
        {
            for (int i = 0; i < factoryVoiceClips.Count; i++)
                if (factoryVoiceClips[i] != null) playableVoiceIndices.Add(i);
        }

        if (playableVoiceIndices.Count == 0) return null;
        int chosenIndex = playableVoiceIndices[Random.Range(0, playableVoiceIndices.Count)];
        lastVoiceClip = factoryVoiceClips[chosenIndex];
        return lastVoiceClip;
    }

    private IEnumerator FadeOutFactoryVoice()
    {
        float startVolume = factoryVoicesSource.volume;
        float duration = Mathf.Max(0f, factoryVoicesFadeOutSeconds);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            factoryVoicesSource.volume = Mathf.Lerp(startVolume, 0f,
                Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        factoryVoicesSource.Stop();
        factoryVoicesSource.volume = factoryVoicesVolume;
        factoryVoicesFadeRoutine = null;
    }

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
        minimumVoiceInterval = Mathf.Max(0.1f, minimumVoiceInterval);
        maximumVoiceInterval = Mathf.Max(minimumVoiceInterval, maximumVoiceInterval);
        factoryVoicesFadeOutSeconds = Mathf.Max(0f, factoryVoicesFadeOutSeconds);
    }
}
