using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Queues unlocks so simultaneous achievements never replace each other.</summary>
public sealed class AchievementToast : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private UnityEngine.UI.RawImage icon;
    [SerializeField, Min(.1f)] private float displaySeconds = 4.5f;
    private readonly Queue<AchievementDefinition> pending = new Queue<AchievementDefinition>();
    private Coroutine routine;

    private void Awake() { panel.alpha = 0; panel.blocksRaycasts = false; panel.interactable = false; }
    private void OnEnable() { AchievementManager.Unlocked += Enqueue; }
    private void OnDisable()
    {
        AchievementManager.Unlocked -= Enqueue;
        StopAllCoroutines(); routine = null; pending.Clear();
        if (panel != null) panel.alpha = 0;
    }
    private void Enqueue(AchievementDefinition achievement)
    {
        pending.Enqueue(achievement);
        if (routine == null) routine = StartCoroutine(ShowPending());
    }
    private IEnumerator ShowPending()
    {
        while (pending.Count > 0)
        {
            var achievement = pending.Dequeue();
            titleText.text = achievement.title;
            descriptionText.text = achievement.description;
            icon.texture = achievement.icon;
            icon.enabled = achievement.icon != null;
            yield return Fade(0, 1, .2f);
            yield return new WaitForSecondsRealtime(displaySeconds);
            yield return Fade(1, 0, .25f);
        }
        routine = null;
    }
    private IEnumerator Fade(float from, float to, float seconds)
    {
        for (float elapsed = 0; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            panel.alpha = Mathf.Lerp(from, to, elapsed / seconds);
            yield return null;
        }
        panel.alpha = to;
    }
}