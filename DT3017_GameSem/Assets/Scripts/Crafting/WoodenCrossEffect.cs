using System.Collections;
using UnityEngine;

/// <summary>Plays the wooden cross's temporary particles and positional sound.</summary>
[DisallowMultipleComponent]
public sealed class WoodenCrossEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CraftedDecorationInteractable interaction;
    [SerializeField] private ParticleSystem effectParticles;

    [Header("Effect")]
    [SerializeField, Min(0.01f)] private float effectDuration = 3f;
    [SerializeField] private AudioClip effectClip;
    [SerializeField, Range(0f, 1f)] private float effectVolume = 1f;

    private AudioSource effectAudio;
    private Coroutine effectRoutine;
    private bool interactionLocked;

    public bool IsPlaying => effectRoutine != null;

    private void Awake()
    {
        if (interaction == null)
            interaction = GetComponent<CraftedDecorationInteractable>();

        if (effectParticles != null)
        {
            ParticleSystem.MainModule main = effectParticles.main;
            main.playOnAwake = false;
            effectParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        var audioObject = new GameObject("Wooden Cross Audio");
        audioObject.transform.SetParent(transform, false);
        effectAudio = audioObject.AddComponent<AudioSource>();
        effectAudio.playOnAwake = false;
        effectAudio.loop = false;
        effectAudio.spatialBlend = 1f;
        effectAudio.rolloffMode = AudioRolloffMode.Logarithmic;
        ConfigureAudioOutput();
    }

    private void OnEnable()
    {
        effectParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Start()
    {
        // The model may have just become visible after crafting.
        effectParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void PlayEffect()
    {
        if (effectRoutine != null || interaction == null || effectParticles == null)
        {
            if (interaction == null || effectParticles == null)
                Debug.LogError("WoodenCrossEffect needs an interaction and a Particle System.", this);
            return;
        }

        interaction.SetCanInteract(false);
        interactionLocked = true;

        effectParticles.Play(true);
        if (effectClip != null)
        {
            ConfigureAudioOutput();
            effectAudio.PlayOneShot(effectClip, effectVolume);
        }

        effectRoutine = StartCoroutine(FinishEffect());
    }

    private IEnumerator FinishEffect()
    {
        float duration = Mathf.Max(0.01f, effectDuration);
        yield return new WaitForSeconds(duration);
        effectParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float remainingAudio = effectClip == null ? 0f :
            Mathf.Max(0f, effectClip.length - duration);
        if (remainingAudio > 0f)
            yield return new WaitForSeconds(remainingAudio);

        effectRoutine = null;
        interaction.SetCanInteract(true);
        interactionLocked = false;
    }

    private void ConfigureAudioOutput()
    {
        if (effectAudio == null) return;

        GameAudioHub hub = GameAudioHub.Instance;
        if (hub == null) return;

        effectAudio.outputAudioMixerGroup = hub.WorldSfxGroup;
        effectAudio.minDistance = hub.WorldMinDistance;
        effectAudio.maxDistance = hub.WorldMaxDistance;
    }

    private void OnDisable()
    {
        if (effectRoutine != null)
            StopCoroutine(effectRoutine);
        effectRoutine = null;

        effectParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effectAudio?.Stop();

        if (interactionLocked && interaction != null)
            interaction.SetCanInteract(true);
        interactionLocked = false;
    }
}
