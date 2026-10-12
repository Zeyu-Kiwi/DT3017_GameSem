using UnityEngine;

[CreateAssetMenu(fileName = "DayPlan", menuName = "Game/Day Plan")]
public class DayPlan : ScriptableObject
{
    public enum PlayerSpawnLocation
    {
        Bed,
        Workstation
    }

    [Header("Day")]
    [SerializeField, Min(1)] private int dayNumber = 1;
    [SerializeField] private PlayerSpawnLocation playerSpawn = PlayerSpawnLocation.Bed;

    [Header("Systems")]
    [SerializeField] private bool timerEnabled = true;
    [SerializeField] private bool overrideTimerDuration;
    [SerializeField, Min(0f)] private float timerDurationSeconds = 600f;
    [SerializeField] private bool bossEnabled = true;
    [SerializeField] private bool drawerEnabled = true;
    [SerializeField] private bool npcInteractionEnabled = true;
    [SerializeField] private bool bedIndicatorVisible;

    [Header("Special Flow")]
    [Tooltip("Use the workstation quota tutorial configured on DayPlanManager.")]
    [SerializeField] private bool workstationQuotaTutorial;

    public int DayNumber => Mathf.Max(1, dayNumber);
    public PlayerSpawnLocation SpawnLocation => playerSpawn;
    public bool TimerEnabled => timerEnabled;
    public bool OverrideTimerDuration => overrideTimerDuration;
    public float TimerDurationSeconds => Mathf.Max(0f, timerDurationSeconds);
    public bool BossEnabled => bossEnabled;
    public bool DrawerEnabled => drawerEnabled;
    public bool NpcInteractionEnabled => npcInteractionEnabled;
    public bool BedIndicatorVisible => bedIndicatorVisible;
    public bool WorkstationQuotaTutorial => workstationQuotaTutorial;

    private void OnValidate()
    {
        dayNumber = Mathf.Max(1, dayNumber);
        timerDurationSeconds = Mathf.Max(0f, timerDurationSeconds);
    }
}
