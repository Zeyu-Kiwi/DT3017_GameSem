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

    [Header("New Recipe Badge")]
    [Tooltip("Assign the single Image GameObject used as the new-recipe badge.")]
    [SerializeField] private GameObject newRecipeBadge;
    [Tooltip("Maximum scale multiplier reached at the middle of each pulse.")]
    [SerializeField, Min(1f)] private float badgeScaleSize = 1.2f;
    [Tooltip("Complete grow-and-shrink pulses per second before the interval pause.")]
    [SerializeField, Min(0.01f)] private float badgeScaleSpeed = 2f;
    [Tooltip("Seconds to wait at normal scale between pulses.")]
    [SerializeField, Min(0f)] private float badgeScaleInterval = 0.5f;

    private readonly List<Image> generatedIngredientIcons = new List<Image>();
    private Vector3 badgeBaseScale = Vector3.one;
    private float badgeAnimationTime;
    private bool badgeVisible;

    private void Awake()
    {
        if (newRecipeBadge != null)
        {
            badgeBaseScale = newRecipeBadge.transform.localScale;
            newRecipeBadge.SetActive(false);
        }
    }

    private void Update()
    {
        if (!badgeVisible || newRecipeBadge == null)
        {
            return;
        }

        badgeAnimationTime += Time.deltaTime;

        float pulseDuration = 1f / Mathf.Max(0.01f, badgeScaleSpeed);
        float cycleDuration = pulseDuration + Mathf.Max(0f, badgeScaleInterval);
        float cycleTime = Mathf.Repeat(badgeAnimationTime, cycleDuration);
        float scaleMultiplier = 1f;

        if (cycleTime < pulseDuration)
        {
            float normalizedTime = cycleTime / pulseDuration;
            float pulse = Mathf.Sin(normalizedTime * Mathf.PI);
            scaleMultiplier = Mathf.Lerp(
                1f,
                Mathf.Max(1f, badgeScaleSize),
                pulse);
        }

        newRecipeBadge.transform.localScale =
            badgeBaseScale * scaleMultiplier;
    }

    public void Setup(
        CraftingRecipeData recipe,
        IReadOnlyDictionary<ItemData, int> selection,
        bool unlocked,
        bool showNewRecipeBadge)
    {
        ClearIngredientIcons();
        SetNewRecipeBadgeVisible(showNewRecipeBadge);

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

    private void SetNewRecipeBadgeVisible(bool visible)
    {
        badgeVisible = visible && newRecipeBadge != null;
        badgeAnimationTime = 0f;

        if (newRecipeBadge == null)
        {
            return;
        }

        newRecipeBadge.transform.localScale = badgeBaseScale;
        newRecipeBadge.SetActive(badgeVisible);
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

    private void OnDisable()
    {
        if (newRecipeBadge != null)
        {
            newRecipeBadge.transform.localScale = badgeBaseScale;
        }
    }

    private void OnValidate()
    {
        badgeScaleSize = Mathf.Max(1f, badgeScaleSize);
        badgeScaleSpeed = Mathf.Max(0.01f, badgeScaleSpeed);
        badgeScaleInterval = Mathf.Max(0f, badgeScaleInterval);
    }
}
