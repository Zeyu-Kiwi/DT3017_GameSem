using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>First-person vent entry, hidden climb, crawl, fall and recovery.</summary>
[DisallowMultipleComponent]
public sealed class VentTraversalCutscene : MonoBehaviour, IInteractable
{
    [Header("Entry gate")]
    [SerializeField] private VentInspectionController inspection;
    [SerializeField] private ScreenFadeCutscene screenFade;
    [SerializeField] private Collider openingCollider;
    [Header("Camera path (world poses)")]
    [SerializeField] private Transform firstVentPoint;
    [SerializeField] private Transform secondVentPoint;
    [Tooltip("Camera-height points, ending directly above the exit hole.")]
    [SerializeField] private Transform[] crawlPoints;
    [Tooltip("Standing player ROOT position, not camera position. Forward sets final facing.")]
    [SerializeField] private Transform playerStandPoint;
    [Header("Timing (seconds)")]
    [SerializeField, Min(.01f)] private float crouchDuration = .7f;
    [SerializeField, Min(.01f)] private float enterDuration = 1.1f;
    [SerializeField, Min(0f)] private float fadeOutDuration = .5f;
    [SerializeField, Min(0f)] private float hiddenClimbDuration = .5f;
    [SerializeField, Min(0f)] private float fadeInDuration = .65f;
    [SerializeField, Min(.01f)] private float crawlSpeed = 1.4f;
    [SerializeField, Min(.01f)] private float fallDuration = .8f;
    [SerializeField, Min(0f)] private float faceDownDuration = 1f;
    [SerializeField, Min(.01f)] private float standUpDuration = 1.6f;
    [Header("Motion")]
    [SerializeField, Min(0f)] private float crouchDrop = .75f;
    [SerializeField, Min(0f)] private float crawlBobAmount = .015f;
    [SerializeField, Min(0f)] private float crawlBobFrequency = 2f;
    [SerializeField, Min(.05f)] private float faceDownEyeHeight = .18f;
    [SerializeField] private Vector3 faceDownAngles = new Vector3(78f, 0f, 12f);
    [Header("Landing shake")]
    [SerializeField] private bool landingShakeEnabled = true;
    [SerializeField, Min(0f)] private float landingShakeDuration = .35f;
    [SerializeField, Min(0f)] private float landingShakeDistance = .025f;
    [SerializeField, Min(0f)] private float landingShakeAngle = 2.5f;
    [SerializeField, Min(0f)] private float landingShakeFrequency = 24f;
    [Header("Hooks")]
    [SerializeField] private UnityEvent onStarted = new UnityEvent();
    [SerializeField] private UnityEvent onLanded = new UnityEvent();
    [SerializeField] private UnityEvent onFinished = new UnityEvent();
    [Header("Runtime")]
    [SerializeField] private string stage = "Idle";
    public bool IsPlaying { get; private set; }
    public bool HasPlayed { get; private set; }
    public bool CanInteract => isActiveAndEnabled && !IsPlaying && !HasPlayed && inspection != null
        && inspection.IsComplete && !inspection.IsInspecting;
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
    private bool controllerEnabled, bodyKinematic;
    private Vector3 originalPlayerPosition, cameraLocalPosition;
    private Quaternion originalPlayerRotation, cameraLocalRotation, originalLookRotation;
    private float originalFov, originalNearClip;
    private CursorLockMode cursorLock;
    private bool cursorVisible;
    private Coroutine sequence;
    private bool stateCaptured;

    public void Interact(GameObject player)
    {
        if (!CanInteract || player == null) return;
        if (firstVentPoint == null || secondVentPoint == null || playerStandPoint == null ||
            crawlPoints == null || crawlPoints.Length == 0 || System.Array.Exists(crawlPoints, p => p == null)
            || screenFade == null || !screenFade.isActiveAndEnabled || !screenFade.IsConfigured || screenFade.IsPlaying)
        {
            Debug.LogError("Vent traversal needs both camera markers, crawl points, a standing point and an available screen fade.", this);
            return;
        }
        controller = player.GetComponent<FirstPersonController>();
        view = player.GetComponentInChildren<Camera>();
        body = player.GetComponent<Rigidbody>();
        if (controller == null || view == null || body == null) return;
        IsPlaying = true;
        sequence = StartCoroutine(Play(player));
    }

