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
    [SerializeField] private int currentInventoryShirts;
    [SerializeField] private int currentRequiredQuota;

    public event Action QuotaChanged;

    public ItemData ShirtItem => shirtItem;
    public int CurrentInventoryShirts => currentInventoryShirts;
    public int DisplayedQuotaAmount => Mathf.Min(
        currentInventoryShirts,
        currentRequiredQuota);
    public int CurrentRequiredQuota => currentRequiredQuota;
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

        return InventoryManager.Instance.TryRemoveItem(
            shirtItem,
            currentRequiredQuota);
    }

    public void AddCaughtPenalty()
    {
        currentRequiredQuota += caughtPenalty;
        RefreshFromInventory();
    }

    public void StartNewDay()
    {
        currentRequiredQuota = Mathf.Max(1, defaultQuota);
        RefreshFromInventory();
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
