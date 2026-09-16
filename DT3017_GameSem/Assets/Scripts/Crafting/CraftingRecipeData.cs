using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Game/Crafting/Crafting Recipe")]
public class CraftingRecipeData : ScriptableObject
{
    [Serializable]
    public class Ingredient
    {
        public ItemData item;
        [Min(1)] public int amount = 1;
    }

    [SerializeField] private string displayName = "New Recipe";
    [SerializeField] private List<Ingredient> ingredients = new List<Ingredient>();
    [SerializeField] private ItemData result;
    [SerializeField, Min(1)] private int resultAmount = 1;
    [SerializeField] private bool startsUnlocked;
    [SerializeField] private GameFlagData unlockFlag;
    [SerializeField] private int displayOrder;

    public string DisplayName => displayName;
    public IReadOnlyList<Ingredient> Ingredients => ingredients;
    public ItemData Result => result;
    public int ResultAmount => resultAmount;
    public bool StartsUnlocked => startsUnlocked;
    public GameFlagData UnlockFlag => unlockFlag;
    public int DisplayOrder => displayOrder;

    public Dictionary<ItemData, int> GetCombinedIngredients()
    {
        Dictionary<ItemData, int> combined = new Dictionary<ItemData, int>();

        for (int i = 0; i < ingredients.Count; i++)
        {
            Ingredient ingredient = ingredients[i];
            if (ingredient.item == null || ingredient.amount <= 0)
            {
                continue;
            }

            if (!combined.ContainsKey(ingredient.item))
            {
                combined.Add(ingredient.item, 0);
            }

            combined[ingredient.item] += ingredient.amount;
        }

        return combined;
    }

    public bool CanContain(IReadOnlyDictionary<ItemData, int> partialSelection)
    {
        Dictionary<ItemData, int> required = GetCombinedIngredients();

        foreach (KeyValuePair<ItemData, int> selected in partialSelection)
        {
            if (!required.TryGetValue(selected.Key, out int amountRequired) ||
                selected.Value > amountRequired)
            {
                return false;
            }
        }

        return true;
    }

    public bool ExactlyMatches(IReadOnlyDictionary<ItemData, int> selection)
    {
        Dictionary<ItemData, int> required = GetCombinedIngredients();
        if (required.Count != selection.Count)
        {
            return false;
        }

        foreach (KeyValuePair<ItemData, int> ingredient in required)
        {
            if (!selection.TryGetValue(ingredient.Key, out int selectedAmount) ||
                selectedAmount != ingredient.Value)
            {
                return false;
            }
        }

        return true;
    }

    private void OnValidate()
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
        resultAmount = Mathf.Max(1, resultAmount);

        for (int i = 0; i < ingredients.Count; i++)
        {
            if (ingredients[i] != null)
            {
                ingredients[i].amount = Mathf.Max(1, ingredients[i].amount);
            }
        }
    }
}
