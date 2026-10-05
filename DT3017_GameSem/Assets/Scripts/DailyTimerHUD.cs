using TMPro;
using UnityEngine;

// Created by DayCycleManager in every gameplay scene, including release builds.
[DisallowMultipleComponent]
[RequireComponent(typeof(DayCycleManager))]
public sealed class DailyTimerHUD : MonoBehaviour
{
    private DayCycleManager dayManager;
    private GameObject hud;
    private TMP_Text timeText;
    private int displayedSeconds = -1;

    private void Awake()
    {
        dayManager = GetComponent<DayCycleManager>();
        hud = new GameObject("Daily Timer HUD", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler));
        hud.transform.SetParent(transform, false);
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = hud.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;

        var panel = new GameObject("Countdown", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var rect = panel.GetComponent<RectTransform>();
        rect.SetParent(hud.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-16, -16);
        rect.sizeDelta = new Vector2(94, 34);
        var background = panel.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(0f, 0f, 0f, .6f);
        background.raycastTarget = false;

        var label = new GameObject("Time Left", typeof(RectTransform), typeof(TextMeshProUGUI));
        timeText = label.GetComponent<TextMeshProUGUI>();
        timeText.rectTransform.SetParent(rect, false);
        timeText.rectTransform.anchorMin = Vector2.zero;
        timeText.rectTransform.anchorMax = Vector2.one;
        timeText.rectTransform.offsetMin = new Vector2(6, 2);
        timeText.rectTransform.offsetMax = new Vector2(-6, -2);
        timeText.font = TMP_Settings.defaultFontAsset;
        timeText.fontSize = 22;
        timeText.color = Color.white;
        timeText.alignment = TextAlignmentOptions.Center;
        timeText.raycastTarget = false;
        timeText.text = "--:--";
        hud.SetActive(false);
    }

    private void LateUpdate()
    {
        bool visible = dayManager.isActiveAndEnabled && dayManager.IsDailyTimerEnabled &&
                       !dayManager.TransitionRunning;
        if (hud.activeSelf != visible) hud.SetActive(visible);
        if (!visible) return;

        int seconds = Mathf.CeilToInt(Mathf.Max(0f, dayManager.RemainingDayTimeSeconds));
        if (seconds == displayedSeconds) return;
        displayedSeconds = seconds;
        timeText.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }

    private void OnDisable()
    {
        if (hud != null) hud.SetActive(false);
    }

    private void OnDestroy()
    {
        if (hud != null) Destroy(hud);
    }
}