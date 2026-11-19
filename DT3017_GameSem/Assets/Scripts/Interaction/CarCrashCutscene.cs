using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Walk onto the road, react to a honk, and cut to black before the approaching car hits.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
public sealed class CarCrashCutscene : MonoBehaviour
{
    [Header("Player path (standing root positions)")]
    [SerializeField] private Transform roadEntryPoint;
    [SerializeField] private Transform honkPoint;
    [SerializeField, Min(.01f)] private float walkSpeed = 2f;
    [SerializeField, Min(.01f)] private float walkTurnDuration = .35f;
    [SerializeField, Min(0f)] private float walkBobAmount = .025f;
    [SerializeField, Min(.01f)] private float walkBobFrequency = 1.7f;
    [Header("Car")]
    [SerializeField] private Transform car;
    [SerializeField] private Transform carStartPoint;
    [SerializeField, Min(.01f)] private float carSpeed = 24f;
    [Tooltip("Distance from the car's front bumper to the player when the screen cuts to black.")]
    [SerializeField, Min(.1f)] private float cutBeforeImpactDistance = 2f;
    [Header("Honk and reaction")]
    [SerializeField] private AudioClip honkClip;
    [SerializeField] private AudioSource honkSource;
    [SerializeField, Range(0f, 1f)] private float honkVolume = 1f;
    [SerializeField, Min(0f)] private float reactionDelay = .15f;
    [SerializeField, Min(.01f)] private float lookTurnDuration = .65f;
    [Header("Walk and crash audio")]
    [SerializeField] private AudioClip[] walkStepClips;
    [SerializeField, Range(0f, 1f)] private float walkStepVolume = 1f;
    [SerializeField, Min(.05f)] private float walkStepInterval = .45f;
    [SerializeField] private Vector2 walkStepPitchRange = new Vector2(.95f, 1.05f);
    [SerializeField] private AudioClip crashClip;
    [SerializeField, Range(0f, 1f)] private float crashVolume = 1f;
    [Header("Systems")]
    [SerializeField] private ScreenFadeCutscene screenFade;
    [SerializeField] private EscapeFlowController escapeFlow;
    [Header("Events")]
    [SerializeField] private UnityEvent onStarted = new UnityEvent();
    [SerializeField] private UnityEvent onHonk = new UnityEvent();
    [Tooltip("Fully black, player remains locked. Connect the next scene/sequence here.")]
    [SerializeField] private UnityEvent onReachedBlack = new UnityEvent();
    [Header("Runtime")]
    [SerializeField] private string stage = "Idle";
    public bool IsPlaying { get; private set; }
    public bool HasPlayed { get; private set; }
    public string Stage => stage;
    public float DistanceToImpact { get; private set; }

    private FirstPersonController controller;
    private PlayerInteractor interactor;
    private Camera view;
    private Rigidbody body;
    private Collider[] playerColliders, carColliders;
    private bool[] playerColliderStates, carColliderStates;
    private HeadBob[] bobs;
    private bool[] bobStates;
    private BossController[] bosses;
    private bool[] bossPauseStates;
    private DayCycleManager dayCycle;
    private Renderer[] carRenderers;
    private Rigidbody[] carBodies;
    private bool[] carBodyStates;
    private bool captured, controllerEnabled, bodyKinematic, carWasActive, startedEscape;
    private Vector3 originalPosition, originalCarPosition, cameraLocalPosition;
    private Quaternion originalRotation, originalCarRotation, cameraLocalRotation, originalLook;
    private CursorLockMode cursorLock;
    private bool cursorVisible;
    private Coroutine sequence;
    private bool mustLeaveTrigger;

