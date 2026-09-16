using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipePanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private CraftingRecipeEntryUI entryPrefab;

    private readonly List<CraftingRecipeEntryUI> generatedEntries =
        new List<CraftingRecipeEntryUI>();

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    public void SetVisible(bool visible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
        }
    }

    public void Toggle()
    {
        SetVisible(!IsOpen);
    }

    public void Refresh(
        IReadOnlyList<CraftingRecipeData> recipes,
        IReadOnlyDictionary<ItemData, int> selection,
        CraftingStation station)
    {
        ClearEntries();

        if (entryContainer == null || entryPrefab == null)
        {
            return;
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            CraftingRecipeData recipe = recipes[i];
            CraftingRecipeEntryUI entry = Instantiate(entryPrefab, entryContainer);
            entry.Setup(recipe, selection, station.IsRecipeUnlocked(recipe));
            entry.gameObject.SetActive(true);
            generatedEntries.Add(entry);
        }
    }

    private void ClearEntries()
    {
        for (int i = 0; i < generatedEntries.Count; i++)
        {
            if (generatedEntries[i] != null)
            {
                generatedEntries[i].gameObject.SetActive(false);
                Destroy(generatedEntries[i].gameObject);
            }
        }

        generatedEntries.Clear();
    }
}
