using System.Collections;
using TMPro;
using UnityEngine;

/// Put on a TextMeshPro text object. Each time it becomes active,
/// it types out the text assigned before the panel is shown.
[RequireComponent(typeof(TMP_Text))]
public class TypewriterText : MonoBehaviour
{
    [SerializeField, Min(1f)] private float charactersPerSecond = 20f;
    [SerializeField, Min(0f)] private float startDelay = 0.5f;
    [SerializeField] private bool allowSkip = true;

    [Header("Optional")]
    [SerializeField] private AudioSource typeAudio;
    [SerializeField] private AudioClip typeClip;
    [Tooltip("Hidden until typing finishes, e.g. a menu button.")]
    [SerializeField] private GameObject[] revealAfterTyping;

    private TMP_Text text;

    public void SetText(string message)
    {
        if (text == null) text = GetComponent<TMP_Text>();
        text.text = message ?? string.Empty;
        if (isActiveAndEnabled) RestartTyping();
    }

    private void OnEnable()
    {
        if (text == null) text = GetComponent<TMP_Text>();

        RestartTyping();
    }

    private void RestartTyping()
    {
        StopAllCoroutines();

        if (revealAfterTyping != null)
            foreach (GameObject go in revealAfterTyping)
                if (go != null) go.SetActive(false);

        StartCoroutine(Type());
    }

    private IEnumerator Type()
    {
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();
        int total = text.textInfo.characterCount;

        if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay);

        float shown = 0f;
        int lastShown = 0;
        while (lastShown < total)
        {
            if (allowSkip && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                shown = total;
            else
                shown += Mathf.Max(1f, charactersPerSecond) * Time.unscaledDeltaTime;

            int visible = Mathf.Min(total, Mathf.FloorToInt(shown));
            if (visible != lastShown)
            {
                lastShown = visible;
                text.maxVisibleCharacters = visible;
                if (typeAudio != null && typeClip != null)
                    typeAudio.PlayOneShot(typeClip);
            }
            yield return null;
        }

        text.maxVisibleCharacters = total;

        if (revealAfterTyping != null)
            foreach (GameObject go in revealAfterTyping)
                if (go != null) go.SetActive(true);
    }
}
