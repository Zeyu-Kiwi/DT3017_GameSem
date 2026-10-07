using TMPro;
using UnityEngine;

// Displays the DayCycleManager countdown using UI designed in the scene/prefab.
[DisallowMultipleComponent]
public sealed class DailyTimerHUD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("May be left empty; the script will find the scene's DayCycleManager at runtime.")]
    [SerializeField] private DayCycleManager dayManager;
    [Tooltip("The artist-created timer panel. Place this script on an always-active parent, not on this object.")]
    [SerializeField] private GameObject interfaceRoot;
    [SerializeField] private TMP_Text timeText;

    private int displayedSeconds = -1;

    private void Awake()
    {
        FindDayManagerIfNeeded();
        RefreshDisplay(true);
    }

    private void OnEnable()
    {
        displayedSeconds = -1;
    }

    private void LateUpdate()
    {
        FindDayManagerIfNeeded();
        RefreshDisplay(false);
    }

    private void FindDayManagerIfNeeded()
    {
        if (dayManager == null)
        {
            dayManager = FindFirstObjectByType<DayCycleManager>();
        }
    }

    private void RefreshDisplay(bool forceTextRefresh)
    {
        bool visible = dayManager != null && dayManager.isActiveAndEnabled &&
                       dayManager.IsDailyTimerEnabled && !dayManager.TransitionRunning;

        if (interfaceRoot != null && interfaceRoot.activeSelf != visible)
        {
            interfaceRoot.SetActive(visible);
        }

        if (!visible || timeText == null)
        {
            return;
        }

        int seconds = Mathf.CeilToInt(Mathf.Max(0f, dayManager.RemainingDayTimeSeconds));
        if (!forceTextRefresh && seconds == displayedSeconds)
        {
            return;
        }

        displayedSeconds = seconds;
        timeText.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }
}
