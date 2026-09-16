using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingRecipeEntryUI : MonoBehaviour
{
    [Header("Ingredients")]
    [SerializeField] private Transform ingredientContainer;
    [SerializeField] private Image ingredientIconPrefab;
    [SerializeField] private Sprite unknownIcon;

    [Header("Result")]
    [SerializeField] private TMP_Text resultNameText;
    [SerializeField] private Image resultIcon;

    private readonly List<Image> generatedIngredientIcons = new List<Image>();

    public void Setup(
        CraftingRecipeData recipe,
        IReadOnlyDictionary<ItemData, int> selection,
        bool unlocked)
    {
        ClearIngredientIcons();

        Dictionary<ItemData, int> remainingSelected =
            new Dictionary<ItemData, int>();

        foreach (KeyValuePair<ItemData, int> selected in selection)
        {
            remainingSelected.Add(selected.Key, selected.Value);
        }

        IReadOnlyList<CraftingRecipeData.Ingredient> ingredients = recipe.Ingredients;
        for (int i = 0; i < ingredients.Count; i++)
        {
            CraftingRecipeData.Ingredient ingredient = ingredients[i];
            if (ingredient == null || ingredient.item == null)
            {
                continue;
            }

            for (int unit = 0; unit < ingredient.amount; unit++)
            {
                bool reveal = unlocked;

                if (!reveal &&
                    remainingSelected.TryGetValue(ingredient.item, out int remaining) &&
                    remaining > 0)
                {
                    reveal = true;
                    remainingSelected[ingredient.item] = remaining - 1;
                }

                CreateIngredientIcon(reveal ? ingredient.item.Icon : unknownIcon);
            }
        }

        if (resultNameText != null)
        {
            resultNameText.text = unlocked && recipe.Result != null
                ? recipe.DisplayName
                : "Unknown Recipe";
        }

        if (resultIcon != null)
        {
            resultIcon.sprite = unlocked && recipe.Result != null
                ? recipe.Result.Icon
                : unknownIcon;
        }
    }

    private void CreateIngredientIcon(Sprite sprite)
    {
        if (ingredientContainer == null || ingredientIconPrefab == null)
        {
            return;
        }

        Image icon = Instantiate(ingredientIconPrefab, ingredientContainer);
        icon.sprite = sprite;
        icon.gameObject.SetActive(true);
        generatedIngredientIcons.Add(icon);
    }

    private void ClearIngredientIcons()
    {
        for (int i = 0; i < generatedIngredientIcons.Count; i++)
        {
            if (generatedIngredientIcons[i] != null)
            {
                generatedIngredientIcons[i].gameObject.SetActive(false);
                Destroy(generatedIngredientIcons[i].gameObject);
            }
        }

        generatedIngredientIcons.Clear();
    }
}
