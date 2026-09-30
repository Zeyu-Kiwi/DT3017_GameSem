using UnityEngine;

[DisallowMultipleComponent]
public class ShirtFoldingController : MonoBehaviour
{
    private enum FoldStage
    {
        Right,
        Left,
        Bottom,
        WaitingToCollect,
        Collected
    }

    private static readonly int FoldRightTrigger = Animator.StringToHash("FoldRight");
    private static readonly int FoldLeftTrigger = Animator.StringToHash("FoldLeft");
    private static readonly int FoldBottomTrigger = Animator.StringToHash("FoldBottom");

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Interaction Hotspots")]
    [Tooltip("Leave empty to find every ShirtFoldHotspot below this object automatically.")]
    [SerializeField] private ShirtFoldHotspot[] hotspots;

    [Header("Runtime State (Debug)")]
    [SerializeField] private FoldStage currentStage = FoldStage.Right;
    [SerializeField] private bool animationPlaying;
    [SerializeField] private ShirtFoldPart animationPart;

    private ClothesPileInteractable clothesPile;
    private ItemData shirtItem;

    public bool AnimationPlaying => animationPlaying;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        FindHotspotsIfNeeded();
        RefreshHotspotColliders();
    }

    public void Initialize(ClothesPileInteractable owner, ItemData item)
    {
        clothesPile = owner;
        shirtItem = item;
        currentStage = FoldStage.Right;
        animationPlaying = false;
        RefreshHotspotColliders();
    }

    public bool CanInteractWith(ShirtFoldPart part)
    {
        if (animationPlaying || currentStage == FoldStage.Collected)
        {
            return false;
        }

        if (currentStage == FoldStage.WaitingToCollect)
        {
            return part == ShirtFoldPart.Collect;
        }

        return part != ShirtFoldPart.Collect;
    }

    public void InteractWith(ShirtFoldPart part)
    {
        if (!CanInteractWith(part))
        {
            return;
        }

        if (currentStage == FoldStage.WaitingToCollect)
        {
            TryCollectFinishedShirt();
            return;
        }

        ShirtFoldPart requiredPart = GetRequiredPart();
        if (part != requiredPart)
        {
            ShowWrongOrderMessage(requiredPart);
            return;
        }

        StartFoldAnimation(part);
    }

    private void StartFoldAnimation(ShirtFoldPart part)
    {
        if (animator == null)
        {
            Debug.LogError("The folding shirt needs an Animator.", this);
            return;
        }

        animationPlaying = true;
        animationPart = part;
        RefreshHotspotColliders();

        switch (part)
        {
            case ShirtFoldPart.Right:
                animator.SetTrigger(FoldRightTrigger);
                break;

            case ShirtFoldPart.Left:
                animator.SetTrigger(FoldLeftTrigger);
                break;

            case ShirtFoldPart.Bottom:
                animator.SetTrigger(FoldBottomTrigger);
                break;
        }
    }

    // Call this through an Animation Event at the final frame of each fold clip.
    public void OnFoldAnimationFinished()
    {
        if (!animationPlaying)
        {
            return;
        }

        animationPlaying = false;

        switch (animationPart)
        {
            case ShirtFoldPart.Right:
                currentStage = FoldStage.Left;
                break;

            case ShirtFoldPart.Left:
                currentStage = FoldStage.Bottom;
                break;

            case ShirtFoldPart.Bottom:
                currentStage = FoldStage.WaitingToCollect;
                TryCollectFinishedShirt();
                return;
        }

        RefreshHotspotColliders();
    }

    private void TryCollectFinishedShirt()
    {
        if (shirtItem == null || InventoryManager.Instance == null)
        {
            Debug.LogError(
                "The folded shirt could not be collected because its ItemData or InventoryManager is missing.",
                this);
            RefreshHotspotColliders();
            return;
        }

        int added = InventoryManager.Instance.AddItem(shirtItem, 1);
        if (added == 0)
        {
            ShowMessage(shirtItem.DisplayName + " is full.");
            RefreshHotspotColliders();
            return;
        }

        currentStage = FoldStage.Collected;
        RefreshHotspotColliders();

        if (clothesPile != null)
        {
            clothesPile.NotifyShirtCollected(this);
        }

        Destroy(gameObject);
    }

    private ShirtFoldPart GetRequiredPart()
    {
        switch (currentStage)
        {
            case FoldStage.Left:
                return ShirtFoldPart.Left;

            case FoldStage.Bottom:
                return ShirtFoldPart.Bottom;

            default:
                return ShirtFoldPart.Right;
        }
    }

    private void ShowWrongOrderMessage(ShirtFoldPart requiredPart)
    {
        switch (requiredPart)
        {
            case ShirtFoldPart.Left:
                ShowMessage("Fold the left side next.");
                break;

            case ShirtFoldPart.Bottom:
                ShowMessage("Fold the bottom last.");
                break;

            default:
                ShowMessage("Fold the right side first.");
                break;
        }
    }

    private void FindHotspotsIfNeeded()
    {
        if (hotspots == null || hotspots.Length == 0)
        {
            hotspots = GetComponentsInChildren<ShirtFoldHotspot>(true);
        }
    }

    private void RefreshHotspotColliders()
    {
        FindHotspotsIfNeeded();

        for (int i = 0; i < hotspots.Length; i++)
        {
            ShirtFoldHotspot hotspot = hotspots[i];
            if (hotspot == null)
            {
                continue;
            }

            bool shouldEnable = false;

            if (!animationPlaying && currentStage != FoldStage.Collected)
            {
                shouldEnable = currentStage == FoldStage.WaitingToCollect
                    ? hotspot.Part == ShirtFoldPart.Collect
                    : hotspot.Part != ShirtFoldPart.Collect;
            }

            hotspot.SetColliderEnabled(shouldEnable);
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
