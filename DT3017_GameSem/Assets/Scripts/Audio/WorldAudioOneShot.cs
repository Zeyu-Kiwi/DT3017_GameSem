using UnityEngine;

/// <summary>Plays a positional sound independently of an object that may be destroyed.</summary>
public static class WorldAudioOneShot
{
    public static void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        var host = new GameObject("World Audio: " + clip.name);
        host.transform.position = position;
        var source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);

        GameAudioHub hub = GameAudioHub.Instance;
        if (hub != null)
        {
            source.outputAudioMixerGroup = hub.WorldSfxGroup;
            source.minDistance = hub.WorldMinDistance;
            source.maxDistance = hub.WorldMaxDistance;
        }

        source.clip = clip;
        source.Play();
        Object.Destroy(host, clip.length / source.pitch + 0.1f);
    }
}
