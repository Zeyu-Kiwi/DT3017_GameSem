using UnityEngine;

/// <summary>Tracks consecutive days of helping a specific NPC.</summary>
[DisallowMultipleComponent]
public class SickNpcProgression : MonoBehaviour
{
    [Header("Day")]
    [SerializeField] private DayCycleManager dayCycleManager;
    [SerializeField, Min(1)] private int firstDayToCheck = 2;

    [Header("Existing Game Variables")]
    [SerializeField] private GameIntData helpSickStreak;
    [SerializeField] private GameIntData noHelpSickStreak;
    [SerializeField] private GameFlagData helpedSick;
    [SerializeField] private GameFlagData isSickGone;

    [Header("Limits")]
    [SerializeField, Min(1)] private int daysWithoutHelpBeforeGone = 2;
    [SerializeField, Min(1)] private int consecutiveHelpDaysToSave = 3;

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

        // This latch survives later resets of HelpSickStreak during this session.
        permanentlySaved = state.GetInt(helpSickStreak) >= consecutiveHelpDaysToSave;
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
        if (variable == helpSickStreak && value >= consecutiveHelpDaysToSave)
            permanentlySaved = true;
    }

    private void OnDayEnding(int endedDay)
    {
        if (endedDay < firstDayToCheck || state.GetFlag(isSickGone)) return;

        // The dialogue option already added one to HelpSickStreak. It can only
        // be selected once because HelpedSick stays true until day end.
        if (state.GetFlag(helpedSick))
        {
            if (state.GetInt(helpSickStreak) >= consecutiveHelpDaysToSave)
                permanentlySaved = true;
            state.SetInt(noHelpSickStreak, 0);
            state.SetFlag(helpedSick, false);
            return;
        }

        if (state.GetInt(helpSickStreak) >= consecutiveHelpDaysToSave)
            permanentlySaved = true;
        state.SetInt(helpSickStreak, 0);
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
        consecutiveHelpDaysToSave = Mathf.Max(1, consecutiveHelpDaysToSave);
    }
}