    private IEnumerator Play(GameObject player)
    {
        interactor = player.GetComponent<PlayerInteractor>();
        originalPlayerPosition = player.transform.position;
        originalPlayerRotation = player.transform.rotation;
        cameraLocalPosition = view.transform.localPosition;
        cameraLocalRotation = view.transform.localRotation;
        originalFov = view.fieldOfView;
        originalNearClip = view.nearClipPlane;
        originalLookRotation = view.transform.rotation;
        view.nearClipPlane = Mathf.Min(originalNearClip, .03f);
        cursorLock = Cursor.lockState; cursorVisible = Cursor.visible;
        controllerEnabled = controller.enabled;
        bodyKinematic = body.isKinematic;
        colliders = player.GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[colliders.Length];
        bobs = player.GetComponentsInChildren<HeadBob>(true);
        bobStates = new bool[bobs.Length];
        bosses = FindObjectsByType<BossController>(FindObjectsSortMode.None);
        bossPauseStates = new bool[bosses.Length];
        stateCaptured = true;
        controller.LockMovement(this, true);
        controller.enabled = false;
        interactor?.LockInteraction(this);
        for (int i=0;i<bobs.Length;i++) { bobStates[i]=bobs[i].enabled; bobs[i].enabled=false; }
        for (int i=0;i<colliders.Length;i++) { colliderStates[i]=colliders[i].enabled; colliders[i].enabled=false; }
        body.isKinematic = true;
        for (int i=0;i<bosses.Length;i++) { bossPauseStates[i]=bosses[i].IsSequencePaused; bosses[i].SetSequencePaused(true); }
        dayCycle = FindFirstObjectByType<DayCycleManager>();
        dayCycle?.SetSequenceTimerPaused(this, true);
        Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
        onStarted.Invoke();

        stage="Crouching";
        var direction=firstVentPoint.position-view.transform.position;
        direction.y=0f;
        var facing=direction.sqrMagnitude>.001f?Quaternion.LookRotation(direction):firstVentPoint.rotation;
        yield return MoveView(view.transform.position-Vector3.up*crouchDrop, facing, crouchDuration);
        stage="Entering vent";
        yield return MoveView(firstVentPoint.position, firstVentPoint.rotation, enterDuration);
        stage="Hidden climb";
        yield return screenFade.PlayRoutine(() => {
            view.transform.SetPositionAndRotation(secondVentPoint.position,secondVentPoint.rotation);
            stage="Inside second vent";
        }, fadeOutDuration, hiddenClimbDuration, fadeInDuration);
        stage="Crawling";
        foreach(var point in crawlPoints)
            yield return MoveView(point.position, point.rotation,
                Vector3.Distance(view.transform.position,point.position)/Mathf.Max(.01f,crawlSpeed),true);

        // Marker stores a standing root pose. Preserve the player's authored feet offset.
        float feetOffset=0f;
        foreach(var c in colliders) if(c is CapsuleCollider capsule && c.transform==player.transform)
        {
            var scale=capsule.transform.lossyScale;
            feetOffset=capsule.center.y*scale.y - Mathf.Max(capsule.height*.5f*scale.y,capsule.radius*Mathf.Max(scale.x,scale.z));
            break;
        }
        Vector3 facePosition=playerStandPoint.position+Vector3.up*(feetOffset+faceDownEyeHeight);
        Quaternion faceRotation=Quaternion.Euler(0f,playerStandPoint.eulerAngles.y,0f)*Quaternion.Euler(faceDownAngles);
        stage="Falling";
        yield return MoveView(facePosition,faceRotation,fallDuration,false,true);
        stage="Face down";
        onLanded.Invoke();
        if (landingShakeEnabled && landingShakeDuration > 0f)
        {
            stage="Landing impact";
            yield return ShakeOnLanding();
            stage="Face down";
        }
        if(faceDownDuration>0f)yield return new WaitForSecondsRealtime(faceDownDuration);
        stage="Standing up";
        var cameraWorldPosition=view.transform.position;
        var cameraWorldRotation=view.transform.rotation;
        controller.TeleportTo(playerStandPoint);
        player.GetComponent<PlayerSeatController>()?.SetSeatedAfterTeleport(false);
        view.transform.localPosition=cameraLocalPosition;
        view.transform.localRotation=Quaternion.identity;
        var standingPosition=view.transform.position;
        var standingRotation=view.transform.rotation;
        view.transform.SetPositionAndRotation(cameraWorldPosition,cameraWorldRotation);
        yield return MoveView(standingPosition,standingRotation,standUpDuration);
        HasPlayed=true;
        Restore(false);
        stage="Complete";
        sequence=null;
        onFinished.Invoke();
    }

