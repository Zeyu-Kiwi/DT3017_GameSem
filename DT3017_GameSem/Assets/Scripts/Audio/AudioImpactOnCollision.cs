using UnityEngine;

/// <summary>Plays one impact after a detached physics object touches the world.</summary>
[DisallowMultipleComponent]
public sealed class AudioImpactOnCollision : MonoBehaviour
{
    private AudioClip impactClip;
    private float volume;
    private bool played;

    public void Configure(AudioClip clip, float loudness)
    {
        impactClip = clip;
        volume = loudness;
        played = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (played || impactClip == null) return;
        if (collision.contactCount == 0 || collision.GetContact(0).normal.y < 0.5f) return;
        played = true;
        WorldAudioOneShot.Play(impactClip, collision.GetContact(0).point, volume);
    }
}
