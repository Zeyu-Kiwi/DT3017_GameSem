using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "Crafting Recipe Database", menuName = "Game/Crafting/Recipe Database")]
public class CraftingRecipeDatabase : ScriptableObject
{
    [SerializeField] private List<CraftingRecipeData> recipes = new List<CraftingRecipeData>();

    public List<CraftingRecipeData> GetOrderedRecipes()
    {
        List<CraftingRecipeData> ordered = new List<CraftingRecipeData>();

        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i] != null)
            {
                ordered.Add(recipes[i]);
            }
        }

        ordered.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
        return ordered;
    }

    private void OnValidate()
    {
        Dictionary<string, CraftingRecipeData> recipesByIngredients =
            new Dictionary<string, CraftingRecipeData>();

        for (int i = 0; i < recipes.Count; i++)
        {
            CraftingRecipeData recipe = recipes[i];
            if (recipe == null)
            {
                continue;
            }

            string signature = CreateIngredientSignature(recipe);
            if (string.IsNullOrEmpty(signature))
            {
                Debug.LogWarning(recipe.name + " has no valid crafting ingredients.", recipe);
                continue;
            }

            if (recipesByIngredients.TryGetValue(signature, out CraftingRecipeData duplicate))
            {
                Debug.LogError(
                    recipe.name + " and " + duplicate.name +
                    " use the same ingredient combination. Exact crafting would be ambiguous.",
                    this);
            }
            else
            {
                recipesByIngredients.Add(signature, recipe);
            }

            if (!recipe.StartsUnlocked && recipe.UnlockFlag == null)
            {
                Debug.LogWarning(
                    recipe.name + " is locked but has no Unlock Flag. Its discovery will not " +
                    "survive a scene reload.",
                    recipe);
            }
        }
    }

    private string CreateIngredientSignature(CraftingRecipeData recipe)
    {
        List<string> parts = new List<string>();

        foreach (KeyValuePair<ItemData, int> ingredient in recipe.GetCombinedIngredients())
        {
            string id = ingredient.Key == null ? "<missing>" : ingredient.Key.ItemId;
            parts.Add(id + ":" + ingredient.Value);
        }

        parts.Sort();
        StringBuilder signature = new StringBuilder();

        for (int i = 0; i < parts.Count; i++)
        {
            signature.Append(parts[i]).Append('|');
        }

        return signature.ToString();
    }
}
