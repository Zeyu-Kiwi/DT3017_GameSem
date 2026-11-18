using UnityEngine;

/// <summary>Uses the player's existing head-bob step event.</summary>
[DisallowMultipleComponent]
public sealed class PlayerFootstepAudio : MonoBehaviour
{
    [SerializeField] private HeadBob headBob;
    [SerializeField] private AudioClip[] stepClips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    private void OnEnable()
    {
        if (headBob == null) headBob = GetComponentInChildren<HeadBob>(true);
        if (headBob != null) headBob.OnStep += PlayStep;
    }

    private void OnDisable()
    {
        if (headBob != null) headBob.OnStep -= PlayStep;
    }

    private void PlayStep()
    {
        if (stepClips == null || stepClips.Length == 0) return;
        AudioClip clip = stepClips[Random.Range(0, stepClips.Length)];
        WorldAudioOneShot.Play(clip, transform.position, volume,
            Random.Range(pitchRange.x, pitchRange.y));
    }
}
