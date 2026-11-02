using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionOutlineHighlighter))]
public sealed class DrawerTutorialGlow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayCycleManager dayCycle;
    [SerializeField] private CraftingDrawerController drawerController;
    [Tooltip("The component targeted when the player looks at the drawer. Usually CraftingStation.")]
    [SerializeField] private Component drawerInteractable;
    [Tooltip("The InteractionOutlineHighlighter on the Player. It is discovered automatically when empty.")]
    [SerializeField] private InteractionOutlineHighlighter playerOutline;

    [Header("Tutorial Day")]
    [SerializeField, Min(1)] private int tutorialDay = 2;

    [Header("Breathing Outline")]
    [SerializeField, ColorUsage(true, true)]
    private Color outlineColor = new Color(1f, 0.75f, 0.15f, 1f);
    [SerializeField, Range(0f, 12f)] private float minimumThickness = 1.5f;
    [SerializeField, Range(0f, 12f)] private float maximumThickness = 6f;
    [Tooltip("Seconds for one complete thin-to-thick-to-thin cycle.")]
    [SerializeField, Min(0.1f)] private float breathingDuration = 1.5f;
    [SerializeField, Min(0f)] private float emissionIntensity = 3f;

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool tutorialGlowActive;
    [SerializeField] private bool completedThisSession;

    private InteractionOutlineHighlighter tutorialOutline;
    private bool tutorialOutlineVisible;
    private bool wasDrawerOpen;

    private void Awake()
    {
        tutorialOutline = GetComponent<InteractionOutlineHighlighter>();
        ResolveReferences();
        HideTutorialOutline();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (dayCycle != null)
        {
            dayCycle.DayStarted += HandleDayStarted;
        }
    }

    private void Start()
    {
        ResolveReferences();
        EvaluateDay(dayCycle == null ? -1 : dayCycle.CurrentDay);
    }

    private void OnDisable()
    {
        if (dayCycle != null)
        {
            dayCycle.DayStarted -= HandleDayStarted;
        }

        HideTutorialOutline();
    }

    private void Update()
    {
        bool drawerOpen = drawerController != null && drawerController.IsDrawerOpen;

        if (tutorialGlowActive && !wasDrawerOpen && drawerOpen)
        {
            CompleteTutorialGlow();
        }

        wasDrawerOpen = drawerOpen;

        if (!tutorialGlowActive || completedThisSession || drawerOpen ||
            IsNormalInteractionOutlineShowing())
        {
            HideTutorialOutline();
            return;
        }

        ShowTutorialOutline();

        float duration = Mathf.Max(0.1f, breathingDuration);
        float phase = Mathf.Repeat(Time.time / duration, 1f);
        float smoothPulse = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
        float minimum = Mathf.Min(minimumThickness, maximumThickness);
        float maximum = Mathf.Max(minimumThickness, maximumThickness);
        float thickness = Mathf.Lerp(minimum, maximum, smoothPulse);

        tutorialOutline.SetAppearance(outlineColor, thickness, emissionIntensity);
    }

    private void ResolveReferences()
    {
        if (dayCycle == null)
        {
            dayCycle = FindFirstObjectByType<DayCycleManager>();
        }

        if (drawerController == null)
        {
            drawerController = GetComponentInParent<CraftingDrawerController>();
        }

        if (drawerInteractable == null)
        {
            drawerInteractable = GetComponentInParent<CraftingStation>();
        }

        if (playerOutline == null)
        {
            PlayerInteractor playerInteractor = FindFirstObjectByType<PlayerInteractor>();
            if (playerInteractor != null)
            {
                playerOutline = playerInteractor.GetComponent<InteractionOutlineHighlighter>();
            }
        }
    }

    private void HandleDayStarted(int day)
    {
        EvaluateDay(day);
    }

    private void EvaluateDay(int day)
    {
        tutorialGlowActive = !completedThisSession && day == tutorialDay;
        wasDrawerOpen = drawerController != null && drawerController.IsDrawerOpen;

        if (!tutorialGlowActive)
        {
            HideTutorialOutline();
        }
    }

    private bool IsNormalInteractionOutlineShowing()
    {
        if (playerOutline == null)
        {
            ResolveReferences();
        }

        Component normalTarget = playerOutline == null ? null : playerOutline.Target;
        if (normalTarget == null)
        {
            return false;
        }

        Transform expected = drawerInteractable == null
            ? transform
            : drawerInteractable.transform;
        Transform actual = normalTarget.transform;

        return actual == expected ||
               actual.IsChildOf(expected) ||
               expected.IsChildOf(actual);
    }

    private void ShowTutorialOutline()
    {
        if (tutorialOutline == null)
        {
            tutorialOutline = GetComponent<InteractionOutlineHighlighter>();
        }

        if (tutorialOutlineVisible || tutorialOutline == null)
        {
            return;
        }

        tutorialOutline.SetAppearance(
            outlineColor,
            minimumThickness,
            emissionIntensity);
        tutorialOutline.SetTarget(this);
        tutorialOutlineVisible = true;
    }

    private void HideTutorialOutline()
    {
        if (tutorialOutline != null && tutorialOutline.Target != null)
        {
            tutorialOutline.Clear();
        }

        tutorialOutlineVisible = false;
    }

    private void CompleteTutorialGlow()
    {
        completedThisSession = true;
        tutorialGlowActive = false;
        HideTutorialOutline();
    }

    private void OnValidate()
    {
        tutorialDay = Mathf.Max(1, tutorialDay);
        minimumThickness = Mathf.Clamp(minimumThickness, 0f, 12f);
        maximumThickness = Mathf.Clamp(maximumThickness, 0f, 12f);
        breathingDuration = Mathf.Max(0.1f, breathingDuration);
        emissionIntensity = Mathf.Max(0f, emissionIntensity);
    }
}
