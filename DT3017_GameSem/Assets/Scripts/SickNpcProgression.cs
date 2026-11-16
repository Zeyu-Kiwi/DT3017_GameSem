using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Tracks total days helped and consecutive days without help.</summary>
[DisallowMultipleComponent]
public class SickNpcProgression : MonoBehaviour
{
    [Header("Day")]
    [SerializeField] private DayCycleManager dayCycleManager;
    [SerializeField, Min(1)] private int firstDayToCheck = 2;

    [Header("Existing Game Variables")]
    [Tooltip("Existing HelpSickStreak variable, now used as the total number of days helped.")]
    [SerializeField] private GameIntData helpSickStreak;
    [SerializeField] private GameIntData noHelpSickStreak;
    [SerializeField] private GameFlagData helpedSick;
    [SerializeField] private GameFlagData isSickGone;

    [Header("Limits")]
    [SerializeField, Min(1)] private int daysWithoutHelpBeforeGone = 3;
    [FormerlySerializedAs("consecutiveHelpDaysToSave")]
    [SerializeField, Min(1)] private int totalHelpDaysToSave = 3;

    [Header("Objects")]
    [SerializeField] private GameObject npcObject;
    [Tooltip("The workstation conversation object. Keep it inactive until the NPC leaves.")]
    [SerializeField] private GameObject emptyWorkstationConversation;

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool permanentlySaved;

    private GameStateManager state;
    private bool started;
    private bool subscribed;

    private void Start()
    {
        started = true;
        Subscribe();
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        // Keep the save permanent for this session even if a developer later edits the int.
        permanentlySaved = state.GetInt(helpSickStreak) >= totalHelpDaysToSave;
        ApplyGoneState();
    }

    private void OnEnable()
    {
        if (started) Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        if (dayCycleManager == null)
            dayCycleManager = FindFirstObjectByType<DayCycleManager>();
        state = GameStateManager.Instance;
        if (dayCycleManager == null || state == null) return;

        dayCycleManager.DayEnding += OnDayEnding;
        dayCycleManager.DayStarted += OnDayStarted;
        state.IntChanged += OnIntChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        dayCycleManager.DayEnding -= OnDayEnding;
        dayCycleManager.DayStarted -= OnDayStarted;
        state.IntChanged -= OnIntChanged;
        subscribed = false;
    }

    private bool ValidateSetup()
    {
        if (dayCycleManager != null && state != null &&
            helpSickStreak != null && noHelpSickStreak != null &&
            helpedSick != null && isSickGone != null &&
            npcObject != null && emptyWorkstationConversation != null &&
            npcObject != emptyWorkstationConversation)
            return true;

        Debug.LogError("SickNpcProgression needs a DayCycleManager, GameStateManager, all four existing game variables, and two different scene objects.", this);
        return false;
    }

    private void OnIntChanged(GameIntData variable, int value)
    {
        if (variable == helpSickStreak && value >= totalHelpDaysToSave)
            permanentlySaved = true;
    }

    private void OnDayEnding(int endedDay)
    {
        if (endedDay < firstDayToCheck || state.GetFlag(isSickGone)) return;

        // The dialogue sets HelpedSick once. Count the help at day end so the
        // updated total changes dialogue only on the following day.
        if (state.GetFlag(helpedSick))
        {
            state.AddInt(helpSickStreak, 1);
            if (state.GetInt(helpSickStreak) >= totalHelpDaysToSave)
                permanentlySaved = true;
            state.SetInt(noHelpSickStreak, 0);
            state.SetFlag(helpedSick, false);
            return;
        }

        if (state.GetInt(helpSickStreak) >= totalHelpDaysToSave)
            permanentlySaved = true;
        state.AddInt(noHelpSickStreak, 1);

        if (!permanentlySaved &&
            state.GetInt(noHelpSickStreak) >= daysWithoutHelpBeforeGone)
        {
            state.SetFlag(isSickGone, true);
            ApplyGoneState();
        }
    }

    private void OnDayStarted(int day)
    {
        // Developer day jumps do not count skipped days as missed help days.
        ApplyGoneState();
    }

    private void ApplyGoneState()
    {
        if (state == null || !state.GetFlag(isSickGone)) return;
        npcObject.SetActive(false);
        emptyWorkstationConversation.SetActive(true);
    }

    private void OnValidate()
    {
        firstDayToCheck = Mathf.Max(1, firstDayToCheck);
        daysWithoutHelpBeforeGone = Mathf.Max(1, daysWithoutHelpBeforeGone);
        totalHelpDaysToSave = Mathf.Max(1, totalHelpDaysToSave);
    }
}
