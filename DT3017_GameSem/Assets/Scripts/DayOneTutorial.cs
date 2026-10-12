using System.Collections;
using DialogueEditor;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Day 1: introduction, fade, folding quota, then sleep to finish.</summary>
[DisallowMultipleComponent]
public class DayOneTutorial : MonoBehaviour
{
    public enum TutorialStage { NotStarted, Introduction, Fade, Folding, GoToBed, Completed }
    [Header("Systems")]
    [SerializeField] private DayCycleManager dayCycle;
    [SerializeField] private DailyQuotaManager quota;
    [SerializeField] private PlayerTutorialPosition tutorialPosition;
    [SerializeField] private FirstPersonController player;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private ScreenFadeCutscene fadeCutscene;
    [SerializeField] private TutorialHUD hud;
    [Header("Tutorial Points")]
    [SerializeField] private Transform tutorialPoint;
    [SerializeField] private Transform tutorialNpcPoint;
    [Tooltip("Look at this point on the workbench while the screen is black. If empty, use Tutorial Point's facing.")]
    [SerializeField] private Transform workbenchLookTarget;
    [SerializeField] private Transform npc;
    [Tooltip("NPC root offset from the marker. Use an upward offset if the marker is at floor level.")]
    [SerializeField] private Vector3 npcPositionOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private NPCConversation introduction;
    [Header("Optional Boss Pause")]
    [SerializeField] private BossController boss;
    [SerializeField] private bool pauseBossDuringTutorial = true;
    [Header("Escape Routes")]
    [Tooltip("Every unique, non-empty route in this list must be inspected after the quota is met, before sleeping on Day 1.")]
    [SerializeField] private System.Collections.Generic.List<EscapeRouteCheck> escapeRoutes =
        new System.Collections.Generic.List<EscapeRouteCheck>();
    private readonly System.Collections.Generic.HashSet<EscapeRouteCheck> requiredRoutes =
        new System.Collections.Generic.HashSet<EscapeRouteCheck>();
    private readonly System.Collections.Generic.HashSet<EscapeRouteCheck> checkedRoutes =
        new System.Collections.Generic.HashSet<EscapeRouteCheck>();
    private readonly System.Collections.Generic.List<NPC_ConversationStart> lockedNpcs =
        new System.Collections.Generic.List<NPC_ConversationStart>();
    public int CheckedEscapeRouteCount => checkedRoutes.Count;
    public int TotalEscapeRouteCount => requiredRoutes.Count;
    public bool AllEscapeRoutesChecked => checkedRoutes.Count >= requiredRoutes.Count;
    [Header("Events")]
    [SerializeField] private UnityEvent onTutorialStarted = new UnityEvent();
    [SerializeField] private UnityEvent onIntroductionFinished = new UnityEvent();
    [SerializeField] private UnityEvent onQuotaMet = new UnityEvent();
    [SerializeField] private UnityEvent onTutorialCompleted = new UnityEvent();
    [Header("Runtime State")]
    [SerializeField] private TutorialStage stage;
    public TutorialStage Stage => stage;
    public bool IsActive => stage != TutorialStage.NotStarted && stage != TutorialStage.Completed;
    public bool CanSleep => !isActiveAndEnabled || !IsActive || (stage == TutorialStage.GoToBed && AllEscapeRoutesChecked);
    private Vector3 originalNpcPosition;
    private Quaternion originalNpcRotation;
    private Rigidbody npcBody;
    private bool originalNpcKinematic;
    private bool originalBossPaused;
    private bool npcPlaced;
    private bool introductionStarted;

    private void OnEnable()
    {
        ConversationManager.OnConversationEnded += OnConversationEnded;
        requiredRoutes.Clear();
        foreach (var route in escapeRoutes)
            if (route != null && requiredRoutes.Add(route)) route.Checked += OnEscapeRouteChecked;
        if (quota != null) quota.QuotaChanged += OnQuotaChanged;
        if (dayCycle != null) dayCycle.DayStarted += OnDayStarted;
    }

    private IEnumerator Start()
    {
        // Wait for inventory, quota and dialogue Start/Awake initialization.
        yield return null;
        if (dayCycle != null && dayCycle.CurrentDay == 1) BeginTutorial();
    }

    public void BeginTutorial()
    {
        if (!isActiveAndEnabled || stage != TutorialStage.NotStarted || dayCycle == null || dayCycle.CurrentDay != 1) return;
        if (quota == null || tutorialPosition == null || player == null || tutorialPoint == null || npc == null || tutorialNpcPoint == null || introduction == null || fadeCutscene == null || ConversationManager.Instance == null)
        {
            Debug.LogError("DayOneTutorial is missing required scene references.", this);
            return;
        }
        checkedRoutes.Clear();
        stage = TutorialStage.Introduction;
        tutorialPosition.BeginTutorialAt(tutorialPoint);
        // Keep looking available while folding; dialogue itself handles its look lock.
        interactor?.LockInteraction(this);
        PlaceNpc();
        var npcRenderer = npc.GetComponentInChildren<Renderer>();
        player.FaceWorldPosition(npcRenderer != null ? npcRenderer.bounds.center : npc.position);
        if (pauseBossDuringTutorial && boss != null)
        {
            originalBossPaused = boss.IsSequencePaused;
            boss.SetSequencePaused(true);
        }
        onTutorialStarted.Invoke();
        StartCoroutine(StartIntroduction());
    }

