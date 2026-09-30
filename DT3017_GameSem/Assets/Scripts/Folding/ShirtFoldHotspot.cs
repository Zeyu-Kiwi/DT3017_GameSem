using UnityEngine;

public enum ShirtFoldPart
{
    Right,
    Left,
    Bottom,
    Collect
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ShirtFoldHotspot : MonoBehaviour, IInteractable
{
    [SerializeField] private ShirtFoldingController foldingController;
    [SerializeField] private ShirtFoldPart part;

    private Collider hotspotCollider;

    public ShirtFoldPart Part => part;
    public bool CanInteract =>
        foldingController != null && foldingController.CanInteractWith(part);

    private void Awake()
    {
        hotspotCollider = GetComponent<Collider>();

        if (foldingController == null)
        {
            foldingController = GetComponentInParent<ShirtFoldingController>();
        }
    }

    public void Interact(GameObject player)
    {
        if (foldingController != null)
        {
            foldingController.InteractWith(part);
        }
    }

    public void SetColliderEnabled(bool value)
    {
        if (hotspotCollider == null)
        {
            hotspotCollider = GetComponent<Collider>();
        }

        hotspotCollider.enabled = value;
    }
}
