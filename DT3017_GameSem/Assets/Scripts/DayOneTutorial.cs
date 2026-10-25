using System.Collections;
using DialogueEditor;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Holds the player at the Day 1 workstation until the shirt quota is met.
/// Dialogue itself owns the temporary movement lock.
/// </summary>
[DisallowMultipleComponent]
public class DayOneTutorial : MonoBehaviour
{
    public enum TutorialStage { Inactive, StartingConversation, Folding, Completed }
    private enum ConversationReason { None, Introduction, LeftWorkstation, DrawerAttempt }

    [Header("Systems")]
    [SerializeField] private DayCycleManager dayCycle;
    [SerializeField] private DailyQuotaManager quota;
    [SerializeField] private PlayerTutorialPosition tutorialPosition;
    [SerializeField] private FirstPersonController player;
    [SerializeField] private CraftingStation craftingStation;
    [SerializeField] private BossController boss;

    [Header("Positions")]
    [Tooltip("Player root position used at the start and after each violation.")]
    [SerializeField] private Transform tutorialPoint;
    [Tooltip("Must be part of the boss locator network so the boss can walk to the exit.")]
    [SerializeField] private BossPatrolLocator bossStareLocator;
    [Tooltip("Optional target for the boss to face. The player is used when empty.")]
    [SerializeField] private Transform bossLookTarget;

    [Header("Conversations")]
    [SerializeField] private NPCConversation startingConversation;
    [SerializeField] private NPCConversation leftWorkstationConversation;
    [SerializeField] private NPCConversation drawerAttemptConversation;

    [Header("Penalty")]
    [SerializeField, Min(0)] private int violationQuotaPenalty = 3;

    [Header("Events")]
    [SerializeField] private UnityEvent onTutorialStarted = new UnityEvent();
    [SerializeField] private UnityEvent onQuotaMet = new UnityEvent();
    [SerializeField] private UnityEvent onTutorialCompleted = new UnityEvent();

    [Header("Runtime State (Debug)")]
    [SerializeField] private TutorialStage stage = TutorialStage.Inactive;
    [SerializeField] private bool planEnabled;
    [SerializeField] private bool playerHasEnteredWorkstation;
    [SerializeField] private bool violationInProgress;

    private ConversationReason activeConversation;
    private Coroutine conversationRoutine;
    private DailyQuotaManager subscribedQuota;

    public TutorialStage Stage => stage;
    public bool IsActive => planEnabled && stage != TutorialStage.Inactive &&
                            stage != TutorialStage.Completed;
    public bool IsRestrictionActive => IsActive;
    public bool CanSkipTutorial => IsActive;

    private void Awake()
    {
        if (dayCycle == null) dayCycle = GetComponent<DayCycleManager>();
        if (quota == null) quota = DailyQuotaManager.Instance;
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        if (tutorialPosition == null && player != null)
            tutorialPosition = player.GetComponent<PlayerTutorialPosition>();
        if (craftingStation == null) craftingStation = FindFirstObjectByType<CraftingStation>();
        if (boss == null) boss = FindFirstObjectByType<BossController>();
    }

    private void OnEnable()
    {
        ConversationManager.OnConversationEnded += OnConversationEnded;
        SubscribeToQuota();
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationEnded -= OnConversationEnded;
        if (subscribedQuota != null)
            subscribedQuota.QuotaChanged -= OnQuotaChanged;
        subscribedQuota = null;
        StopTutorial(false);
    }

    private void Update()
    {
        if (!IsActive || stage == TutorialStage.StartingConversation ||
            violationInProgress || craftingStation == null) return;

        if (craftingStation.IsPlayerInsideWorkstation)
        {
            playerHasEnteredWorkstation = true;
            return;
        }

        if (playerHasEnteredWorkstation)
        {
            playerHasEnteredWorkstation = false;
            TriggerViolation(ConversationReason.LeftWorkstation,
                leftWorkstationConversation);
        }
    }

    public void SetPlanEnabled(bool enabled)
    {
        if (planEnabled == enabled) return;
        planEnabled = enabled;
        if (enabled) BeginTutorial();
        else StopTutorial(false);
    }

    public void BeginTutorial()
    {
        if (!isActiveAndEnabled || !planEnabled || IsActive) return;
        if (quota == null) quota = DailyQuotaManager.Instance;
        SubscribeToQuota();
        if (quota == null || tutorialPosition == null || tutorialPoint == null ||
            player == null || craftingStation == null || boss == null ||
            bossStareLocator == null)
        {
            Debug.LogError("DayOneTutorial is missing required scene references.", this);
            return;
        }

        stage = TutorialStage.StartingConversation;
        violationInProgress = false;
        playerHasEnteredWorkstation = false;
        tutorialPosition.TeleportWithoutLock(tutorialPoint);
        Physics.SyncTransforms();
        boss.BeginTutorialStare(bossStareLocator,
            bossLookTarget != null ? bossLookTarget : player.transform);
        onTutorialStarted.Invoke();
        BeginConversation(startingConversation, ConversationReason.Introduction);
    }

