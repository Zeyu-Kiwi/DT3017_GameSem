using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>A reusable fade-to-black cutscene callable from any UnityEvent.</summary>
[DisallowMultipleComponent]
public class ScreenFadeCutscene : MonoBehaviour
{
    [SerializeField] private CanvasGroup blackScreen;
    [SerializeField, Min(0f)] private float fadeOutDuration = .25f;
    [SerializeField, Min(0f)] private float blackDuration = 0f;
    [SerializeField, Min(0f)] private float fadeInDuration = .25f;
    [SerializeField] private UnityEvent onStarted = new UnityEvent();
    [SerializeField] private UnityEvent onBlack = new UnityEvent();
    [SerializeField] private UnityEvent onFinished = new UnityEvent();
    public bool IsPlaying { get; private set; }
    public bool IsConfigured => blackScreen != null;
    public UnityEvent OnBlack => onBlack;
    public UnityEvent OnFinished => onFinished;

    public void PlayCutscene()
    {
        if (isActiveAndEnabled && !IsPlaying) StartCoroutine(PlayRoutine());
    }

    public IEnumerator PlayRoutine(System.Action atBlack = null)
    {
        return PlayRoutine(atBlack, fadeOutDuration, blackDuration, fadeInDuration);
    }

    public IEnumerator PlayRoutine(System.Action atBlack, float fadeOutSeconds, float holdSeconds, float fadeInSeconds)
    {
        if (IsPlaying || !isActiveAndEnabled) yield break;
        if (blackScreen == null)
        {
            Debug.LogError("ScreenFadeCutscene requires a Black Screen CanvasGroup.", this);
            yield break;
        }
        // The overlay may be disabled in the scene; CanvasGroup alpha alone cannot show it.
        blackScreen.gameObject.SetActive(true);
        blackScreen.transform.SetAsLastSibling();
        IsPlaying = true;
        blackScreen.blocksRaycasts = true;
        onStarted.Invoke();
        yield return FadeTo(1f, Mathf.Max(0f, fadeOutSeconds));
        atBlack?.Invoke();
        onBlack.Invoke();
        // Render at least one fully black frame before fading back in.
        yield return null;
        if (holdSeconds > 0f) yield return new WaitForSecondsRealtime(holdSeconds);
        yield return FadeTo(0f, Mathf.Max(0f, fadeInSeconds));
        blackScreen.blocksRaycasts = false;
        IsPlaying = false;
        onFinished.Invoke();
    }

    /// <summary>Fade out and hold black. Call FadeInRoutine or CancelCutscene to clear it.</summary>
    public IEnumerator FadeOutRoutine(float duration, System.Action atBlack = null)
    {
        if (IsPlaying || !isActiveAndEnabled || blackScreen == null) yield break;
        blackScreen.gameObject.SetActive(true);
        blackScreen.transform.SetAsLastSibling();
        blackScreen.blocksRaycasts = true;
        IsPlaying = true;
        onStarted.Invoke();
        yield return FadeTo(1f, Mathf.Max(0f, duration));
        onBlack.Invoke();
        atBlack?.Invoke();
    }

    public IEnumerator FadeInRoutine(float duration)
    {
        if (!isActiveAndEnabled || blackScreen == null) yield break;
        IsPlaying = true;
        yield return FadeTo(0f, Mathf.Max(0f, duration));
        blackScreen.blocksRaycasts = false;
        IsPlaying = false;
        onFinished.Invoke();
    }
    private IEnumerator FadeTo(float target, float duration)
    {
        float from = blackScreen.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackScreen.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        blackScreen.alpha = target;
    }

    public void CancelCutscene()
    {
        StopAllCoroutines();
        IsPlaying = false;
        if (blackScreen == null) return;
        blackScreen.alpha = 0f;
        blackScreen.blocksRaycasts = false;
    }

    private void OnDisable() { CancelCutscene(); }
}