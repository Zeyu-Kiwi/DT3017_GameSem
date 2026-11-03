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
    [Tooltip("When enabled, quota increases caused by failed days remain permanently. When disabled, completing a day resets the following day to Default Quota.")]
    [SerializeField] private bool failureIncreaseIsPermanent = true;

    [Header("UI")]
    [SerializeField] private TMP_Text quotaText;

    [Header("Runtime State (Debug)")]
    [SerializeField] private int currentInventoryShirts;
    [SerializeField] private int currentDailyBaseQuota;
    [SerializeField] private int currentRequiredQuota;

    public event Action QuotaChanged;

    public ItemData ShirtItem => shirtItem;
    public int CurrentInventoryShirts => currentInventoryShirts;
    public int DisplayedQuotaAmount => currentInventoryShirts;
    public int CurrentRequiredQuota => currentRequiredQuota;
    public int CurrentDailyBaseQuota => currentDailyBaseQuota;
    public int MissingShirts => Mathf.Max(
        0,
        currentRequiredQuota - currentInventoryShirts);
    public bool HasMetQuota => shirtItem != null &&
                               InventoryManager.Instance != null &&
                               currentInventoryShirts >= currentRequiredQuota;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one DailyQuotaManager exists in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        currentDailyBaseQuota = Mathf.Max(1, defaultQuota);
        StartNewDay();
    }

    private void Start()
    {
        SubscribeToInventory();
        RefreshFromInventory();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.QuantityChanged -= OnQuantityChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool ConsumeRequiredShirtsForDayEnd()
    {
        if (!HasMetQuota || InventoryManager.Instance == null)
        {
            return false;
        }

        if (!InventoryManager.Instance.TryRemoveItem(
                shirtItem,
                currentRequiredQuota))
        {
            return false;
        }

        ApplySuccessfulDayCompletion();
        return true;
    }

    public bool ResolveTimerExpiredDay(
        out int missingShirts,
        out int nextDailyBaseQuota)
    {
        RefreshFromInventory();
        missingShirts = MissingShirts;

        if (missingShirts <= 0)
        {
            bool consumed = ConsumeRequiredShirtsForDayEnd();
            nextDailyBaseQuota = currentDailyBaseQuota;
            return consumed;
        }

        if (InventoryManager.Instance == null || shirtItem == null)
        {
            nextDailyBaseQuota = currentDailyBaseQuota;
            return false;
        }

        int shirtsToRemove = currentInventoryShirts;
        if (shirtsToRemove > 0 &&
            !InventoryManager.Instance.TryRemoveItem(shirtItem, shirtsToRemove))
        {
            nextDailyBaseQuota = currentDailyBaseQuota;
            return false;
        }

        // Boss penalties increase today's required amount, but do not directly
        // become part of the daily base. Only the amount still missing carries on.
        currentDailyBaseQuota = Mathf.Max(
            1,
            currentDailyBaseQuota + missingShirts);
        nextDailyBaseQuota = currentDailyBaseQuota;
        return true;
    }

    public void AddCaughtPenalty()
    {
        AddPenalty(caughtPenalty);
    }

    public void AddPenalty(int amount)
    {
        currentRequiredQuota += Mathf.Max(0, amount);
        RefreshFromInventory();
    }

    public void StartNewDay()
    {
        if (currentDailyBaseQuota <= 0)
        {
            currentDailyBaseQuota = Mathf.Max(1, defaultQuota);
        }

        currentRequiredQuota = currentDailyBaseQuota;
        RefreshFromInventory();
    }

    private void ApplySuccessfulDayCompletion()
    {
        if (!failureIncreaseIsPermanent)
        {
            currentDailyBaseQuota = Mathf.Max(1, defaultQuota);
        }
    }

    private void SubscribeToInventory()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError(
                "DailyQuotaManager needs an InventoryManager in the scene.",
                this);
            return;
        }

        InventoryManager.Instance.QuantityChanged -= OnQuantityChanged;
        InventoryManager.Instance.QuantityChanged += OnQuantityChanged;
    }

    private void OnQuantityChanged(ItemData changedItem, int newQuantity)
    {
        if (changedItem == shirtItem)
        {
            RefreshFromInventory();
        }
    }

    private void RefreshFromInventory()
    {
        currentInventoryShirts =
            shirtItem != null && InventoryManager.Instance != null
                ? InventoryManager.Instance.GetQuantity(shirtItem)
                : 0;

        RefreshUI();
        QuotaChanged?.Invoke();
    }

    private void RefreshUI()
    {
        if (quotaText != null)
        {
            quotaText.text = DisplayedQuotaAmount + "/" + currentRequiredQuota;
        }
    }

    private void OnValidate()
    {
        defaultQuota = Mathf.Max(1, defaultQuota);
        caughtPenalty = Mathf.Max(0, caughtPenalty);
    }
}
