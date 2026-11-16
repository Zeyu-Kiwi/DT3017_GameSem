using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverSound : MonoBehaviour,
    IPointerEnterHandler, IPointerClickHandler
{
    public AudioSource audioSource;

    [Header("Hover")]
    public AudioClip hoverClip;
    [Range(0f, 1f)] public float hoverVolume = 0.7f;

    [Header("Click")]
    public AudioClip clickClip;
    [Range(0f, 1f)] public float clickVolume = 1f;

    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    public void OnPointerEnter(PointerEventData eventData)
    {
        Play(hoverClip, hoverVolume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Play(clickClip, clickVolume);
    }

    void Play(AudioClip clip, float volume)
    {
        if (!audioSource || !clip) return;
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume);
    }
}