    private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<FirstPersonController>();
        if (player != null && !mustLeaveTrigger) Begin(player.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<FirstPersonController>() != null && !IsPlaying) mustLeaveTrigger = false;
    }

    public void Begin(GameObject player)
    {
        if (IsPlaying || HasPlayed || !isActiveAndEnabled || player == null) return;
        if (roadEntryPoint == null || honkPoint == null || car == null || carStartPoint == null
            || screenFade == null || !screenFade.IsConfigured || !screenFade.isActiveAndEnabled || screenFade.IsPlaying)
        {
            Debug.LogError("Car crash cutscene needs road/honk/car-start markers, a car and an available screen fade.", this);
            return;
        }
        controller = player.GetComponent<FirstPersonController>();
        body = player.GetComponent<Rigidbody>();
        view = player.GetComponentInChildren<Camera>();
        if (controller == null || body == null || view == null) return;
        IsPlaying = true;
        sequence = StartCoroutine(Play(player));
    }

    private IEnumerator Play(GameObject player)
    {
        interactor = player.GetComponent<PlayerInteractor>();
        originalPosition = player.transform.position; originalRotation = player.transform.rotation;
        originalLook = view.transform.rotation;
        cameraLocalPosition = view.transform.localPosition; cameraLocalRotation = view.transform.localRotation;
        originalCarPosition = car.position; originalCarRotation = car.rotation; carWasActive = car.gameObject.activeSelf;
        controllerEnabled = controller.enabled; bodyKinematic = body.isKinematic;
        cursorLock = Cursor.lockState; cursorVisible = Cursor.visible;
        playerColliders = player.GetComponentsInChildren<Collider>(true);
        carColliders = car.GetComponentsInChildren<Collider>(true);
        playerColliderStates = new bool[playerColliders.Length]; carColliderStates = new bool[carColliders.Length];
        bobs = player.GetComponentsInChildren<HeadBob>(true); bobStates = new bool[bobs.Length];
        carBodies = car.GetComponentsInChildren<Rigidbody>(true); carBodyStates = new bool[carBodies.Length];
        bosses = FindObjectsByType<BossController>(FindObjectsSortMode.None); bossPauseStates = new bool[bosses.Length];
        captured = true;
        controller.LockMovement(this, true); controller.enabled = false; interactor?.LockInteraction(this);
        for (int i=0;i<playerColliders.Length;i++){playerColliderStates[i]=playerColliders[i].enabled;playerColliders[i].enabled=false;}
        for (int i=0;i<carColliders.Length;i++){carColliderStates[i]=carColliders[i].enabled;carColliders[i].enabled=false;}
        for (int i=0;i<bobs.Length;i++){bobStates[i]=bobs[i].enabled;bobs[i].enabled=false;}
        for (int i=0;i<carBodies.Length;i++){carBodyStates[i]=carBodies[i].isKinematic;carBodies[i].isKinematic=true;}
        for (int i=0;i<bosses.Length;i++){bossPauseStates[i]=bosses[i].IsSequencePaused;bosses[i].SetSequencePaused(true);}
        body.isKinematic = true;
        dayCycle = FindFirstObjectByType<DayCycleManager>(); dayCycle?.SetSequenceTimerPaused(this,true);
        if (escapeFlow == null) escapeFlow = FindFirstObjectByType<EscapeFlowController>();
        startedEscape = escapeFlow != null && escapeFlow.BeginEscape();
        Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        car.SetPositionAndRotation(carStartPoint.position,carStartPoint.rotation);car.gameObject.SetActive(true);
        carRenderers=car.GetComponentsInChildren<Renderer>();
        if(honkClip!=null && honkClip.loadState==AudioDataLoadState.Unloaded)honkClip.LoadAudioData();
        onStarted.Invoke();
        stage="Walking onto road";
        yield return WalkTo(roadEntryPoint.position);
        stage="Crossing road";
        yield return WalkTo(honkPoint.position);
        // Give the honk time to load during the walk, so first playback does not stall the reveal.
        while(honkClip!=null && honkClip.loadState==AudioDataLoadState.Loading)yield return null;
        stage="Honk";
        if(honkSource!=null && honkClip!=null)
        {
            honkSource.transform.position=car.position;
            var hub=GameAudioHub.Instance;
            if(hub!=null)honkSource.outputAudioMixerGroup=hub.WorldSfxGroup;
            honkSource.PlayOneShot(honkClip,honkVolume);
        }
        onHonk.Invoke();
        Quaternion initialView=view.transform.rotation;
        Vector3 direction=honkPoint.position-car.position;direction.y=0;
        direction=direction.sqrMagnitude>.001f?direction.normalized:carStartPoint.forward;
        float elapsed=0;
        while(true)
        {
            float gap=FrontGap(direction);
            DistanceToImpact=gap;
            if(gap<=cutBeforeImpactDistance+.001f)break;
            float step=Mathf.Min(carSpeed*Time.unscaledDeltaTime,Mathf.Max(0,gap-cutBeforeImpactDistance));
            car.position+=direction*step;
            if(honkSource!=null)honkSource.transform.position=car.position;
            elapsed+=Time.unscaledDeltaTime;
            if(elapsed>=reactionDelay)
            {
                float turn=Mathf.Clamp01((elapsed-reactionDelay)/Mathf.Max(.01f,lookTurnDuration));
                stage=turn<1?"Turning toward car":"Car approaching";
                Vector3 target=CarBounds().center;
                Quaternion targetLook=Quaternion.LookRotation(target-view.transform.position);
                Quaternion look=Quaternion.Slerp(initialView,targetLook,Mathf.SmoothStep(0,1,turn));
                Quaternion yaw=Quaternion.Euler(0,look.eulerAngles.y,0);
                controller.transform.rotation=yaw;body.rotation=yaw;
                view.transform.rotation=look*Quaternion.Euler(0,0,Mathf.Sin(turn*Mathf.PI)*1.5f);
            }
            DistanceToImpact=FrontGap(direction);
            if(DistanceToImpact<=cutBeforeImpactDistance+.001f)break;
            yield return null;
        }
        view.transform.localPosition=cameraLocalPosition;
        // Hard cut; keep both the player and car frozen before physical contact.
        yield return screenFade.FadeOutRoutine(0f);
        WorldAudioOneShot.Play(crashClip, view.transform.position, crashVolume);
        stage="Black";HasPlayed=true;sequence=null;
        escapeFlow?.CompleteEscape(player, EscapeEnding.CarCrash);
        onReachedBlack.Invoke();
    }

    private IEnumerator WalkTo(Vector3 destination)
    {
        Vector3 start=controller.transform.position;
        Vector3 direction=destination-start;direction.y=0;
        Quaternion facing=direction.sqrMagnitude>.001f?Quaternion.LookRotation(direction):controller.transform.rotation;
        Quaternion initial=controller.transform.rotation;
        float elapsed=0;
        while(Quaternion.Angle(initial,facing)>.5f && elapsed<walkTurnDuration)
        {
            elapsed+=Time.unscaledDeltaTime;
            Quaternion q=Quaternion.Slerp(initial,facing,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/walkTurnDuration)));
            controller.transform.rotation=q;body.rotation=q;yield return null;
        }
        controller.transform.rotation=facing;body.rotation=facing;controller.ResetLook();view.transform.localRotation=cameraLocalRotation;
        float duration=Vector3.Distance(start,destination)/Mathf.Max(.01f,walkSpeed);
        elapsed=0;
        float nextStepAt=0f;
        while(elapsed<duration)
        {
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/Mathf.Max(.001f,duration));
            Vector3 position=Vector3.Lerp(start,destination,t);controller.transform.position=position;body.position=position;
            if(elapsed>=nextStepAt)
            {
                PlayWalkStep(position);
                nextStepAt+=Mathf.Max(.05f,walkStepInterval);
            }
            float envelope=Mathf.Min(1,Mathf.Min(elapsed,duration-elapsed)/.2f);
            view.transform.localPosition=cameraLocalPosition+Vector3.up*(Mathf.Sin(elapsed*walkBobFrequency*Mathf.PI*2)*walkBobAmount*Mathf.Max(0,envelope));
            yield return null;
        }
        controller.transform.position=destination;body.position=destination;view.transform.localPosition=cameraLocalPosition;
    }

    private void PlayWalkStep(Vector3 position)
    {
        if(walkStepClips==null || walkStepClips.Length==0)return;
        AudioClip clip=walkStepClips[Random.Range(0,walkStepClips.Length)];
        float pitch=Random.Range(Mathf.Min(walkStepPitchRange.x,walkStepPitchRange.y),
            Mathf.Max(walkStepPitchRange.x,walkStepPitchRange.y));
        WorldAudioOneShot.Play(clip,position,walkStepVolume,pitch);
    }

    private Bounds CarBounds()
    {
        Bounds bounds=new Bounds(car.position,Vector3.zero);bool found=false;
        foreach(var renderer in carRenderers)if(renderer!=null){if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
        return bounds;
    }

    private float FrontGap(Vector3 direction)
    {
        var bounds=CarBounds();
        float extent=Vector3.Dot(bounds.extents,new Vector3(Mathf.Abs(direction.x),Mathf.Abs(direction.y),Mathf.Abs(direction.z)));
        return Vector3.Dot(controller.transform.position-bounds.center,direction)-extent;
    }

    public void CancelForExternalSequence()
    {
        if(!captured)return;
        StopAllCoroutines();sequence=null;screenFade?.CancelCutscene();
        if(honkSource!=null)honkSource.Stop();
        if(controller!=null)controller.transform.SetPositionAndRotation(originalPosition,originalRotation);
        if(body!=null){body.position=originalPosition;body.rotation=originalRotation;}
        if(view!=null){view.transform.localPosition=cameraLocalPosition;view.transform.localRotation=cameraLocalRotation;}
        if(controller!=null && view!=null)controller.FaceWorldPosition(view.transform.position+originalLook*Vector3.forward);
        if(car!=null){car.SetPositionAndRotation(originalCarPosition,originalCarRotation);car.gameObject.SetActive(carWasActive);}
        RestoreControls();
        if(startedEscape)escapeFlow?.CancelEscape();
        startedEscape=false;HasPlayed=false;mustLeaveTrigger=true;stage="Cancelled";
    }

    private void RestoreControls()
    {
        for(int i=0;i<playerColliders.Length;i++)if(playerColliders[i]!=null)playerColliders[i].enabled=playerColliderStates[i];
        for(int i=0;i<carColliders.Length;i++)if(carColliders[i]!=null)carColliders[i].enabled=carColliderStates[i];
        for(int i=0;i<bobs.Length;i++)if(bobs[i]!=null)bobs[i].enabled=bobStates[i];
        for(int i=0;i<carBodies.Length;i++)if(carBodies[i]!=null)carBodies[i].isKinematic=carBodyStates[i];
        for(int i=0;i<bosses.Length;i++)if(bosses[i]!=null)bosses[i].SetSequencePaused(bossPauseStates[i]);
        if(body!=null)body.isKinematic=bodyKinematic;
        if(controller!=null){controller.enabled=controllerEnabled;controller.UnlockMovement(this);}
        if(interactor!=null)interactor.UnlockInteraction(this);
        dayCycle?.SetSequenceTimerPaused(this,false);Cursor.lockState=cursorLock;Cursor.visible=cursorVisible;
        captured=false;IsPlaying=false;Physics.SyncTransforms();
    }

    private void OnDisable()=>CancelForExternalSequence();
}
