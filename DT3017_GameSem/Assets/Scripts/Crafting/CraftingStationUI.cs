using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CraftingStationUI : MonoBehaviour
{
    [SerializeField] private Canvas craftingCanvas;
    [SerializeField] private GameObject interfaceRoot;

    [Header("Recipe Window")]
    [SerializeField] private CraftingRecipePanelUI recipePanel;

    [Header("Hover")]
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
        IReadOnlyList<CraftingRecipeData> candidateRecipes,
        IReadOnlyDictionary<ItemData, int> selection)
    {
        if (!hasSelection)
        {
            CloseRecipePanel();
        }
        else if (recipePanel != null)
        {
            recipePanel.SetVisible(true);
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

    public void CloseRecipePanel()
    {
        if (recipePanel != null)
        {
            recipePanel.SetVisible(false);
        }
    }
}
