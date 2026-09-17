using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingStationUI : MonoBehaviour
{
    [SerializeField] private Canvas craftingCanvas;
    [SerializeField] private GameObject interfaceRoot;

    [Header("Gear")]
    [SerializeField] private Button gearButton;
    [SerializeField] private Image gearImage;
    [SerializeField] private Color recipeAvailableColor = Color.gray;
    [SerializeField] private Color craftableColor = Color.green;

    [Header("Recipe Window")]
    [SerializeField] private CraftingRecipePanelUI recipePanel;
    [SerializeField] private Button recipeCloseButton;

    [Header("Other Controls")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text hoveredItemNameText;

    private CraftingStation station;

    private void Awake()
    {
        if (craftingCanvas == null)
        {
            craftingCanvas = GetComponentInParent<Canvas>();
        }

        if (interfaceRoot == null)
        {
            Debug.LogError("CraftingStationUI has no Interface Root assigned.", this);
        }
        else if (interfaceRoot == gameObject)
        {
            Debug.LogWarning(
                "Interface Root should be a separate child object, not the object that owns " +
                "CraftingStationUI.",
                this);
        }

        if (closeButton == null)
        {
            Debug.LogWarning("CraftingStationUI has no Close Button assigned.", this);
        }

        if (gearButton == null || gearImage == null)
        {
            Debug.LogWarning("CraftingStationUI gear references are incomplete.", this);
        }

        if (gearButton != null)
        {
            gearButton.onClick.AddListener(OnGearClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (recipeCloseButton != null)
        {
            recipeCloseButton.onClick.AddListener(CloseRecipePanel);
        }

        SetOpen(false);
    }

    public void Initialize(CraftingStation owningStation)
    {
        station = owningStation;
    }

    public void SetRenderCamera(Camera renderCamera)
    {
        if (craftingCanvas != null &&
            craftingCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            craftingCanvas.worldCamera = renderCamera;
        }
    }

    public void SetOpen(bool open)
    {
        if (interfaceRoot != null)
        {
            interfaceRoot.SetActive(open);
        }

        if (!open)
        {
            CloseRecipePanel();
            ShowHoveredItem(null);
        }
    }

    public void Refresh(
        bool hasSelection,
        bool canCraft,
        IReadOnlyList<CraftingRecipeData> candidateRecipes,
        IReadOnlyDictionary<ItemData, int> selection)
    {
        if (gearButton != null)
        {
            gearButton.gameObject.SetActive(hasSelection);
            gearButton.interactable = hasSelection;
        }

        if (gearImage != null)
        {
            gearImage.color = canCraft ? craftableColor : recipeAvailableColor;
        }

        if (!hasSelection)
        {
            CloseRecipePanel();
        }
        else if (recipePanel != null && recipePanel.IsOpen)
        {
            recipePanel.Refresh(candidateRecipes, selection, station);
        }
    }

    public void ShowHoveredItem(ItemData item)
    {
        if (hoveredItemNameText == null)
        {
            return;
        }

        bool show = item != null;
        hoveredItemNameText.gameObject.SetActive(show);
        hoveredItemNameText.text = show ? item.DisplayName : string.Empty;
    }

    private void OnGearClicked()
    {
        if (station != null)
        {
            station.HandleGearPressed();
        }
    }

    private void OnCloseClicked()
    {
        if (station != null)
        {
            station.CloseCrafting();
        }
    }

    public void ToggleRecipePanel(
        IReadOnlyList<CraftingRecipeData> candidateRecipes,
        IReadOnlyDictionary<ItemData, int> selection)
    {
        if (recipePanel == null)
        {
            return;
        }

        recipePanel.Toggle();
        if (recipePanel.IsOpen)
        {
            recipePanel.Refresh(candidateRecipes, selection, station);
        }
    }

    public void CloseRecipePanel()
    {
        if (recipePanel != null)
        {
            recipePanel.SetVisible(false);
        }
    }
}
