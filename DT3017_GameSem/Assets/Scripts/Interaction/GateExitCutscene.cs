using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Opens a gate around its authored pivot, walks the player out and holds on black.</summary>
[DisallowMultipleComponent]
public sealed class GateExitCutscene : MonoBehaviour, IInteractable
{
    [Header("Gate")]
    [SerializeField] private Transform gatePivot;
    [SerializeField] private float openAngle = -100f;
    [SerializeField, Min(.01f)] private float openDuration = 1f;
    [Header("Player path (standing root positions)")]
    [SerializeField] private Transform walkStartPoint;
    [SerializeField] private Transform walkExitPoint;
    [SerializeField, Min(.01f)] private float walkSpeed = 1.8f;
    [SerializeField, Min(0f)] private float turnDuration = .4f;
    [Tooltip("Begin fading this many seconds before the walk finishes.")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 1.2f;
    [SerializeField] private ScreenFadeCutscene screenFade;
    [Header("Events")]
    [SerializeField] private UnityEvent onOpened = new UnityEvent();
    [Tooltip("Screen is fully black and control remains locked. Connect the next scene or cutscene here.")]
    [SerializeField] private UnityEvent onReachedBlack = new UnityEvent();
    [Header("Runtime")]
    [SerializeField] private string stage = "Idle";
    public bool IsPlaying { get; private set; }
    public bool HasOpened { get; private set; }
    public bool CanInteract => isActiveAndEnabled && !IsPlaying && !HasOpened;
    public string Stage => stage;

    private FirstPersonController controller;
    private PlayerInteractor interactor;
    private Camera view;
    private Rigidbody body;
    private Collider[] colliders;
    private bool[] colliderStates;
    private HeadBob[] bobs;
    private bool[] bobStates;
    private BossController[] bosses;
    private bool[] bossPauseStates;
    private DayCycleManager dayCycle;
    private bool controllerEnabled, wasKinematic, captured, fading, reachedBlack;
    private Vector3 originalPosition, cameraLocalPosition;
    private Quaternion originalRotation, originalViewRotation, cameraLocalRotation, closedRotation;
    private Coroutine routine;

    public void Interact(GameObject player)
    {
        if (!CanInteract || player == null) return;
        if (gatePivot == null || walkExitPoint == null || screenFade == null || !screenFade.IsConfigured
            || !screenFade.isActiveAndEnabled || screenFade.IsPlaying)
        {
            Debug.LogError("Gate exit requires a pivot, exit point and available screen fade.", this);
            return;
        }
        controller = player.GetComponent<FirstPersonController>();
        body = player.GetComponent<Rigidbody>();
        view = player.GetComponentInChildren<Camera>();
        if (controller == null || body == null || view == null) return;
        IsPlaying = true;
        routine = StartCoroutine(Play(player));
    }

    private IEnumerator Play(GameObject player)
    {
        interactor = player.GetComponent<PlayerInteractor>();
        originalPosition = player.transform.position;
        originalRotation = player.transform.rotation;
        originalViewRotation = view.transform.rotation;
        cameraLocalPosition = view.transform.localPosition;
        cameraLocalRotation = view.transform.localRotation;
        closedRotation = gatePivot.localRotation;
        controllerEnabled = controller.enabled;
        wasKinematic = body.isKinematic;
        colliders = player.GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[colliders.Length];
        bobs = player.GetComponentsInChildren<HeadBob>(true);
        bobStates = new bool[bobs.Length];
        bosses = FindObjectsByType<BossController>(FindObjectsSortMode.None);
        bossPauseStates = new bool[bosses.Length];
        captured = true;
        controller.LockMovement(this, true);
        controller.enabled = false;
        interactor?.LockInteraction(this);
        for (int i=0; i<colliders.Length; i++) { colliderStates[i]=colliders[i].enabled; colliders[i].enabled=false; }
        for (int i=0; i<bobs.Length; i++) { bobStates[i]=bobs[i].enabled; bobs[i].enabled=false; }
        for (int i=0; i<bosses.Length; i++) { bossPauseStates[i]=bosses[i].IsSequencePaused; bosses[i].SetSequencePaused(true); }
        body.isKinematic = true;
        dayCycle = FindFirstObjectByType<DayCycleManager>();
        dayCycle?.SetSequenceTimerPaused(this, true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        stage = "Opening gate";
        Quaternion opened = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            gatePivot.localRotation = Quaternion.Slerp(closedRotation, opened,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(.01f, openDuration))));
            yield return null;
        }
        gatePivot.localRotation = opened;
        HasOpened = true;
        onOpened.Invoke();
        if (walkStartPoint != null && Vector3.Distance(player.transform.position, walkStartPoint.position) > .05f)
            yield return WalkTo(walkStartPoint.position, false);
        yield return WalkTo(walkExitPoint.position, true);
        stage = "Fading out";
        // The fade runs concurrently with walking, then remains opaque until the next sequence.
        while (!reachedBlack) yield return null;
        stage = "Black";
        routine = null;
        onReachedBlack.Invoke();
    }

    private IEnumerator WalkTo(Vector3 destination, bool finishOnBlack)
    {
        stage = "Facing exit";
        Vector3 start = controller.transform.position;
        Vector3 direction = destination - start;
        direction.y = 0f;
        Quaternion facing = direction.sqrMagnitude > .001f ? Quaternion.LookRotation(direction) : controller.transform.rotation;
        Quaternion initialFacing = controller.transform.rotation;
        float elapsed = 0f;
        while (elapsed < turnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Quaternion rotation = Quaternion.Slerp(initialFacing, facing, Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(elapsed / Mathf.Max(.001f, turnDuration))));
            controller.transform.rotation = rotation;
            body.rotation = rotation;
            yield return null;
        }
        controller.transform.rotation = facing;
        body.rotation = facing;
        controller.ResetLook();
        stage = "Walking forward";
        float duration = Vector3.Distance(start, destination) / Mathf.Max(.01f, walkSpeed);
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.001f, duration));
            Vector3 position = Vector3.Lerp(start, destination, t);
            controller.transform.position = position;
            body.position = position;
            if (finishOnBlack && !fading && duration - elapsed <= fadeOutDuration) BeginFade();
            yield return null;
        }
        controller.transform.position = destination;
        body.position = destination;
        if (finishOnBlack && !fading) BeginFade();
    }

    private void BeginFade()
    {
        fading = true;
        StartCoroutine(screenFade.FadeOutRoutine(fadeOutDuration, () => reachedBlack = true));
    }

    /// <summary>Optional continuation when the next event needs to return to gameplay.</summary>
    public void ResumeFromBlack()
    {
        if (!IsPlaying || stage != "Black" || routine != null) return;
        routine = StartCoroutine(Resume());
    }

    private IEnumerator Resume()
    {
        yield return screenFade.FadeInRoutine(.5f);
        Restore(false);
        routine = null;
        stage = "Complete";
    }

    public void CancelForExternalSequence()
    {
        if (!IsPlaying) return;
        StopAllCoroutines();
        if (fading && screenFade != null) screenFade.CancelCutscene();
        Restore(true);
        routine = null;
        fading = false;
        reachedBlack = false;
        HasOpened = false;
        stage = "Cancelled";
    }

    private void Restore(bool returnToStart)
    {
        if (captured)
        {
            if (returnToStart && controller != null)
            {
                controller.transform.SetPositionAndRotation(originalPosition, originalRotation);
                if (body != null) { body.position=originalPosition; body.rotation=originalRotation; }
                if (gatePivot != null) gatePivot.localRotation=closedRotation;
            }
            if (view != null) { view.transform.localPosition=cameraLocalPosition; view.transform.localRotation=returnToStart?cameraLocalRotation:Quaternion.identity; }
            if (returnToStart && controller != null && view != null)
                controller.FaceWorldPosition(view.transform.position + originalViewRotation * Vector3.forward);
            if (body != null) body.isKinematic=wasKinematic;
            for (int i=0; i<colliders.Length; i++) if(colliders[i]!=null) colliders[i].enabled=colliderStates[i];
            for (int i=0; i<bobs.Length; i++) if(bobs[i]!=null) bobs[i].enabled=bobStates[i];
            for (int i=0; i<bosses.Length; i++) if(bosses[i]!=null) bosses[i].SetSequencePaused(bossPauseStates[i]);
            dayCycle?.SetSequenceTimerPaused(this,false);
            if (controller!=null) { controller.enabled=controllerEnabled; controller.UnlockMovement(this); }
            interactor?.UnlockInteraction(this);
            Physics.SyncTransforms();
        }
        captured=false;
        IsPlaying=false;
    }

    private void OnDisable() => CancelForExternalSequence();
}