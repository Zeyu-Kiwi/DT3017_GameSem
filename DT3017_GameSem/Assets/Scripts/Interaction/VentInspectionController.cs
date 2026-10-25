using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class VentInspectionController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private Transform inspectionPoint;
    [SerializeField] private VentScrew[] screws;
    [SerializeField] private Transform coverVisuals;
    [SerializeField] private Collider coverCollider;
    [SerializeField] private GameObject screwdriverVisualTemplate;
    [SerializeField] private UnityEvent onCoverRemoved = new UnityEvent();
    [Header("Camera")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.55f;
    [SerializeField, Range(30f, 90f)] private float inspectionFieldOfView = 55f;
    [Header("Held Screwdriver")]
    [Tooltip("Camera-local position of the handle grip, rather than the screwdriver tip.")]
    [SerializeField] private Vector3 heldPosition = new Vector3(0.30f, -0.255f, 0.45f);
    [SerializeField] private Vector3 heldRotation = new Vector3(50f, 147f, 0f);
    [Tooltip("Grip point relative to the tip-centred screwdriver template. Does not change screw attachment.")]
    [SerializeField] private Vector3 heldGripLocalPosition = new Vector3(0f, 0f, 0.29f);
    [Header("Circular Mouse Input")]
    [SerializeField, Min(1f)] private float minimumCursorRadius = 18f;
    [SerializeField, Min(20f)] private float maximumCursorRadius = 220f;
    [SerializeField, Range(10f, 120f)] private float maximumStepDegrees = 85f;

    private Camera view;
    private FirstPersonController controller;
    private PlayerInteractor interactor;
    private InteractionOutlineHighlighter outline;
    private HeadBob[] bobs;
    private bool[] bobStates;
    private bool controllerWasEnabled;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private float cameraFov;
    private CursorLockMode cursorLock;
    private bool cursorVisible;
    private GameObject heldTool;
    private GameObject attachedTool;
    private VentScrew hoveredScrew;
    private VentScrew activeScrew;
    private bool hoveringCover;
    private bool ready;
    private bool returning;
    private bool hasPreviousAngle;
    private float previousAngle;
    private Coroutine cameraRoutine;
    public bool IsInspecting { get; private set; }
    public bool IsComplete { get; private set; }
    public int RemainingScrews
    {
        get { int count = 0; foreach (var screw in screws) if (screw != null && !screw.IsRemoved) count++; return count; }
    }

    public void Begin(GameObject player)
    {
        if (IsInspecting || IsComplete || player == null || inspectionPoint == null) return;
        view = player.GetComponentInChildren<Camera>();
        if (view == null) return;
        controller = player.GetComponent<FirstPersonController>();
        interactor = player.GetComponent<PlayerInteractor>();
        outline = player.GetComponent<InteractionOutlineHighlighter>();
        cameraPosition = view.transform.localPosition;
        cameraRotation = view.transform.localRotation;
        cameraFov = view.fieldOfView;
        cursorLock = Cursor.lockState;
        cursorVisible = Cursor.visible;
        bobs = player.GetComponentsInChildren<HeadBob>(true);
        bobStates = new bool[bobs.Length];
        for (int i = 0; i < bobs.Length; i++) { bobStates[i] = bobs[i].enabled; bobs[i].enabled = false; }
        if (controller != null)
        {
            controllerWasEnabled = controller.enabled;
            controller.LockMovement(this, true);
            controller.enabled = false;
        }
        interactor?.LockInteraction(this);
        outline?.Clear();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        IsInspecting = true;
        returning = false;
        cameraRoutine = StartCoroutine(MoveCamera(false));
    }

    private IEnumerator MoveCamera(bool back)
    {
        ready = false;
        Vector3 start = view.transform.position;
        Quaternion rotation = view.transform.rotation;
        float fov = view.fieldOfView;
        Vector3 target = back ? view.transform.parent.TransformPoint(cameraPosition) : inspectionPoint.position;
        Quaternion targetRotation = back ? view.transform.parent.rotation * cameraRotation : inspectionPoint.rotation;
        float targetFov = back ? cameraFov : inspectionFieldOfView;
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0.01f, transitionDuration))
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, transitionDuration)));
            view.transform.SetPositionAndRotation(Vector3.Lerp(start, target, t), Quaternion.Slerp(rotation, targetRotation, t));
            view.fieldOfView = Mathf.Lerp(fov, targetFov, t);
            yield return null;
        }
        view.transform.SetPositionAndRotation(target, targetRotation);
        view.fieldOfView = targetFov;
        cameraRoutine = null;
        if (back) RestoreControls();
        else { ready = true; ShowHeldTool(); Tell("Click a screw, then move the cursor clockwise around it. Escape to leave."); }
    }

    private void Update()
    {
        if (!IsInspecting || returning) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Exit(); return; }
        if (!ready) return;
        if (activeScrew != null) { UpdateTurning(Input.mousePosition); return; }
        UpdateHover(Input.mousePosition);
        if (!Input.GetMouseButtonDown(0)) return;
        if (hoveredScrew != null) SelectScrew(hoveredScrew);
        else if (hoveringCover)
        {
            if (RemainingScrews == 0) RemoveCover();
            else Tell("Remove all " + RemainingScrews + " remaining screws first.");
        }
    }

    private void UpdateHover(Vector2 cursor)
    {
        hoveredScrew = null;
        hoveringCover = false;
        var hits = Physics.RaycastAll(view.ScreenPointToRay(cursor), 4f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider == coverCollider) { hoveringCover = true; continue; }
            var screw = hit.collider.GetComponentInParent<VentScrew>();
            if (screw != null && System.Array.IndexOf(screws, screw) >= 0 && !screw.IsRemoved) hoveredScrew = screw;
            break;
        }
        outline?.SetTarget(hoveredScrew);
    }

    private void SelectScrew(VentScrew screw)
    {
        if (!ready || activeScrew != null || screw == null || screw.IsRemoved || System.Array.IndexOf(screws, screw) < 0) return;
        activeScrew = screw;
        hasPreviousAngle = false;
        outline?.Clear();
        if (heldTool != null) heldTool.SetActive(false);
        attachedTool = MakeTool(screw.transform);
        Tell("Move the cursor clockwise around the screw to loosen it.");
    }

    private void UpdateTurning(Vector2 cursor)
    {
        if (activeScrew == null) return;
        Vector3 screen = view.WorldToScreenPoint(activeScrew.transform.position);
        if (screen.z <= 0f) { hasPreviousAngle = false; return; }
        Vector2 offset = cursor - new Vector2(screen.x, screen.y);
        if (offset.magnitude < minimumCursorRadius || offset.magnitude > maximumCursorRadius)
        { hasPreviousAngle = false; return; }
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        if (hasPreviousAngle)
        {
            float delta = Mathf.DeltaAngle(previousAngle, angle);
            if (Mathf.Abs(delta) <= maximumStepDegrees && activeScrew.Turn(-delta)) FinishScrew();
        }
        previousAngle = angle;
        hasPreviousAngle = true;
    }

    private void FinishScrew()
    {
        if (attachedTool != null) Destroy(attachedTool);
        activeScrew.Drop();
        activeScrew = null;
        hasPreviousAngle = false;
        ShowHeldTool();
        Tell(RemainingScrews == 0 ? "All screws removed. Click the vent cover to remove it." : RemainingScrews + " screws remaining.");
    }

    private GameObject MakeTool(Transform parent)
    {
        if (screwdriverVisualTemplate == null) return null;
        var tool = Instantiate(screwdriverVisualTemplate, parent, false);
        tool.name = "Held Phillips Screwdriver";
        tool.SetActive(true);
        return tool;
    }

    private void ShowHeldTool()
    {
        if (heldTool == null) heldTool = MakeTool(view.transform);
        if (heldTool == null) return;
        Quaternion rotation = Quaternion.Euler(heldRotation);
        heldTool.transform.localRotation = rotation;
        heldTool.transform.localPosition = heldPosition - rotation * heldGripLocalPosition;
        heldTool.SetActive(RemainingScrews > 0);
    }

    private void RemoveCover()
    {
        if (!ready || IsComplete || RemainingScrews > 0) return;
        IsComplete = true;
        if (coverCollider != null) coverCollider.enabled = false;
        if (coverVisuals != null)
        {
            coverVisuals.SetParent(null, true);
            var body = coverVisuals.gameObject.AddComponent<Rigidbody>();
            body.mass = 1f;
            var collider = coverVisuals.gameObject.AddComponent<BoxCollider>();
            var renderer = coverVisuals.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                collider.center = coverVisuals.InverseTransformPoint(renderer.bounds.center);
                Vector3 size = renderer.bounds.size;
                collider.size = new Vector3(Mathf.Max(size.x,0.03f),Mathf.Max(size.y,0.03f),Mathf.Max(size.z,0.03f));
            }
            body.AddForce(inspectionPoint.forward * -0.5f, ForceMode.VelocityChange);
            Destroy(coverVisuals.gameObject, 15f);
        }
        onCoverRemoved.Invoke();
        Tell("Vent cover removed.");
        Exit();
    }

    public void Exit()
    {
        if (!IsInspecting || returning) return;
        returning = true;
        ready = false;
        outline?.Clear();
        if (heldTool != null) Destroy(heldTool);
        if (attachedTool != null) Destroy(attachedTool);
        activeScrew = null;
        hoveredScrew = null;
        if (cameraRoutine != null) StopCoroutine(cameraRoutine);
        cameraRoutine = StartCoroutine(MoveCamera(true));
    }

    private void RestoreControls()
    {
        if (view != null)
        {
            view.transform.localPosition = cameraPosition;
            view.transform.localRotation = cameraRotation;
            view.fieldOfView = cameraFov;
        }
        if (controller != null) { controller.UnlockMovement(this); controller.enabled = controllerWasEnabled; }
        interactor?.UnlockInteraction(this);
        if (bobs != null) for (int i = 0; i < bobs.Length; i++) if (bobs[i] != null) bobs[i].enabled = bobStates[i];
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorVisible;
        IsInspecting = false;
        ready = false;
        returning = false;
    }

    private void OnDisable()
    {
        if (!IsInspecting) return;
        StopAllCoroutines();
        outline?.Clear();
        if (heldTool != null) Destroy(heldTool);
        if (attachedTool != null) Destroy(attachedTool);
        activeScrew = null;
        RestoreControls();
    }

    private void Tell(string message) { NotificationManager.Instance?.ShowMessage(message); }
}