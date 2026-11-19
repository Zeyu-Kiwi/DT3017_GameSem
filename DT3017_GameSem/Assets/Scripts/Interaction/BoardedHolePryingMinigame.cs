using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class BoardedHolePryingMinigame : MonoBehaviour
{
    public enum PryStage { Inactive, MovingCamera, ChoosingPlank, MovingCrowbar, Prying, Ripping, Returning }

    [Header("Scene")]
    [SerializeField] private ItemData requiredTool;
    [SerializeField] private ToolSelectionInteractable toolInteraction;
    [SerializeField] private Transform inspectionPoint;
    [SerializeField] private Transform[] planks;
    [Tooltip("Optional head-pivot locators, matching the order of Planks. Parent each locator to its plank. Missing entries use the clicked position.")]
    [SerializeField] private Transform[] plankCrowbarLocators;
    [SerializeField] private Collider openingBlocker;
    [Header("Mash intensity")]
    [SerializeField] private KeyCode pryKey = KeyCode.Space;
    [SerializeField, Min(1f)] private float mashCap = 100f;
    [SerializeField, Min(.01f)] private float forcePerPress = 12f;
    [Tooltip("Force lost per second when taps slow down. This is not a cumulative press-count objective.")]
    [SerializeField, Min(.01f)] private float forceDecayPerSecond = 35f;
    [SerializeField, Min(0f)] private float decayDelay = .08f;
    [Header("Camera")]
    [SerializeField, Min(.01f)] private float transitionDuration = .5f;
    [SerializeField, Range(30f, 90f)] private float inspectionFov = 55f;
    [Header("Held crowbar")]
    [Tooltip("Template root must be at the hooked head's contact point.")]
    [SerializeField] private GameObject crowbarVisualTemplate;
    [Tooltip("Optional camera-child locator for the crowbar HEAD pivot and rotation. Move/rotate it to adjust the held pose, including during Play mode.")]
    [SerializeField] private Transform heldCrowbarLocator;
    [SerializeField] private Vector3 heldPosition = new Vector3(.32f, -.33f, .65f);
    [SerializeField] private Vector3 heldRotation = new Vector3(-65f, -30f, 0f);
    [Tooltip("Handle grip relative to the head-pivot template.")]
    [SerializeField] private Vector3 heldGripLocalPosition;
    [SerializeField, Min(.01f)] private float toolTravelDuration = .4f;
    [SerializeField, Min(0f)] private float contactOffset = .006f;
    [SerializeField, Range(0f, 60f)] private float restingLeanAngle = 20f;
    [SerializeField, Range(0f, 80f)] private float maximumPryAngle = 35f;
    [SerializeField, Range(0f, 15f)] private float pryPulseAngle = 3f;
    [Header("Plank motion")]
    [Tooltip("World direction away from the wall, into the room.")]
    [SerializeField] private Vector3 pullDirection = Vector3.left;
    [SerializeField, Min(0f)] private float loosenDistance = .08f;
    [SerializeField, Min(0f)] private float pulseDistance = .025f;
    [SerializeField, Min(.01f)] private float pulseDuration = .16f;
    [SerializeField, Min(.01f)] private float ripDuration = .3f;
    [SerializeField, Min(0f)] private float ripDistance = .45f;
    [SerializeField, Min(0f)] private float dropImpulse = .65f;
    [SerializeField, Min(0f)] private float debrisLifetime = 15f;
    [Header("UI")]
    [SerializeField] private GameObject minigameUI;
    [SerializeField] private UnityEngine.UI.Image progressFill;
    [SerializeField] private UnityEngine.UI.Image spacebarIcon;
    [SerializeField] private bool flashSpacebarIcon = true;
    [SerializeField, Min(.01f)] private float spacebarFlashDuration = .12f;
    [SerializeField] private Color spacebarFlashColor = Color.yellow;
    private Color spacebarOriginalColor = Color.white;
    private float spacebarFlashUntil = float.NegativeInfinity;
    [Header("Events")]
    [SerializeField] private UnityEvent onPlankRemoved = new UnityEvent();
    [SerializeField] private UnityEvent onAllPlanksRemoved = new UnityEvent();
    [Header("Runtime")]
    [SerializeField] private PryStage stage;
    [SerializeField] private int removedPlanks;
    [SerializeField] private float mashForce;
    private int selectedPlank = -1;
    private int hoveredPlank = -1;
    private bool[] removed;
    private Vector3[] restPositions;
    private Quaternion[] restRotations;
    private Vector3 hoveredPoint, headContactLocal;
    private Quaternion attachedRestRotation;
    private FirstPersonController controller;
    private PlayerInteractor interactor;
    private PauseMenuController pauseMenu;
    private InteractionOutlineHighlighter outline;
    private Camera view;
    private HeadBob[] bobs;
    private bool[] bobStates;
    private bool controllerEnabled;
    private Vector3 cameraLocalPosition;
    private Quaternion cameraLocalRotation;
    private float cameraFov, pulseStarted = -100f, lastMashTime;
    private CursorLockMode savedCursorLock;
    private bool savedCursorVisible;
    private int lastPressFrame = -1;
    private GameObject crowbar;
    private Coroutine cameraRoutine, toolRoutine;

    public bool IsRunning { get; private set; }
    public bool IsComplete { get; private set; }
    public UnityEvent OnAllPlanksRemoved => onAllPlanksRemoved;
    public int RemovedPlanks => removedPlanks;
    public int TotalPlanks => planks == null ? 0 : planks.Length;
    public int SelectedPlankIndex => selectedPlank;
    public float MashValue => mashForce;
    public float MashCap => Mathf.Max(1f, mashCap);
    public float MashProgress => Mathf.Clamp01(mashForce / MashCap);
    public PryStage Stage => stage;
    private Vector3 Pull => pullDirection.sqrMagnitude > .001f ? pullDirection.normalized : Vector3.left;
    private Vector3 LeverAxis => Vector3.Cross(Vector3.down, Pull).sqrMagnitude > .001f
        ? Vector3.Cross(Vector3.down, Pull).normalized : Vector3.forward;

    private void Awake()
    {
        pauseMenu = FindFirstObjectByType<PauseMenuController>();
        if (spacebarIcon != null) spacebarOriginalColor = spacebarIcon.color;
        restPositions = new Vector3[TotalPlanks];
        restRotations = new Quaternion[TotalPlanks];
        removed = new bool[TotalPlanks];
        for (int i = 0; i < TotalPlanks; i++) if (planks[i] != null)
        { restPositions[i] = planks[i].position; restRotations[i] = planks[i].rotation; }
        if (minigameUI != null) minigameUI.SetActive(false);
    }

    public void Begin(GameObject player)
    {
        if (IsRunning || IsComplete || !isActiveAndEnabled || player == null) return;
        if (!HasCrowbar()) { ShowToolRequiredMessage(); return; }
        if (inspectionPoint == null || TotalPlanks == 0 || crowbarVisualTemplate == null)
        { Debug.LogError("Board prying needs plank references, inspection point and a head-pivot crowbar template.", this); return; }
        controller = player.GetComponent<FirstPersonController>();
        interactor = player.GetComponent<PlayerInteractor>();
        outline = player.GetComponent<InteractionOutlineHighlighter>();
        view = player.GetComponentInChildren<Camera>();
        if (controller == null || view == null) return;
        cameraLocalPosition = view.transform.localPosition;
        cameraLocalRotation = view.transform.localRotation;
        cameraFov = view.fieldOfView;
        savedCursorLock = Cursor.lockState; savedCursorVisible = Cursor.visible;
        controllerEnabled = controller.enabled;
        bobs = player.GetComponentsInChildren<HeadBob>(true); bobStates = new bool[bobs.Length];
        for (int i = 0; i < bobs.Length; i++) { bobStates[i] = bobs[i].enabled; bobs[i].enabled = false; }
        controller.LockMovement(this, true); controller.enabled = false;
        interactor?.LockInteraction(this); outline?.Clear();
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        IsRunning = true; selectedPlank = hoveredPlank = -1; mashForce = 0f; lastPressFrame = -1;
        stage = PryStage.MovingCamera;
        UpdateUI(); if (minigameUI != null) minigameUI.SetActive(true);
        cameraRoutine = StartCoroutine(MoveCamera(false));
    }

    private bool HasCrowbar() => requiredTool != null && InventoryManager.Instance != null && InventoryManager.Instance.Has(requiredTool, 1);

    public void ShowToolRequiredMessage()
    {
        if (IsRunning) return;
        Tell("You need something to pry off these wood planks.");
    }

    private IEnumerator MoveCamera(bool back)
    {
        var start = view.transform.position; var rotation = view.transform.rotation; float fov = view.fieldOfView;
        Vector3 target = back ? view.transform.parent.TransformPoint(cameraLocalPosition) : inspectionPoint.position;
        Quaternion targetRotation = back ? view.transform.parent.rotation * cameraLocalRotation : inspectionPoint.rotation;
        float targetFov = back ? cameraFov : inspectionFov;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.01f, transitionDuration)));
            view.transform.SetPositionAndRotation(Vector3.Lerp(start, target, t), Quaternion.Slerp(rotation, targetRotation, t));
            view.fieldOfView = Mathf.Lerp(fov, targetFov, t); yield return null;
        }
        view.transform.SetPositionAndRotation(target, targetRotation); view.fieldOfView = targetFov; cameraRoutine = null;
        if (back) RestoreControls();
        else { stage = PryStage.ChoosingPlank; ShowHeldCrowbar(); UpdateUI(); ShowPlankSelectionInstruction(); }
    }

    private void ShowHeldCrowbar()
    {
        DestroyCrowbar();
        crowbar = Instantiate(crowbarVisualTemplate, view.transform, false);
        crowbar.name = "Held Crowbar";
        ApplyHeldPose();
        crowbar.SetActive(true);
    }

    private void ApplyHeldPose()
    {
        if (heldCrowbarLocator != null)
        {
            crowbar.transform.SetPositionAndRotation(heldCrowbarLocator.position, heldCrowbarLocator.rotation);
            return;
        }
        Quaternion rotation = Quaternion.Euler(heldRotation);
        crowbar.transform.localRotation = rotation;
        crowbar.transform.localPosition = heldPosition - rotation * heldGripLocalPosition;
        crowbar.SetActive(true);
    }

    private void DestroyCrowbar()
    {
        if (crowbar == null) return;
        crowbar.SetActive(false); Destroy(crowbar); crowbar = null;
    }

    private void Update()
    {
        UpdateSpacebarFlash();
        if (!IsRunning || stage == PryStage.Returning) return;
        if (Time.timeScale <= 0f || (pauseMenu != null && (pauseMenu.IsPaused || pauseMenu.ResumedThisFrame))) return;
        // Escape belongs to the pause menu, independent of script update order.
        if (Input.GetKeyDown(KeyCode.Escape)) return;
        if (Input.GetKeyDown(PlayerOptions.GetKey(GameplayAction.Interact, KeyCode.F)) && stage != PryStage.Ripping) { Exit(); return; }
        if (stage == PryStage.ChoosingPlank)
        {
            UpdateHover(Input.mousePosition);
            if (Input.GetMouseButtonDown(0) && hoveredPlank >= 0) SelectPlank(hoveredPlank, hoveredPoint);
        }
        else if (stage == PryStage.Prying)
        {
            if (!HasCrowbar()) { Tell("I need a crowbar to continue."); Exit(); return; }
            // Drain according to elapsed time, independent of frame rate or lifetime press count.
            float elapsedSincePress = Time.time - lastMashTime;
            float drainTime = Mathf.Clamp(elapsedSincePress - decayDelay, 0f, Time.deltaTime);
            mashForce = Mathf.Max(0f, mashForce - forceDecayPerSecond * drainTime);
            if (Input.GetKeyDown(pryKey)) TryPry();
            if (stage == PryStage.Prying) UpdatePlankPose();
            UpdateUI();
        }
    }

    private void UpdateHover(Vector2 cursor)
    {
        hoveredPlank = -1;
        var hits = Physics.RaycastAll(view.ScreenPointToRay(cursor), 4f, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider == openingBlocker || hit.collider.transform.IsChildOf(controller.transform)) continue;
            for (int i = 0; i < TotalPlanks; i++)
                if (!removed[i] && planks[i] != null && hit.collider.transform.IsChildOf(planks[i]))
                { hoveredPlank = i; hoveredPoint = hit.point; break; }
            if (hoveredPlank >= 0 || !hit.collider.isTrigger) break;
        }
        outline?.SetTarget(hoveredPlank >= 0 ? planks[hoveredPlank] : null);
    }

    public bool SelectPlank(int index, Vector3 contactPoint)
    {
        if (Time.timeScale <= 0f || !IsRunning || stage != PryStage.ChoosingPlank || index < 0 || index >= TotalPlanks || removed[index] || planks[index] == null) return false;
        selectedPlank = index; hoveredPlank = -1; mashForce = 0f; outline?.Clear();
        headContactLocal = planks[index].InverseTransformPoint(contactPoint + Pull * contactOffset);
        Vector3 handleDirection = (Vector3.down * Mathf.Cos(restingLeanAngle * Mathf.Deg2Rad) + Pull * Mathf.Sin(restingLeanAngle * Mathf.Deg2Rad)).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, Pull).normalized;
        if (side.sqrMagnitude < .001f) side = view.transform.right;
        attachedRestRotation = Quaternion.LookRotation(-handleDirection, side);
        var locator = SelectedPlankLocator;
        if (locator != null)
        {
            headContactLocal = planks[index].InverseTransformPoint(locator.position);
            attachedRestRotation = locator.rotation;
        }
        stage = PryStage.MovingCrowbar; UpdateUI();
        toolRoutine = StartCoroutine(MoveCrowbarToPlank()); return true;
    }

    private IEnumerator MoveCrowbarToPlank()
    {
        var start = crowbar.transform.position; var rotation = crowbar.transform.rotation;
        crowbar.transform.SetParent(null, true); crowbar.name = "Prying Crowbar";
        float elapsed = 0f;
        while (elapsed < toolTravelDuration)
        {
            elapsed += Time.deltaTime; float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.01f, toolTravelDuration)));
            crowbar.transform.SetPositionAndRotation(Vector3.Lerp(start, ContactPosition, t), Quaternion.Slerp(rotation, ContactRotation, t));
            yield return null;
        }
        crowbar.transform.SetParent(planks[selectedPlank], true);
        crowbar.transform.SetPositionAndRotation(ContactPosition, ContactRotation);
        toolRoutine = null; lastMashTime = Time.time; pulseStarted = -100f;
        stage = PryStage.Prying; UpdateUI();
        Tell("Press spacebar to remove.");
    }

    public bool TryPry()
    {
        if (Time.timeScale <= 0f || !IsRunning || stage != PryStage.Prying || lastPressFrame == Time.frameCount || !HasCrowbar()) return false;
        lastPressFrame = Time.frameCount; lastMashTime = pulseStarted = Time.time;
        spacebarFlashUntil = Time.time + Mathf.Max(.01f, spacebarFlashDuration);
        mashForce = Mathf.Min(MashCap, mashForce + forcePerPress); UpdateUI();
        if (mashForce >= MashCap) { stage = PryStage.Ripping; StartCoroutine(RipPlank()); }
        return true;
    }

    private void UpdatePlankPose()
    {
        var plank = planks[selectedPlank];
        float t = Mathf.Clamp01((Time.time - pulseStarted) / Mathf.Max(.01f, pulseDuration));
        float pulse = Mathf.Sin(t * Mathf.PI) * (1f - t);
        plank.SetPositionAndRotation(restPositions[selectedPlank] + Pull * (loosenDistance * MashProgress + pulseDistance * pulse), restRotations[selectedPlank]);
    }

    private Transform SelectedPlankLocator => plankCrowbarLocators != null && selectedPlank >= 0
        && selectedPlank < plankCrowbarLocators.Length ? plankCrowbarLocators[selectedPlank] : null;
    private Vector3 ContactPosition => SelectedPlankLocator != null
        ? SelectedPlankLocator.position : planks[selectedPlank].TransformPoint(headContactLocal);
    private Quaternion ContactRotation => SelectedPlankLocator != null
        ? SelectedPlankLocator.rotation : attachedRestRotation;

    private void LateUpdate()
    {
        if (!IsRunning || crowbar == null || Time.timeScale <= 0f) return;
        if (stage == PryStage.ChoosingPlank) { ApplyHeldPose(); return; }
        if (selectedPlank < 0 || (stage != PryStage.Prying && stage != PryStage.Ripping)) return;
        float t = Mathf.Clamp01((Time.time - pulseStarted) / Mathf.Max(.01f, pulseDuration));
        float angle = maximumPryAngle * MashProgress + pryPulseAngle * Mathf.Sin(t * Mathf.PI);
        // The root is the hooked head: rotating this root moves the handle away from the wall.
        crowbar.transform.SetPositionAndRotation(ContactPosition, Quaternion.AngleAxis(angle, LeverAxis) * ContactRotation);
    }

    private IEnumerator RipPlank()
    {
        int index = selectedPlank; var plank = planks[index];
        Vector3 start = plank.position; Quaternion rotation = plank.rotation; float elapsed = 0f;
        while (elapsed < ripDuration)
        {
            elapsed += Time.deltaTime; float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.01f, ripDuration)));
            plank.SetPositionAndRotation(Vector3.Lerp(start, restPositions[index] + Pull * ripDistance, t), rotation * Quaternion.Euler(0, 0, 18f * t));
            yield return null;
        }
        DestroyCrowbar(); plank.SetParent(null, true);
        var colliders = plank.GetComponentsInChildren<Collider>();
        foreach (var c in colliders) { c.isTrigger = false; c.enabled = true; }
        if (colliders.Length == 0)
        {
            var filter = plank.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            { var box = plank.gameObject.AddComponent<BoxCollider>(); box.center = filter.sharedMesh.bounds.center; box.size = filter.sharedMesh.bounds.size; }
        }
        foreach (var t in plank.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;
        var body = plank.GetComponent<Rigidbody>(); if (body == null) body = plank.gameObject.AddComponent<Rigidbody>();
        body.mass = .5f; body.useGravity = true; body.isKinematic = false;
        body.AddForce(Pull * dropImpulse + Vector3.up * .2f, ForceMode.VelocityChange);
        body.AddTorque(Vector3.forward * 2f, ForceMode.VelocityChange);
        if (debrisLifetime > 0f) Destroy(plank.gameObject, debrisLifetime);
        removed[index] = true; removedPlanks++; selectedPlank = -1; mashForce = 0f;
        stage = PryStage.ChoosingPlank; onPlankRemoved.Invoke();
        if (removedPlanks >= TotalPlanks)
        {
            IsComplete = true; if (openingBlocker != null) openingBlocker.enabled = false;
            toolInteraction?.SetCanInteract(false); onAllPlanksRemoved.Invoke();
            Tell("The planks are off. Press " + PlayerOptions.KeyName(PlayerOptions.GetKey(GameplayAction.Interact)) + " to crawl through the opening."); Exit();
        }
        else { ShowHeldCrowbar(); UpdateUI(); ShowPlankSelectionInstruction(); }
    }

    private void SetBar(float progress)
    {
        if (progressFill == null) return;
        progressFill.fillAmount = progress;
        progressFill.rectTransform.anchorMax = new Vector2(progress, 1f);
    }

    private void UpdateUI()
    {
        SetBar(MashProgress);
        UpdateSpacebarFlash();
    }

    public void Exit()
    {
        if (!IsRunning || stage == PryStage.Returning || stage == PryStage.Ripping) return;
        if (toolRoutine != null) { StopCoroutine(toolRoutine); toolRoutine = null; }
        ResetRemainingPlanks(); selectedPlank = hoveredPlank = -1; mashForce = 0f;
        outline?.Clear(); DestroyCrowbar(); stage = PryStage.Returning;
        if (minigameUI != null) minigameUI.SetActive(false);
        if (cameraRoutine != null) StopCoroutine(cameraRoutine);
        cameraRoutine = StartCoroutine(MoveCamera(true));
    }

    private void ResetRemainingPlanks()
    {
        if (removed == null) return;
        for (int i = 0; i < TotalPlanks; i++) if (!removed[i] && planks[i] != null)
            planks[i].SetPositionAndRotation(restPositions[i], restRotations[i]);
    }

    public void CancelForExternalSequence()
    {
        if (!IsRunning) return;
        StopAllCoroutines(); cameraRoutine = toolRoutine = null;
        ResetRemainingPlanks(); RestoreControls();
    }

    private void RestoreControls()
    {
        outline?.Clear(); DestroyCrowbar(); selectedPlank = hoveredPlank = -1; mashForce = 0f;
        if (view != null) { view.transform.localPosition = cameraLocalPosition; view.transform.localRotation = cameraLocalRotation; view.fieldOfView = cameraFov; }
        if (controller != null) { controller.enabled = controllerEnabled; controller.UnlockMovement(this); }
        interactor?.UnlockInteraction(this);
        if (bobs != null) for (int i = 0; i < bobs.Length; i++) if (bobs[i] != null) bobs[i].enabled = bobStates[i];
        Cursor.lockState = savedCursorLock; Cursor.visible = savedCursorVisible;
        if (minigameUI != null) minigameUI.SetActive(false);
        IsRunning = false; stage = PryStage.Inactive;
        spacebarFlashUntil = float.NegativeInfinity;
        UpdateSpacebarFlash();
    }

    private void OnDisable()
    {
        CancelForExternalSequence();
        if (minigameUI != null) minigameUI.SetActive(false);
    }

    private void UpdateSpacebarFlash()
    {
        if (spacebarIcon == null) return;
        bool flashing = IsRunning && flashSpacebarIcon
            && (stage == PryStage.Prying || stage == PryStage.Ripping)
            && Time.time < spacebarFlashUntil;
        Color color = flashing ? spacebarFlashColor : spacebarOriginalColor;
        color.a = spacebarOriginalColor.a;
        spacebarIcon.color = color;
    }

    private void ShowPlankSelectionInstruction() => Tell("Click on plank to put the crowbar onto.");

    private void Tell(string message) => NotificationManager.Instance?.ShowMessage(message);
}