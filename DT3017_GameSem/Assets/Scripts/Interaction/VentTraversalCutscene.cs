using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>First-person vent entry, hidden climb, crawl, fall and recovery.</summary>
[DisallowMultipleComponent]
public sealed class VentTraversalCutscene : MonoBehaviour, IInteractable
{
    [Header("Entry gate")]
    [SerializeField] private VentInspectionController inspection;
    [Tooltip("Optional alternative gate for a boarded passage.")]
    [SerializeField] private BoardedHolePryingMinigame boardedHole;
    [Tooltip("Crawl out and stand directly, without the vent fall/landing sequence.")]
    [SerializeField] private bool walkOutInsteadOfFalling;
    [SerializeField] private ScreenFadeCutscene screenFade;
    [SerializeField] private Collider openingCollider;
    [SerializeField] private EscapeFlowController escapeFlow;
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
    [Header("Entry crawl during fade")]
    [SerializeField] private bool crawlDuringEntryFade;
    [Tooltip("Start crawling through the second passage at full black and continue through fade-in.")]
    [SerializeField] private bool crawlDuringFadeIn;
    [SerializeField, Min(0f)] private float entryCrawlBeforeFade = 1f;
    [Header("Motion")]
    [SerializeField, Min(0f)] private float crouchDrop = .75f;
    [SerializeField, Min(0f)] private float crawlBobAmount = .015f;
    [SerializeField, Min(0f)] private float crawlBobFrequency = 2f;
    [SerializeField, Min(.05f)] private float faceDownEyeHeight = .18f;
    [SerializeField] private Vector3 faceDownAngles = new Vector3(78f, 0f, 12f);
    [Header("Natural stand-up")]
    [SerializeField] private bool naturalStandUp;
    [Tooltip("Normalized rise, including a small push-off dip and settling overshoot.")]
    [SerializeField] private AnimationCurve standRiseCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f), new Keyframe(.12f, -.025f, 0f, 0f),
        new Keyframe(.28f, .08f, 1f, 1f), new Keyframe(.6f, .78f, 1.6f, 1.6f),
        new Keyframe(.82f, 1.035f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f));
    [Tooltip("Positive pitch looks down while pushing off, then gently settles level.")]
    [SerializeField] private AnimationCurve standLookDownCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(.18f, 14f), new Keyframe(.4f, 12f),
        new Keyframe(.72f, 3f), new Keyframe(.88f, -1.5f), new Keyframe(1f, 0f));
    [SerializeField, Min(0f)] private float standForwardLean = .045f;
    [SerializeField, Min(0f)] private float standNoisePosition = .006f;
    [SerializeField, Min(0f)] private float standNoiseAngle = .35f;
    [SerializeField, Min(.01f)] private float standNoiseFrequency = 2.2f;
    [Header("Vent edge fall")]
    [SerializeField, Min(.01f)] private float edgeTipDistance = .65f;
    [SerializeField, Range(0f, 60f)] private float edgeTipPitch = 22f;
    [SerializeField, Range(0f, 20f)] private float edgeTipRoll = 4f;
    [SerializeField, Min(0f)] private float edgeTipDrop = .12f;
    [SerializeField, Min(0f)] private float initialFallSpeed = .65f;
    [Header("Landing shake")]
    [Tooltip("Body impact starts before the camera finishes settling at floor height.")]
    [SerializeField, Min(0f)] private float landingImpactLead = .1f;
    [SerializeField, Min(0f)] private float landingCompression = .06f;
    [Tooltip("Signed pitch kick on impact. Negative values kick the gaze up before settling.")]
    [SerializeField] private float landingImpactPitch = 8f;
    [SerializeField] private float landingImpactRoll = 10f;
    [SerializeField] private bool landingShakeEnabled = true;
    [SerializeField, Min(0f)] private float landingShakeDuration = .35f;
    [SerializeField, Min(0f)] private float landingShakeDistance = .025f;
    [SerializeField, Min(0f)] private float landingShakeAngle = 2.5f;
    [SerializeField, Min(0f)] private float landingShakeFrequency = 24f;
    [Header("Audio")]
    [SerializeField] private AudioClip crouchClip;
    [SerializeField] private AudioClip enterVentClip;
    [SerializeField] private AudioClip crawlClip;
    [Tooltip("Short crawl sounds per camera bob cycle.")]
    [SerializeField, Min(1)] private int crawlSoundsPerBobCycle = 1;
    [SerializeField] private Vector2 crawlPitchRange = new Vector2(.9f, 1.1f);
    [SerializeField] private AudioClip playerFallClip;
    [SerializeField] private AudioClip landingClip;
    [SerializeField] private AudioClip standUpClip;
    [SerializeField, Range(0f, 1f)] private float cutsceneVolume = 1f;
    [Header("Hooks")]
    [SerializeField] private UnityEvent onStarted = new UnityEvent();
    [SerializeField] private UnityEvent onLanded = new UnityEvent();
    [SerializeField] private UnityEvent onFinished = new UnityEvent();
    [Header("Runtime")]
    [SerializeField] private string stage = "Idle";
    public bool IsPlaying { get; private set; }
    public bool HasPlayed { get; private set; }
    private bool OpeningReady => boardedHole != null
        ? boardedHole.IsComplete && !boardedHole.IsRunning
        : inspection != null && inspection.IsComplete && !inspection.IsInspecting;
    public bool CanInteract => isActiveAndEnabled && !IsPlaying && !HasPlayed && OpeningReady;
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
    private Coroutine entryCrawlRoutine;
    private Coroutine secondCrawlRoutine;
    private Coroutine crawlAudioRoutine;
    private AudioSource crawlAudioSource;
    private bool stateCaptured;
    private bool startedEscapeFlow;

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
        if (escapeFlow == null)
            escapeFlow = FindFirstObjectByType<EscapeFlowController>();
        startedEscapeFlow = escapeFlow != null && escapeFlow.BeginEscape();
        Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
        onStarted.Invoke();

        stage="Crouching";
        WorldAudioOneShot.Play(crouchClip, view.transform.position, cutsceneVolume);
        var direction=firstVentPoint.position-view.transform.position;
        direction.y=0f;
        var facing=direction.sqrMagnitude>.001f?Quaternion.LookRotation(direction):firstVentPoint.rotation;
        yield return MoveView(view.transform.position-Vector3.up*crouchDrop, facing, crouchDuration);
        stage="Entering vent";
        WorldAudioOneShot.Play(enterVentClip, firstVentPoint.position, cutsceneVolume);
        GameAudioHub.Instance?.PlayVentAmbience();
        StartCrawlAudio();
        float entrySeconds = walkOutInsteadOfFalling
            ? Vector3.Distance(view.transform.position, firstVentPoint.position) / Mathf.Max(.01f, crawlSpeed)
            : enterDuration;
        yield return MoveView(firstVentPoint.position, firstVentPoint.rotation, entrySeconds, walkOutInsteadOfFalling);
        if (crawlDuringEntryFade)
        {
            stage = "Crawling into passage";
            entryCrawlRoutine = StartCoroutine(CrawlIntoEntry());
            if (entryCrawlBeforeFade > 0f) yield return new WaitForSecondsRealtime(entryCrawlBeforeFade);
        }
        stage = crawlDuringEntryFade ? "Crawling while fading out" : "Hidden climb";
        yield return screenFade.PlayRoutine(() => {
            PreloadTraversalAudio();
            StopEntryCrawl();
            view.transform.SetPositionAndRotation(secondVentPoint.position,secondVentPoint.rotation);
            stage = crawlDuringFadeIn ? "Crawling through fade-in" : "Inside second vent";
            if (crawlDuringFadeIn) secondCrawlRoutine = StartCoroutine(CrawlThroughSecondVent());
        }, fadeOutDuration, hiddenClimbDuration, fadeInDuration, IsTraversalAudioReady);
        stage="Crawling";
        if (crawlDuringFadeIn)
        {
            while (secondCrawlRoutine != null) yield return null;
        }
        else yield return CrawlThroughSecondVent();

        StopCrawlAudio();

        if (!walkOutInsteadOfFalling)
        {
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
        WorldAudioOneShot.Play(playerFallClip, view.transform.position, cutsceneVolume);
        yield return FallFromVent(facePosition, faceRotation);
        if(faceDownDuration>0f)yield return new WaitForSecondsRealtime(faceDownDuration);
        }
        else GameAudioHub.Instance?.PlayExteriorAmbience();
        stage="Standing up";
        WorldAudioOneShot.Play(standUpClip, view.transform.position, cutsceneVolume);
        var cameraWorldPosition=view.transform.position;
        var cameraWorldRotation=view.transform.rotation;
        controller.TeleportTo(playerStandPoint);
        player.GetComponent<PlayerSeatController>()?.SetSeatedAfterTeleport(false);
        view.transform.localPosition=cameraLocalPosition;
        view.transform.localRotation=Quaternion.identity;
        var standingPosition=view.transform.position;
        var standingRotation=view.transform.rotation;
        view.transform.SetPositionAndRotation(cameraWorldPosition,cameraWorldRotation);
        if (naturalStandUp) yield return StandNaturally(standingPosition, standingRotation);
        else yield return MoveView(standingPosition,standingRotation,standUpDuration);
        HasPlayed=true;
        Restore(false);
        startedEscapeFlow = false;
        stage="Complete";
        sequence=null;
        onFinished.Invoke();
    }

    private void PreloadTraversalAudio()
    {
        GameAudioHub.Instance?.PreloadEscapeAmbience();
        RequestAudioData(playerFallClip);
        RequestAudioData(landingClip);
        RequestAudioData(standUpClip);
    }

    private static void RequestAudioData(AudioClip clip)
    {
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
    }

    private bool IsTraversalAudioReady()
    {
        var hub = GameAudioHub.Instance;
        return (hub == null || hub.IsEscapeAmbienceReady)
            && (playerFallClip == null || playerFallClip.loadState != AudioDataLoadState.Loading)
            && (landingClip == null || landingClip.loadState != AudioDataLoadState.Loading)
            && (standUpClip == null || standUpClip.loadState != AudioDataLoadState.Loading);
    }

    private IEnumerator StandNaturally(Vector3 destination, Quaternion rotation)
    {
        Vector3 start = view.transform.position;
        Quaternion startRotation = view.transform.rotation;
        float elapsed = 0f;
        while (elapsed < standUpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, standUpDuration));
            float envelope = 16f * t * t * (1f - t) * (1f - t);
            float smooth = t * t * t * (t * (t * 6f - 15f) + 10f);
            float phase = elapsed * standNoiseFrequency;
            Vector3 noise = new Vector3(
                Mathf.PerlinNoise(11.3f, phase) * 2f - 1f,
                Mathf.PerlinNoise(37.9f, phase) * 2f - 1f,
                Mathf.PerlinNoise(61.7f, phase) * 2f - 1f);
            Vector3 position = Vector3.LerpUnclamped(start, destination, standRiseCurve.Evaluate(t));
            position += rotation * (Vector3.forward * (standForwardLean * envelope)
                + noise * (standNoisePosition * envelope));
            Vector3 angles = noise * (standNoiseAngle * envelope);
            angles.x += standLookDownCurve.Evaluate(t);
            angles.z += Mathf.Sin(t * Mathf.PI * 2f) * envelope * .65f;
            view.transform.SetPositionAndRotation(position,
                Quaternion.Slerp(startRotation, rotation, smooth) * Quaternion.Euler(angles));
            yield return null;
        }
        view.transform.SetPositionAndRotation(destination, rotation);
    }

    private IEnumerator CrawlThroughSecondVent()
    {
        for (int i = 0; i < crawlPoints.Length; i++)
        {
            var point = crawlPoints[i];
            if (!walkOutInsteadOfFalling && i == crawlPoints.Length - 1)
                yield return CrawlToVentEdge(point);
            else yield return MoveView(point.position, point.rotation,
                Vector3.Distance(view.transform.position, point.position) / Mathf.Max(.01f, crawlSpeed), true);
        }
        secondCrawlRoutine = null;
    }

    private void StartCrawlAudio()
    {
        StopCrawlAudio();
        if (crawlClip == null) return;
        var host = new GameObject("Traversal Crawl Audio");
        host.transform.SetParent(view.transform, false);
        crawlAudioSource = host.AddComponent<AudioSource>();
        crawlAudioSource.playOnAwake = false;
        crawlAudioSource.spatialBlend = 1f;
        crawlAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        crawlAudioSource.volume = cutsceneVolume;
        crawlAudioSource.clip = crawlClip;
        var hub = GameAudioHub.Instance;
        if (hub != null)
        {
            crawlAudioSource.outputAudioMixerGroup = hub.WorldSfxGroup;
            crawlAudioSource.minDistance = hub.WorldMinDistance;
            crawlAudioSource.maxDistance = hub.WorldMaxDistance;
        }
        crawlAudioRoutine = StartCoroutine(PlayCrawlAudio());
    }

    private IEnumerator PlayCrawlAudio()
    {
        while (true)
        {
            float pitch = Random.Range(Mathf.Min(crawlPitchRange.x, crawlPitchRange.y),
                Mathf.Max(crawlPitchRange.x, crawlPitchRange.y));
            crawlAudioSource.pitch = Mathf.Clamp(pitch, .1f, 3f);
            crawlAudioSource.Play();
            float interval = 1f / (Mathf.Max(.01f, crawlBobFrequency) *
                Mathf.Max(1, crawlSoundsPerBobCycle));
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    private void StopCrawlAudio()
    {
        if (crawlAudioRoutine != null) StopCoroutine(crawlAudioRoutine);
        crawlAudioRoutine = null;
        if (crawlAudioSource != null)
        {
            crawlAudioSource.Stop();
            Destroy(crawlAudioSource.gameObject);
            crawlAudioSource = null;
        }
    }

    private IEnumerator CrawlToVentEdge(Transform point)
    {
        Vector3 start = view.transform.position;
        Quaternion startRotation = view.transform.rotation;
        float distance = Vector3.Distance(start, point.position);
        float duration = distance / Mathf.Max(.01f, crawlSpeed);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, duration));
            float remaining = distance * (1f - t);
            float tip = Mathf.Clamp01(1f - remaining / Mathf.Max(.01f, edgeTipDistance));
            float turn = Mathf.SmoothStep(0f, 1f, tip);
            Vector3 position = Vector3.Lerp(start, point.position, t) - Vector3.up * (edgeTipDrop * tip * tip);
            position += Vector3.up * (Mathf.Sin(elapsed * crawlBobFrequency * Mathf.PI * 2f)
                * crawlBobAmount * Mathf.Sin(t * Mathf.PI) * (1f - tip));
            view.transform.SetPositionAndRotation(position,
                Quaternion.Slerp(startRotation, point.rotation, Mathf.SmoothStep(0f, 1f, t))
                * Quaternion.Euler(edgeTipPitch * turn, 0f, edgeTipRoll * turn));
            yield return null;
        }
        view.transform.SetPositionAndRotation(point.position - Vector3.up * edgeTipDrop,
            point.rotation * Quaternion.Euler(edgeTipPitch, 0f, edgeTipRoll));
    }

    private IEnumerator FallFromVent(Vector3 destination, Quaternion rotation)
    {
        Vector3 start = view.transform.position;
        Quaternion startRotation = view.transform.rotation;
        Vector3 forward = Quaternion.Euler(0f, playerStandPoint.eulerAngles.y, 0f) * Vector3.forward;
        float duration = Mathf.Max(.01f, fallDuration);
        float height = Mathf.Max(0f, start.y - destination.y);
        float initialSpeed = Mathf.Min(initialFallSpeed, height / duration);
        float acceleration = 2f * (height - initialSpeed * duration) / (duration * duration);
        float contactTime = Mathf.Max(0f, duration - landingImpactLead);
        float elapsed = 0f, impactStarted = -1f;
        while (true)
        {
            float time = Mathf.Min(elapsed, duration);
            float t = time / duration;
            Vector3 position = Vector3.Lerp(start, destination, t);
            position.y = start.y - initialSpeed * time - .5f * acceleration * time * time;
            position += forward * (crawlSpeed * duration * t * (1f - t) * (1f - t));
            Quaternion facing = Quaternion.Slerp(startRotation, rotation, Mathf.SmoothStep(0f, 1f, t));
            if (impactStarted < 0f && elapsed >= contactTime)
            {
                impactStarted = elapsed;
                stage = "Landing impact";
                GameAudioHub.Instance?.PlayExteriorAmbience();
                WorldAudioOneShot.Play(landingClip, position, cutsceneVolume);
                onLanded.Invoke();
            }
            float impactElapsed = impactStarted < 0f ? -1f : elapsed - impactStarted;
            bool shaking = impactElapsed >= 0f && landingShakeEnabled
                && landingShakeDuration > 0f && impactElapsed < landingShakeDuration;
            // Compose the impact onto the final descent: no grounded pause or coroutine handoff.
            if (shaking) ApplyLandingImpact(position, facing, impactElapsed);
            else view.transform.SetPositionAndRotation(position, facing);
            if (elapsed >= duration && impactStarted >= 0f && !shaking) break;
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        view.transform.SetPositionAndRotation(destination, rotation);
        stage = "Face down";
    }

    private IEnumerator CrawlIntoEntry()
    {
        Vector3 start = view.transform.position;
        Quaternion rotation = firstVentPoint.rotation;
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector3 position = start + rotation * Vector3.forward * (elapsed * Mathf.Max(.01f, crawlSpeed));
            position += Vector3.up * (Mathf.Sin(elapsed * crawlBobFrequency * Mathf.PI * 2f) * crawlBobAmount);
            view.transform.SetPositionAndRotation(position, rotation);
            yield return null;
        }
    }

    private void StopEntryCrawl()
    {
        if (entryCrawlRoutine != null) StopCoroutine(entryCrawlRoutine);
        entryCrawlRoutine = null;
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
            float blend = falling ? t * t : (bob && walkOutInsteadOfFalling ? t : Mathf.SmoothStep(0f, 1f, t));
            Vector3 position=Vector3.Lerp(start,destination,blend);
            if(bob)position+=Vector3.up*(Mathf.Sin(elapsed*crawlBobFrequency*Mathf.PI*2f)*crawlBobAmount*Mathf.Sin(t*Mathf.PI));
            view.transform.SetPositionAndRotation(position,Quaternion.Slerp(startRotation,rotation,blend));
            yield return null;
        }
        view.transform.SetPositionAndRotation(destination,rotation);
    }

    private void ApplyLandingImpact(Vector3 restingPosition, Quaternion restingRotation, float elapsed)
    {
        float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, landingShakeDuration));
        float strength = (1f - t) * (1f - t);
        float phase = elapsed * landingShakeFrequency * Mathf.PI * 2f;
        float impact = Mathf.Exp(-t * 7f) * Mathf.Cos(t * Mathf.PI * 3.5f);
        Vector3 offset = new Vector3(Mathf.Sin(phase), Mathf.Sin(phase * 1.37f), 0f)
            * (landingShakeDistance * strength);
        Vector3 angles = new Vector3(Mathf.Sin(phase * .83f), Mathf.Sin(phase * 1.13f), Mathf.Sin(phase))
            * (landingShakeAngle * strength);
        angles.x += landingImpactPitch * impact;
        angles.z += landingImpactRoll * impact;
        view.transform.SetPositionAndRotation(restingPosition - Vector3.up * (landingCompression * impact)
            + restingRotation * offset, restingRotation * Quaternion.Euler(angles));
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
        StopCrawlAudio();
        StopEntryCrawl();
        if (secondCrawlRoutine != null) StopCoroutine(secondCrawlRoutine);
        secondCrawlRoutine = null;
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
        if (returnToEntry && startedEscapeFlow)
        {
            GameAudioHub.Instance?.PlayFactoryAmbience();
            escapeFlow?.CancelEscape();
            startedEscapeFlow = false;
        }
    }

    private void OnEnable()
    {
        if (inspection != null) inspection.OnCoverRemoved.AddListener(EnableOpening);
        if (boardedHole != null) boardedHole.OnAllPlanksRemoved.AddListener(EnableOpening);
        if (openingCollider != null) openingCollider.enabled = OpeningReady;
    }

    private void EnableOpening()
    {
        if (openingCollider != null) openingCollider.enabled = true;
    }

    private void OnDisable()
    {
        if (inspection != null) inspection.OnCoverRemoved.RemoveListener(EnableOpening);
        if (boardedHole != null) boardedHole.OnAllPlanksRemoved.RemoveListener(EnableOpening);
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
