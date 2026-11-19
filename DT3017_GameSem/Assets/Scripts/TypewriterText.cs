using System.Collections;
using TMPro;
using UnityEngine;

/// Put on a TextMeshPro text object. Each time it becomes active
/// (e.g. when the end game panel is shown), it types out its own text.
[RequireComponent(typeof(TMP_Text))]
public class TypewriterText : MonoBehaviour
{
    [SerializeField] private float charactersPerSecond = 20f;
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private bool allowSkip = true;

    [Header("Optional")]
    [SerializeField] private AudioSource typeAudio;
    [SerializeField] private AudioClip typeClip;
    [Tooltip("Hidden until typing finishes, e.g. a menu button.")]
    [SerializeField] private GameObject[] revealAfterTyping;

    private TMP_Text text;

    private void OnEnable()
    {
        if (text == null) text = GetComponent<TMP_Text>();

        foreach (GameObject go in revealAfterTyping)
            if (go != null) go.SetActive(false);

        StartCoroutine(Type());
    }

    private IEnumerator Type()
    {
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();
        int total = text.textInfo.characterCount;

        yield return new WaitForSecondsRealtime(startDelay);

        float shown = 0f;
        int lastShown = 0;
        while (lastShown < total)
        {
            if (allowSkip && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                shown = total;
            else
                shown += charactersPerSecond * Time.unscaledDeltaTime;

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

        foreach (GameObject go in revealAfterTyping)
            if (go != null) go.SetActive(true);
    }
}