    private IEnumerator MoveView(Vector3 destination, Quaternion rotation, float duration, bool bob=false, bool falling=false)
    {
        var start=view.transform.position;
        var startRotation=view.transform.rotation;
        float elapsed=0f;
        while(elapsed<duration)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(elapsed/Mathf.Max(.001f,duration));
            float blend=falling?t*t:Mathf.SmoothStep(0f,1f,t);
            Vector3 position=Vector3.Lerp(start,destination,blend);
            if(bob)position+=Vector3.up*(Mathf.Sin(elapsed*crawlBobFrequency*Mathf.PI*2f)*crawlBobAmount*Mathf.Sin(t*Mathf.PI));
            view.transform.SetPositionAndRotation(position,Quaternion.Slerp(startRotation,rotation,blend));
            yield return null;
        }
        view.transform.SetPositionAndRotation(destination,rotation);
    }

    private IEnumerator ShakeOnLanding()
    {
        Vector3 restingPosition = view.transform.position;
        Quaternion restingRotation = view.transform.rotation;
        float elapsed = 0f;
        while (elapsed < landingShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / landingShakeDuration);
            float strength = (1f - t) * (1f - t);
            float phase = elapsed * landingShakeFrequency * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Sin(phase), Mathf.Sin(phase * 1.37f), 0f)
                * (landingShakeDistance * strength);
            Vector3 angles = new Vector3(Mathf.Sin(phase * .83f), Mathf.Sin(phase * 1.13f), Mathf.Sin(phase))
                * (landingShakeAngle * strength);
            view.transform.SetPositionAndRotation(restingPosition + restingRotation * offset,
                restingRotation * Quaternion.Euler(angles));
            yield return null;
        }
        view.transform.SetPositionAndRotation(restingPosition, restingRotation);
    }
    public void CancelForExternalSequence()
    {
        if(!IsPlaying)return;
        if(sequence!=null)StopCoroutine(sequence);
        sequence=null;
        // The fade routine is driven by this coroutine, so cancel its overlay explicitly.
        screenFade?.CancelCutscene();
        Restore(true);
        stage="Cancelled";
    }

    private void Restore(bool returnToEntry)
    {
        if(stateCaptured)
        {
            if(controller!=null && returnToEntry)
            {
                controller.transform.SetPositionAndRotation(originalPlayerPosition,originalPlayerRotation);
                if(body!=null){body.position=originalPlayerPosition;body.rotation=originalPlayerRotation;}
            }
            if(view!=null){view.transform.localPosition=cameraLocalPosition;view.transform.localRotation=returnToEntry?cameraLocalRotation:Quaternion.identity;view.fieldOfView=originalFov;view.nearClipPlane=originalNearClip;}
            if(returnToEntry && controller!=null && view!=null)
                controller.FaceWorldPosition(view.transform.position + originalLookRotation * Vector3.forward);
            if(body!=null)body.isKinematic=bodyKinematic;
            for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=colliderStates[i];
            for(int i=0;i<bobs.Length;i++)if(bobs[i]!=null)bobs[i].enabled=bobStates[i];
            for(int i=0;i<bosses.Length;i++)if(bosses[i]!=null)bosses[i].SetSequencePaused(bossPauseStates[i]);
            dayCycle?.SetSequenceTimerPaused(this,false);
            if(controller!=null){controller.enabled=controllerEnabled;controller.UnlockMovement(this);}
            interactor?.UnlockInteraction(this);
            if(returnToEntry){Cursor.lockState=cursorLock;Cursor.visible=cursorVisible;}
            Physics.SyncTransforms();
        }
        stateCaptured=false;
        IsPlaying=false;
    }

    private void OnEnable()
    {
        if (inspection != null) inspection.OnCoverRemoved.AddListener(EnableOpening);
        if (openingCollider != null) openingCollider.enabled = inspection != null && inspection.IsComplete;
    }

    private void EnableOpening()
    {
        if (openingCollider != null) openingCollider.enabled = true;
    }

    private void OnDisable()
    {
        if (inspection != null) inspection.OnCoverRemoved.RemoveListener(EnableOpening);
        CancelForExternalSequence();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.cyan;
        if(firstVentPoint!=null)Gizmos.DrawWireSphere(firstVentPoint.position,.12f);
        Transform previous=secondVentPoint;
        if(previous!=null)Gizmos.DrawWireSphere(previous.position,.12f);
        if(crawlPoints!=null)foreach(var point in crawlPoints){if(point==null)continue;if(previous!=null)Gizmos.DrawLine(previous.position,point.position);Gizmos.DrawWireSphere(point.position,.12f);previous=point;}
        if(playerStandPoint!=null){Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(playerStandPoint.position,.2f);if(previous!=null)Gizmos.DrawLine(previous.position,playerStandPoint.position);}
    }
}