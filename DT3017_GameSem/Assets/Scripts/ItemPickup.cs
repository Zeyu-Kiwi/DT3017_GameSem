using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int quantity = 1;

    [Header("Item Shine")]
    [Tooltip("Show sparkles and a diagonal sheen while this pickup is in the world. Add Item Shine Effect to customize its appearance.")]
    [SerializeField] private bool itemShineEnabled = true;

    private void Awake()
    {
        var effect = GetComponent<ItemShineEffect>();
        if (effect == null && itemShineEnabled) effect = gameObject.AddComponent<ItemShineEffect>();
        if (effect != null) effect.enabled = itemShineEnabled;
    }

    [Header("Interaction Outline")]
    [Tooltip("Use this item's outline color instead of the player's default highlight color.")]
    [SerializeField] private bool useCustomOutlineColor;
    [SerializeField, ColorUsage(true, true)] private Color outlineColor = new Color(1f, .75f, .15f, 1f);

    [Tooltip("Override the player's default outline emission intensity for this item.")]
    [SerializeField] private bool useCustomOutlineEmission;
    [SerializeField, Min(0f)] private float outlineEmissionIntensity;

    public bool UseCustomOutlineEmission => useCustomOutlineEmission;
    public float OutlineEmissionIntensity => Mathf.Max(0f, outlineEmissionIntensity);
    public bool UseCustomOutlineColor => useCustomOutlineColor;
    public Color OutlineColor => outlineColor;
    public bool CanInteract => item != null && quantity > 0;

    public void Interact(GameObject player)
    {
        if (InventoryManager.Instance == null || item == null)
        {
            return;
        }

        int added = InventoryManager.Instance.AddItem(item, quantity);

        if (added == 0)
        {
            ShowMessage(item.DisplayName + " is full.");
            return;
        }

        quantity -= added;

        if (quantity > 0)
        {
            ShowMessage(
                "Picked up " + added + " " + item.DisplayName +
                ". " + item.DisplayName + " is now full.");
        }
        else
        {
            ShowMessage("Picked up " + added + " " + item.DisplayName + ".");
            Destroy(gameObject);
        }
    }

    private void ShowMessage(string message)
    {
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowMessage(message);
        }
        else
        {
            Debug.Log(message, this);
        }
    }
}