    public bool TryHandleDrawerAttempt()
    {
        if (!IsRestrictionActive) return false;
        TriggerViolation(ConversationReason.DrawerAttempt,
            drawerAttemptConversation);
        return true;
    }

    private void TriggerViolation(ConversationReason reason,
        NPCConversation conversation)
    {
        if (!IsActive || violationInProgress) return;
        violationInProgress = true;
        quota.AddPenalty(violationQuotaPenalty);
        tutorialPosition.TeleportWithoutLock(tutorialPoint);
        Physics.SyncTransforms();
        BeginConversation(conversation, reason);
    }

    private void BeginConversation(NPCConversation conversation,
        ConversationReason reason)
    {
        if (conversationRoutine != null) StopCoroutine(conversationRoutine);
        conversationRoutine = StartCoroutine(
            StartConversationWhenReady(conversation, reason));
    }

    private IEnumerator StartConversationWhenReady(NPCConversation conversation,
        ConversationReason reason)
    {
        while (ConversationManager.Instance != null &&
               ConversationManager.Instance.IsConversationActive)
            yield return null;

        conversationRoutine = null;
        if (!IsActive) yield break;
        if (conversation == null || ConversationManager.Instance == null)
        {
            Debug.LogWarning("A Day 1 tutorial conversation is not assigned.", this);
            FinishConversationReason(reason);
            yield break;
        }

        activeConversation = reason;
        if (DialogueVariableBridge.Instance != null)
            DialogueVariableBridge.Instance.StartConversation(conversation);
        else
            ConversationManager.Instance.StartConversation(conversation);
    }

    private void OnConversationEnded()
    {
        if (activeConversation == ConversationReason.None) return;
        ConversationReason finished = activeConversation;
        activeConversation = ConversationReason.None;
        FinishConversationReason(finished);
    }

    private void FinishConversationReason(ConversationReason reason)
    {
        if (!IsActive) return;
        if (reason == ConversationReason.Introduction)
            stage = TutorialStage.Folding;
        else
            violationInProgress = false;

        playerHasEnteredWorkstation = craftingStation != null &&
                                      craftingStation.IsPlayerInsideWorkstation;
        OnQuotaChanged();
    }

    private void OnQuotaChanged()
    {
        if (!IsActive || quota == null || !quota.HasMetQuota) return;
        stage = TutorialStage.Completed;
        violationInProgress = false;
        onQuotaMet.Invoke();
        boss?.EndTutorialStareAndLeave();
        onTutorialCompleted.Invoke();
    }

    private void SubscribeToQuota()
    {
        if (subscribedQuota == quota) return;
        if (subscribedQuota != null)
            subscribedQuota.QuotaChanged -= OnQuotaChanged;
        subscribedQuota = quota;
        if (subscribedQuota != null)
            subscribedQuota.QuotaChanged += OnQuotaChanged;
    }

    private void StopTutorial(bool makeCompleted)
    {
        if (conversationRoutine != null)
        {
            StopCoroutine(conversationRoutine);
            conversationRoutine = null;
        }
        activeConversation = ConversationReason.None;
        violationInProgress = false;
        playerHasEnteredWorkstation = false;
        boss?.CancelTutorialStare();
        stage = makeCompleted ? TutorialStage.Completed : TutorialStage.Inactive;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void SkipTutorial()
    {
        if (!CanSkipTutorial) return;
        OnQuotaChangedForced();
    }

    private void OnQuotaChangedForced()
    {
        if (!IsActive) return;
        // Cancel waiting dialogue before the end event can notify any tutorial callbacks.
        if (conversationRoutine != null)
        {
            StopCoroutine(conversationRoutine);
            conversationRoutine = null;
        }
        activeConversation = ConversationReason.None;
        stage = TutorialStage.Completed;
        violationInProgress = false;
        playerHasEnteredWorkstation = false;
        foreach (var conversation in FindObjectsByType<ConversationManager>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            conversation.EndConversationImmediately();
        boss?.EndTutorialStareAndLeave();
        onTutorialCompleted.Invoke();
    }

    public void ResetForDevelopment()
    {
        StopTutorial(false);
        planEnabled = false;
    }
#endif

    private void OnValidate()
    {
        violationQuotaPenalty = Mathf.Max(0, violationQuotaPenalty);
    }
}
