using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CraftingStation : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private CraftingRecipeDatabase recipeDatabase;
    [SerializeField] private List<CraftingMaterialSection> materialSections =
        new List<CraftingMaterialSection>();
    [SerializeField] private CraftingEquipmentDisplay equipmentDisplay;

    [Header("Centre Display")]
    [SerializeField] private List<Transform> centerSpawnPoints = new List<Transform>();

    [Header("Input")]
    [SerializeField] private LayerMask craftingItemLayers = ~0;
    [SerializeField] private CraftingStationUI stationUI;
    [SerializeField] private CraftingCameraController cameraController;

    private readonly Dictionary<ItemData, int> selection =
        new Dictionary<ItemData, int>();
    private readonly List<ItemData> selectionOrder = new List<ItemData>();
    private readonly List<GameObject> centerModels = new List<GameObject>();
    private readonly HashSet<CraftingRecipeData> discoveredThisSession =
        new HashSet<CraftingRecipeData>();

    private FirstPersonController playerController;
    private PlayerInteractor playerInteractor;
    private PlayerInteractUI playerInteractUI;
    private Camera playerCamera;
    private CraftingItemView hoveredItem;
    private bool isOpen;
    private bool inputReady;
    private bool inventorySubscribed;
    private bool flagsSubscribed;

    public bool CanInteract => canInteract && !isOpen;
    public IReadOnlyDictionary<ItemData, int> Selection => selection;

    private void Awake()
    {
        if (stationUI != null)
        {
            stationUI.Initialize(this);
        }

        for (int i = 0; i < materialSections.Count; i++)
        {
            if (materialSections[i] != null)
            {
                materialSections[i].Initialize(this);
            }
        }

        if (equipmentDisplay != null)
        {
            equipmentDisplay.Initialize(this);
        }
    }

    private void Start()
    {
        SubscribeToManagers();
        RefreshDisplays();
        RefreshUI();
    }

    private void OnDestroy()
    {
        UnsubscribeFromManagers();
    }

    private void Update()
    {
        if (!isOpen || !inputReady)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCrafting();
            return;
        }

        UpdateHoveredItem();

        if (hoveredItem == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            hoveredItem.HandleLeftClick();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            hoveredItem.HandleRightClick();
        }
    }

    public void Interact(GameObject player)
    {
        if (!CanInteract || player == null)
        {
            return;
        }

        if (InventoryManager.Instance == null)
        {
            ShowMessage("No InventoryManager exists in the scene.");
            return;
        }

        SubscribeToManagers();

        playerController = player.GetComponent<FirstPersonController>();
        playerInteractor = player.GetComponent<PlayerInteractor>();
        playerInteractUI = player.GetComponent<PlayerInteractUI>();
        playerCamera = player.GetComponentInChildren<Camera>(true);

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        isOpen = true;
        inputReady = false;
        SetHoveredItem(null);
        ClearSelection();

        if (playerInteractUI != null)
        {
            playerInteractUI.Hide();
        }

        if (playerInteractor != null)
        {
            playerInteractor.enabled = false;
        }

        if (playerController != null)
        {
            playerController.DisableController();
            playerController.enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        RefreshDisplays();

        if (stationUI != null)
        {
            stationUI.SetOpen(false);
        }

        if (cameraController != null)
        {
            cameraController.Open(playerCamera, FinishOpening);
        }
        else
        {
            FinishOpening();
        }
    }

    public void CloseCrafting()
    {
        if (!isOpen || !inputReady)
        {
            return;
        }

        inputReady = false;
        SetHoveredItem(null);
        ClearSelection();

        if (stationUI != null)
        {
            stationUI.SetOpen(false);
        }

        if (cameraController != null)
        {
            cameraController.Close(FinishClosing);
        }
        else
        {
            FinishClosing();
        }
    }

    public void TryAddIngredient(ItemData item)
    {
        if (!inputReady || item == null || InventoryManager.Instance == null)
        {
            return;
        }

        if (selectionOrder.Count >= centerSpawnPoints.Count)
        {
            ShowMessage("The crafting area is full.");
            return;
        }

        int selectedAmount = GetSelectedAmount(item);
        if (selectedAmount >= InventoryManager.Instance.GetQuantity(item))
        {
            return;
        }

        if (!selection.ContainsKey(item))
        {
            selection.Add(item, 0);
        }

        selection[item]++;
        selectionOrder.Add(item);
        RefreshDisplays();
        RefreshUI();
    }

    public void TryRemoveIngredient(ItemData item)
    {
        if (!inputReady || item == null || !selection.ContainsKey(item))
        {
            return;
        }

        selection[item]--;
        if (selection[item] <= 0)
        {
            selection.Remove(item);
        }

        for (int i = selectionOrder.Count - 1; i >= 0; i--)
        {
            if (selectionOrder[i] == item)
            {
                selectionOrder.RemoveAt(i);
                break;
            }
        }

        RefreshDisplays();
        RefreshUI();
    }

    public void HandleGearPressed()
    {
        if (!inputReady || selection.Count == 0)
        {
            return;
        }

        CraftingRecipeData exactRecipe = FindExactRecipe();
        if (exactRecipe != null)
        {
            Craft(exactRecipe);
        }
        else if (stationUI != null)
        {
            stationUI.ToggleRecipePanel(GetCandidateRecipes(), selection);
        }
    }

    public bool IsRecipeUnlocked(CraftingRecipeData recipe)
    {
        if (recipe == null)
        {
            return false;
        }

        if (recipe.StartsUnlocked || discoveredThisSession.Contains(recipe))
        {
            return true;
        }

        return recipe.UnlockFlag != null &&
               GameStateManager.Instance != null &&
               GameStateManager.Instance.GetFlag(recipe.UnlockFlag);
    }

    private void FinishOpening()
    {
        if (!isOpen)
        {
            return;
        }

        if (stationUI != null)
        {
            stationUI.SetOpen(true);
        }

        inputReady = true;
        RefreshUI();
    }

    private void FinishClosing()
    {
        isOpen = false;

        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.EnableController();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerInteractor != null)
        {
            playerInteractor.enabled = true;
        }

        playerController = null;
        playerInteractor = null;
        playerInteractUI = null;
        playerCamera = null;
    }

    private void UpdateHoveredItem()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            SetHoveredItem(null);
            return;
        }

        Camera rayCamera = cameraController != null
            ? cameraController.CraftingCamera
            : playerCamera;

        if (rayCamera == null)
        {
            SetHoveredItem(null);
            return;
        }

        Ray ray = rayCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                Mathf.Infinity,
                craftingItemLayers,
                QueryTriggerInteraction.Collide))
        {
            SetHoveredItem(hit.collider.GetComponentInParent<CraftingItemView>());
        }
        else
        {
            SetHoveredItem(null);
        }
    }

    private void SetHoveredItem(CraftingItemView itemView)
    {
        if (hoveredItem == itemView)
        {
            return;
        }

        if (hoveredItem != null)
        {
            hoveredItem.SetHovered(false);
        }

        hoveredItem = itemView;

        if (hoveredItem != null)
        {
            hoveredItem.SetHovered(true);
        }

        if (stationUI != null)
        {
            stationUI.ShowHoveredItem(hoveredItem == null ? null : hoveredItem.Item);
        }
    }

    private void RefreshDisplays()
    {
        InventoryManager inventory = InventoryManager.Instance;

        for (int i = 0; i < materialSections.Count; i++)
        {
            CraftingMaterialSection section = materialSections[i];
            if (section == null || section.Item == null)
            {
                continue;
            }

            int owned = inventory == null ? 0 : inventory.GetQuantity(section.Item);
            section.Refresh(owned - GetSelectedAmount(section.Item));
        }

        if (equipmentDisplay != null)
        {
            equipmentDisplay.Refresh();
        }

        RebuildCenterModels();
    }

    private void RebuildCenterModels()
    {
        SetHoveredItem(null);

        for (int i = 0; i < centerModels.Count; i++)
        {
            if (centerModels[i] != null)
            {
                centerModels[i].SetActive(false);
                Destroy(centerModels[i]);
            }
        }

        centerModels.Clear();

        int count = Mathf.Min(selectionOrder.Count, centerSpawnPoints.Count);
        for (int i = 0; i < count; i++)
        {
            ItemData item = selectionOrder[i];
            CraftingMaterialSection section = FindSection(item);
            Transform spawnPoint = centerSpawnPoints[i];

            if (section == null || section.ModelPrefab == null || spawnPoint == null)
            {
                continue;
            }

            GameObject model = Instantiate(
                section.ModelPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                spawnPoint);

            CraftingItemView view = model.GetComponent<CraftingItemView>();
            if (view == null)
            {
                view = model.AddComponent<CraftingItemView>();
            }

            view.Configure(this, item, true, false);
            centerModels.Add(model);
        }
    }

    private CraftingMaterialSection FindSection(ItemData item)
    {
        for (int i = 0; i < materialSections.Count; i++)
        {
            if (materialSections[i] != null && materialSections[i].Item == item)
            {
                return materialSections[i];
            }
        }

        return null;
    }

    private int GetSelectedAmount(ItemData item)
    {
        return item != null && selection.TryGetValue(item, out int amount) ? amount : 0;
    }

    private List<CraftingRecipeData> GetCandidateRecipes()
    {
        List<CraftingRecipeData> candidates = recipeDatabase == null
            ? new List<CraftingRecipeData>()
            : recipeDatabase.GetOrderedRecipes();

        if (selectionOrder.Count == 0)
        {
            candidates.Clear();
            return candidates;
        }

        Dictionary<ItemData, int> acceptedSelection = new Dictionary<ItemData, int>();

        for (int selectedIndex = 0; selectedIndex < selectionOrder.Count; selectedIndex++)
        {
            ItemData selectedItem = selectionOrder[selectedIndex];
            Dictionary<ItemData, int> proposedSelection =
                new Dictionary<ItemData, int>(acceptedSelection);

            if (!proposedSelection.ContainsKey(selectedItem))
            {
                proposedSelection.Add(selectedItem, 0);
            }

            proposedSelection[selectedItem]++;

            List<CraftingRecipeData> filtered = new List<CraftingRecipeData>();
            for (int recipeIndex = 0; recipeIndex < candidates.Count; recipeIndex++)
            {
                if (candidates[recipeIndex].CanContain(proposedSelection))
                {
                    filtered.Add(candidates[recipeIndex]);
                }
            }

            if (selectedIndex == 0)
            {
                candidates = filtered;
                if (candidates.Count == 0)
                {
                    return candidates;
                }

                acceptedSelection = proposedSelection;
            }
            else if (filtered.Count > 0)
            {
                candidates = filtered;
                acceptedSelection = proposedSelection;
            }
        }

        return candidates;
    }

    private CraftingRecipeData FindExactRecipe()
    {
        if (recipeDatabase == null)
        {
            return null;
        }

        List<CraftingRecipeData> recipes = recipeDatabase.GetOrderedRecipes();
        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i].ExactlyMatches(selection))
            {
                return recipes[i];
            }
        }

        return null;
    }

    private void Craft(CraftingRecipeData recipe)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || recipe == null || recipe.Result == null)
        {
            ShowMessage("This recipe is not configured correctly.");
            return;
        }

        Dictionary<ItemData, int> ingredients = recipe.GetCombinedIngredients();
        foreach (KeyValuePair<ItemData, int> ingredient in ingredients)
        {
            if (!inventory.Has(ingredient.Key, ingredient.Value))
            {
                ShowMessage("Not enough " + ingredient.Key.DisplayName + ".");
                return;
            }
        }

        if (!inventory.TryGetItemById(recipe.Result.ItemId, out ItemData registeredResult) ||
            registeredResult != recipe.Result)
        {
            ShowMessage(recipe.Result.DisplayName + " is not registered in InventoryManager.");
            return;
        }

        int resultCost = ingredients.TryGetValue(recipe.Result, out int amountUsed)
            ? amountUsed
            : 0;
        int projectedResultQuantity =
            inventory.GetQuantity(recipe.Result) - resultCost + recipe.ResultAmount;

        if (projectedResultQuantity > recipe.Result.StackLimit)
        {
            ShowMessage(recipe.Result.DisplayName + " is full.");
            return;
        }

        foreach (KeyValuePair<ItemData, int> ingredient in ingredients)
        {
            inventory.TryRemoveItem(ingredient.Key, ingredient.Value);
        }

        int added = inventory.AddItem(recipe.Result, recipe.ResultAmount);
        if (added != recipe.ResultAmount)
        {
            Debug.LogError("Crafting validation succeeded but the result could not be added.", this);
            return;
        }

        discoveredThisSession.Add(recipe);
        if (recipe.UnlockFlag != null && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetFlag(recipe.UnlockFlag, true);
        }

        ShowMessage("Crafted " + recipe.ResultAmount + " " + recipe.Result.DisplayName + ".");
        ClearSelection();
        RefreshDisplays();
        RefreshUI();
    }

    private void ClearSelection()
    {
        selection.Clear();
        selectionOrder.Clear();
        RefreshDisplays();
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (stationUI == null)
        {
            return;
        }

        bool hasSelection = selection.Count > 0;
        bool canCraftSelection = FindExactRecipe() != null;
        stationUI.Refresh(
            hasSelection,
            canCraftSelection,
            GetCandidateRecipes(),
            selection);
    }

    private void SubscribeToManagers()
    {
        if (!inventorySubscribed && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.QuantityChanged += OnQuantityChanged;
            inventorySubscribed = true;
        }

        if (!flagsSubscribed && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FlagChanged += OnFlagChanged;
            flagsSubscribed = true;
        }
    }

    private void UnsubscribeFromManagers()
    {
        if (inventorySubscribed && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.QuantityChanged -= OnQuantityChanged;
        }

        if (flagsSubscribed && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FlagChanged -= OnFlagChanged;
        }

        inventorySubscribed = false;
        flagsSubscribed = false;
    }

    private void OnQuantityChanged(ItemData item, int quantity)
    {
        RefreshDisplays();
        RefreshUI();
    }

    private void OnFlagChanged(GameFlagData flag, bool value)
    {
        RefreshUI();
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
