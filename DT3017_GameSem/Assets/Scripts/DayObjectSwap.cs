using UnityEngine;

/// <summary>
/// Swaps two scene objects when the configured day begins.
/// Earlier days restore the objects to the states they had when the scene loaded.
/// </summary>
[DisallowMultipleComponent]
public class DayObjectSwap : MonoBehaviour
{
    [Header("Day")]
    [SerializeField] private DayCycleManager dayCycleManager;
    [SerializeField, Min(1)] private int changeOnDay = 6;

    [Header("Objects")]
    [SerializeField] private GameObject objectToDeactivate;
    [SerializeField] private GameObject objectToActivate;

    private bool deactivateObjectOriginalState;
    private bool activateObjectOriginalState;

    private void Awake()
    {
        if (dayCycleManager == null)
            dayCycleManager = FindFirstObjectByType<DayCycleManager>();

        if (objectToDeactivate != null)
            deactivateObjectOriginalState = objectToDeactivate.activeSelf;
        if (objectToActivate != null)
            activateObjectOriginalState = objectToActivate.activeSelf;
    }

    private void OnEnable()
    {
        if (dayCycleManager == null)
            dayCycleManager = FindFirstObjectByType<DayCycleManager>();
        if (dayCycleManager != null)
            dayCycleManager.DayStarted += ApplyForDay;
    }

    private void Start()
    {
        if (dayCycleManager == null)
        {
            Debug.LogError("DayObjectSwap could not find a DayCycleManager.", this);
            return;
        }

        ApplyForDay(dayCycleManager.CurrentDay);
    }

    private void OnDisable()
    {
        if (dayCycleManager != null)
            dayCycleManager.DayStarted -= ApplyForDay;
    }

    private void ApplyForDay(int day)
    {
        bool shouldSwap = day >= changeOnDay;

        if (objectToDeactivate != null)
        {
            objectToDeactivate.SetActive(
                shouldSwap ? false : deactivateObjectOriginalState);
        }

        if (objectToActivate != null)
        {
            objectToActivate.SetActive(
                shouldSwap ? true : activateObjectOriginalState);
        }
    }

    private void OnValidate()
    {
        changeOnDay = Mathf.Max(1, changeOnDay);
    }
}
