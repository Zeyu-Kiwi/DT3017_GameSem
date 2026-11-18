using UnityEngine;

[DisallowMultipleComponent]
public sealed class DrawerTutorialGlow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayCycleManager dayCycle;
    [SerializeField] private CraftingDrawerController drawerController;
    [Tooltip("The MeshRenderer on drawer_model, not the whole workstation.")]
    [SerializeField] private Renderer drawerRenderer;
    [Tooltip("The component targeted when the player looks at the drawer. Usually CraftingStation.")]
    [SerializeField] private Component drawerInteractable;
    [Tooltip("The InteractionOutlineHighlighter on the Player. It is discovered automatically when empty.")]
    [SerializeField] private InteractionOutlineHighlighter playerOutline;

    [Header("Tutorial Day")]
    [SerializeField, Min(1)] private int tutorialDay = 2;

    [Header("Breathing Material Glow")]
    [SerializeField, ColorUsage(true, true)]
    private Color glowColor = new Color(1f, 0.75f, 0.15f, 1f);
    [SerializeField, Min(0f)] private float minimumBrightness = 0.15f;
    [SerializeField, Min(0f)] private float maximumBrightness = 3f;
    [Tooltip("Seconds for one complete dim-to-bright-to-dim cycle.")]
    [SerializeField, Min(0.1f)] private float breathingDuration = 1.5f;

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool tutorialGlowActive;
    [SerializeField] private bool completedThisSession;

    private Material[] originalMaterials;
    private Material[] glowMaterials;
    private Color[] originalEmissionColors;
    private bool wasDrawerOpen;
    private bool warnedAboutMaterial;

    private void Awake()
    {
        ResolveReferences();
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

        RestoreDrawerMaterials();
    }

    private void Update()
    {
        bool drawerOpen = drawerController != null && drawerController.IsDrawerOpen;

        // Only a successful opening on the tutorial day dismisses its glow.
        // Earlier openings do not prevent the Day 2 reminder.
        if (tutorialGlowActive && !wasDrawerOpen && drawerOpen)
        {
            CompleteTutorialGlow();
        }

        wasDrawerOpen = drawerOpen;

        if (!tutorialGlowActive || completedThisSession || drawerOpen)
        {
            RestoreDrawerMaterials();
            return;
        }

        if (IsNormalInteractionOutlineShowing())
        {
            SetEmission(0f);
            return;
        }

        if (!PrepareDrawerMaterials()) return;

        float duration = Mathf.Max(0.1f, breathingDuration);
        float phase = Mathf.Repeat(Time.time / duration, 1f);
        float smoothPulse = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
        float minimum = Mathf.Min(minimumBrightness, maximumBrightness);
        float maximum = Mathf.Max(minimumBrightness, maximumBrightness);
        SetEmission(Mathf.Lerp(minimum, maximum, smoothPulse));
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

        if (drawerRenderer == null)
        {
            Transform drawerModel = transform.Find("drawer_model");
            if (drawerModel != null) drawerRenderer = drawerModel.GetComponent<Renderer>();
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
            RestoreDrawerMaterials();
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

    private bool PrepareDrawerMaterials()
    {
        if (glowMaterials != null) return true;
        if (drawerRenderer == null)
        {
            WarnAboutMaterial("DrawerTutorialGlow needs the drawer_model Renderer assigned.");
            return false;
        }

        originalMaterials = drawerRenderer.sharedMaterials;
        glowMaterials = new Material[originalMaterials.Length];
        originalEmissionColors = new Color[originalMaterials.Length];
        bool hasEmissionMaterial = false;

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            Material original = originalMaterials[i];
            if (original == null || !original.HasProperty("_EmissionColor"))
            {
                glowMaterials[i] = original;
                continue;
            }

            Material copy = new Material(original);
            copy.name = original.name + " (Drawer Tutorial Glow)";
            originalEmissionColors[i] = original.GetColor("_EmissionColor");
            copy.EnableKeyword("_EMISSION");
            glowMaterials[i] = copy;
            hasEmissionMaterial = true;
        }

        if (!hasEmissionMaterial)
        {
            WarnAboutMaterial("The drawer material does not support _EmissionColor. Assign an emission-capable material to drawer_model.");
            glowMaterials = null;
            originalMaterials = null;
            originalEmissionColors = null;
            return false;
        }

        drawerRenderer.sharedMaterials = glowMaterials;
        return true;
    }

    private void WarnAboutMaterial(string message)
    {
        if (warnedAboutMaterial) return;
        warnedAboutMaterial = true;
        Debug.LogWarning(message, this);
    }

    private void SetEmission(float brightness)
    {
        if (glowMaterials == null) return;
        for (int i = 0; i < glowMaterials.Length; i++)
        {
            Material material = glowMaterials[i];
            if (material != null && material != originalMaterials[i])
            {
                material.SetColor("_EmissionColor",
                    originalEmissionColors[i] + glowColor * brightness);
            }
        }
    }

    private void RestoreDrawerMaterials()
    {
        if (glowMaterials == null) return;
        if (drawerRenderer != null) drawerRenderer.sharedMaterials = originalMaterials;

        for (int i = 0; i < glowMaterials.Length; i++)
        {
            if (glowMaterials[i] != null && glowMaterials[i] != originalMaterials[i])
                Destroy(glowMaterials[i]);
        }

        glowMaterials = null;
        originalMaterials = null;
        originalEmissionColors = null;
    }

    private void CompleteTutorialGlow()
    {
        completedThisSession = true;
        tutorialGlowActive = false;
        RestoreDrawerMaterials();
    }

    private void OnValidate()
    {
        tutorialDay = Mathf.Max(1, tutorialDay);
        minimumBrightness = Mathf.Max(0f, minimumBrightness);
        maximumBrightness = Mathf.Max(0f, maximumBrightness);
        breathingDuration = Mathf.Max(0.1f, breathingDuration);
    }
}