    private IEnumerator StartIntroduction()
    {
        while (ConversationManager.Instance.IsConversationActive) yield return null;
        introductionStarted = true;
        if (DialogueVariableBridge.Instance != null) DialogueVariableBridge.Instance.StartConversation(introduction);
        else ConversationManager.Instance.StartConversation(introduction);
    }

    private void PlaceNpc()
    {
        originalNpcPosition = npc.position;
        originalNpcRotation = npc.rotation;
        npcBody = npc.GetComponent<Rigidbody>();
        if (npcBody != null)
        {
            originalNpcKinematic = npcBody.isKinematic;
            if (!npcBody.isKinematic) { npcBody.linearVelocity = Vector3.zero; npcBody.angularVelocity = Vector3.zero; }
            npcBody.isKinematic = true;
        }
        Vector3 position = tutorialNpcPoint.position + npcPositionOffset;
        Vector3 facing = player.transform.position - position;
        facing.y = 0f;
        Quaternion rotation = facing.sqrMagnitude > .001f ? Quaternion.LookRotation(facing) : tutorialNpcPoint.rotation;
        npc.SetPositionAndRotation(position, rotation);
        if (npcBody != null) { npcBody.position = position; npcBody.rotation = rotation; }
        Physics.SyncTransforms();
        npcPlaced = true;
    }

    private void OnConversationEnded()
    {
        if (stage != TutorialStage.Introduction || !introductionStarted) return;
        introductionStarted = false;
        stage = TutorialStage.Fade;
        player.LockMovement(this, true);
        onIntroductionFinished.Invoke();
        StartCoroutine(AfterIntroduction());
    }

    private IEnumerator AfterIntroduction()
    {
        while (fadeCutscene.IsPlaying) yield return null;
        yield return fadeCutscene.PlayRoutine(PrepareWorkbenchView);
        if (stage != TutorialStage.Fade) yield break;
        player.UnlockMovement(this);
        interactor?.UnlockInteraction(this);
        stage = TutorialStage.Folding;
        OnQuotaChanged();
    }

    private void PrepareWorkbenchView()
    {
        RestoreNpc();
        tutorialPosition.BeginTutorialAt(tutorialPoint);
        if (workbenchLookTarget != null) player.FaceWorldPosition(workbenchLookTarget.position);
    }

    private void OnQuotaChanged()
    {
        if (stage != TutorialStage.Folding || !quota.HasMetQuota) return;
        stage = TutorialStage.GoToBed;
        tutorialPosition.EndTutorial();
        foreach (var npcInteraction in FindObjectsByType<NPC_ConversationStart>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            npcInteraction.LockInteraction(this);
            lockedNpcs.Add(npcInteraction);
        }
        hud?.ShowBedObjective();
        onQuotaMet.Invoke();
    }

    private void OnEscapeRouteChecked(EscapeRouteCheck route)
    {
        if (stage != TutorialStage.GoToBed || !requiredRoutes.Contains(route) || !checkedRoutes.Add(route)) return;
        hud?.ShowEscapeRouteProgress(CheckedEscapeRouteCount, TotalEscapeRouteCount);
    }

    public bool TryAllowSleep()
    {
        if (CanSleep) return true;
        if (stage == TutorialStage.GoToBed && !AllEscapeRoutesChecked)
            hud?.ShowMessage("I should check out the room first");
        return false;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public bool CanSkipTutorial => isActiveAndEnabled && dayCycle != null &&
        dayCycle.CurrentDay == 1 && stage != TutorialStage.Completed;

    public void SkipTutorial()
    {
        if (!CanSkipTutorial) return;
        bool endIntroduction = stage == TutorialStage.Introduction && introductionStarted;
        if (IsActive) Cleanup();
        else StopAllCoroutines();
        introductionStarted = false;
        stage = TutorialStage.Completed;
        // End only this tutorial's dialogue, without triggering its normal fade.
        if (endIntroduction && ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            ConversationManager.Instance.EndConversation();
    }
#endif

    private void OnDayStarted(int day)
    {
        if (!IsActive || day <= 1) return;
        stage = TutorialStage.Completed;
        Cleanup();
        onTutorialCompleted.Invoke();
    }

    private void RestoreNpc()
    {
        if (!npcPlaced || npc == null) return;
        npc.SetPositionAndRotation(originalNpcPosition, originalNpcRotation);
        if (npcBody != null)
        {
            npcBody.position = originalNpcPosition;
            npcBody.rotation = originalNpcRotation;
            npcBody.isKinematic = originalNpcKinematic;
        }
        Physics.SyncTransforms();
        npcPlaced = false;
    }

    private void Cleanup()
    {
        StopAllCoroutines();
        if (stage == TutorialStage.Fade) fadeCutscene?.CancelCutscene();
        tutorialPosition?.EndTutorial();
        player?.UnlockMovement(this);
        interactor?.UnlockInteraction(this);
        hud?.Hide();
        foreach (var npcInteraction in lockedNpcs)
            if (npcInteraction != null) npcInteraction.UnlockInteraction(this);
        lockedNpcs.Clear();
        RestoreNpc();
        if (pauseBossDuringTutorial && boss != null) boss.SetSequencePaused(originalBossPaused);
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationEnded -= OnConversationEnded;
        foreach (var route in requiredRoutes)
            if (route != null) route.Checked -= OnEscapeRouteChecked;
        if (quota != null) quota.QuotaChanged -= OnQuotaChanged;
        if (dayCycle != null) dayCycle.DayStarted -= OnDayStarted;
        if (IsActive) Cleanup();
    }
}