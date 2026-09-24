using System;
using TMPro;
using UnityEngine;

public class DailyQuotaManager : MonoBehaviour
{
    public static DailyQuotaManager Instance { get; private set; }

    [Header("Quota")]
    [SerializeField] private ItemData shirtItem;
    [SerializeField, Min(1)] private int defaultQuota = 5;
    [SerializeField, Min(0)] private int caughtPenalty = 5;

    [Header("UI")]
    [SerializeField] private TMP_Text quotaText;

    [Header("Runtime State (Debug)")]
    [SerializeField] private int submittedShirts;
    [SerializeField] private int currentRequiredQuota;
    [SerializeField] private bool dayComplete;

    public event Action QuotaChanged;
    public event Action QuotaCompleted;

    public ItemData ShirtItem => shirtItem;
    public int SubmittedShirts => submittedShirts;
    public int CurrentRequiredQuota => currentRequiredQuota;
    public int RemainingQuota => Mathf.Max(0, currentRequiredQuota - submittedShirts);
    public bool IsDayComplete => dayComplete;
    public bool CanSubmit => !dayComplete &&
                             shirtItem != null &&
                             InventoryManager.Instance != null &&
                             InventoryManager.Instance.GetQuantity(shirtItem) > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one DailyQuotaManager exists in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        StartNewDay();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int SubmitAvailableShirts()
    {
        if (!CanSubmit)
        {
            return 0;
        }

        InventoryManager inventory = InventoryManager.Instance;
        int amountToSubmit = Mathf.Min(
            inventory.GetQuantity(shirtItem),
            RemainingQuota);

        if (amountToSubmit <= 0 || !inventory.TryRemoveItem(shirtItem, amountToSubmit))
        {
            return 0;
        }

        submittedShirts += amountToSubmit;
        RefreshUI();
        QuotaChanged?.Invoke();

        if (submittedShirts >= currentRequiredQuota)
        {
            dayComplete = true;
            QuotaCompleted?.Invoke();
        }

        return amountToSubmit;
    }

    public void AddCaughtPenalty()
    {
        if (dayComplete)
        {
            return;
        }

        currentRequiredQuota += caughtPenalty;
        RefreshUI();
        QuotaChanged?.Invoke();
    }

    public void StartNewDay()
    {
        submittedShirts = 0;
        currentRequiredQuota = Mathf.Max(1, defaultQuota);
        dayComplete = false;
        RefreshUI();
        QuotaChanged?.Invoke();
    }

    private void RefreshUI()
    {
        if (quotaText != null)
        {
            quotaText.text = submittedShirts + "/" + currentRequiredQuota;
        }
    }

    private void OnValidate()
    {
        defaultQuota = Mathf.Max(1, defaultQuota);
        caughtPenalty = Mathf.Max(0, caughtPenalty);
    }
}
