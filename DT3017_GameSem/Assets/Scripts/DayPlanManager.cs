using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DayPlanManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private DayCycleManager dayCycle;
    [SerializeField] private BossController boss;
    [SerializeField] private CraftingStation craftingStation;
    [SerializeField] private DayOneTutorial workstationTutorial;
    [SerializeField] private TutorialHUD bedIndicator;

    [Header("Spawn Points")]
    [SerializeField] private Transform workstationSpawnPoint;
    [SerializeField] private Transform bedSpawnPoint;

    [Header("Plans")]
    [Tooltip("The first plan matching the current day is used.")]
    [SerializeField] private List<DayPlan> dayPlans = new List<DayPlan>();
    [Tooltip("Used for days that do not have their own plan. Leave empty to use normal gameplay defaults.")]
    [SerializeField] private DayPlan defaultPlan;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("Developer")]
    [SerializeField, Min(1)] private int developerJumpDay = 1;
#endif

    private readonly List<NPC_ConversationStart> lockedNpcs =
        new List<NPC_ConversationStart>();
    private DayPlan currentPlan;

    public DayPlan CurrentPlan => currentPlan;
    public string CurrentPlanName => currentPlan != null ? currentPlan.name : "Default gameplay";

    private void Awake()
    {
        if (dayCycle == null) dayCycle = GetComponent<DayCycleManager>();
        if (dayCycle == null) dayCycle = FindFirstObjectByType<DayCycleManager>();
        if (boss == null) boss = FindFirstObjectByType<BossController>();
        if (craftingStation == null) craftingStation = FindFirstObjectByType<CraftingStation>();
        if (workstationTutorial == null) workstationTutorial = GetComponent<DayOneTutorial>();
    }

    private void OnEnable()
    {
        if (dayCycle != null) dayCycle.DayStarted += ApplyDay;
    }

    private IEnumerator Start()
    {
        // Let every system finish its own Start initialization first.
        yield return null;
        if (dayCycle != null) ApplyDay(dayCycle.CurrentDay);
    }

    private void OnDisable()
    {
        if (dayCycle != null) dayCycle.DayStarted -= ApplyDay;
        UnlockNpcs();
    }

    public DayPlan GetPlanForDay(int day)
    {
        for (int i = 0; i < dayPlans.Count; i++)
        {
            if (dayPlans[i] != null && dayPlans[i].DayNumber == day)
                return dayPlans[i];
        }

        return defaultPlan;
    }

    public Transform GetSpawnPointForDay(int day, Transform fallback)
    {
        DayPlan plan = GetPlanForDay(day);
        if (plan == null) return fallback;
        Transform configured = plan.SpawnLocation == DayPlan.PlayerSpawnLocation.Workstation
            ? workstationSpawnPoint
            : bedSpawnPoint;
        return configured != null ? configured : fallback;
    }

    public void ApplyDay(int day)
    {
        currentPlan = GetPlanForDay(day);

        if (dayCycle != null)
        {
            if (currentPlan != null)
                dayCycle.ApplyDayPlanTimer(currentPlan.TimerEnabled,
                    currentPlan.OverrideTimerDuration,
                    currentPlan.TimerDurationSeconds);
            else
                dayCycle.ResetDailyTimer();
        }

        bool bossEnabled = currentPlan == null || currentPlan.BossEnabled;
        if (boss != null)
        {
            if (!bossEnabled && boss.gameObject.activeSelf)
                boss.ResetOutsideAndRestartTimer();
            boss.gameObject.SetActive(bossEnabled);
        }

        craftingStation?.SetDayPlanInteractionEnabled(
            currentPlan == null || currentPlan.DrawerEnabled);
        SetNpcInteractionEnabled(
            currentPlan == null || currentPlan.NpcInteractionEnabled);
        bedIndicator?.SetBedIndicatorVisible(
            currentPlan != null && currentPlan.BedIndicatorVisible);

        workstationTutorial?.SetPlanEnabled(
            currentPlan != null && currentPlan.WorkstationQuotaTutorial);
    }

    private void SetNpcInteractionEnabled(bool enabled)
    {
        if (enabled)
        {
            UnlockNpcs();
            return;
        }

        UnlockNpcs();
        foreach (NPC_ConversationStart npc in FindObjectsByType<NPC_ConversationStart>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            npc.LockInteraction(this);
            lockedNpcs.Add(npc);
        }
    }

    private void UnlockNpcs()
    {
        for (int i = 0; i < lockedNpcs.Count; i++)
            if (lockedNpcs[i] != null) lockedNpcs[i].UnlockInteraction(this);
        lockedNpcs.Clear();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("Developer/Jump To Configured Day")]
    public void JumpToDeveloperDay()
    {
        dayCycle?.SetDayForDevelopment(Mathf.Max(1, developerJumpDay));
    }

    [ContextMenu("Developer/Restart Current Plan")]
    public void RestartCurrentPlan()
    {
        if (dayCycle == null) return;
        workstationTutorial?.ResetForDevelopment();
        ApplyDay(dayCycle.CurrentDay);
    }
#endif
}
