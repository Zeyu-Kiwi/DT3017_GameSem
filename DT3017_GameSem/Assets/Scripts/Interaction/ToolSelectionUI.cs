using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolSelectionUI : MonoBehaviour
{
    public static ToolSelectionUI Instance { get; private set; }

    [Header("Tools")]
    [Tooltip("Add every tool ItemData once. Only owned tools appear in the UI.")]
    [SerializeField] private List<ItemData> toolItems = new List<ItemData>();

    [Header("UI")]
    [SerializeField] private GameObject interfaceRoot;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private ToolSelectionButtonUI buttonPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<ToolSelectionButtonUI> spawnedButtons =
        new List<ToolSelectionButtonUI>();

    private DoorToolInteractable activeDoor;
    private GameObject activePlayer;
    private FirstPersonController firstPersonController;
    private PlayerInteractor playerInteractor;
    private PlayerInteractUI playerInteractUI;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one ToolSelectionUI exists in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (interfaceRoot == gameObject)
        {
            Debug.LogWarning(
                "ToolSelectionUI Interface Root should be a separate child object.",
                this);
        }

        if (interfaceRoot != null)
        {
            interfaceRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Open(DoorToolInteractable door, GameObject player)
    {
        if (isOpen || door == null || player == null)
        {
            return;
        }

        if (InventoryManager.Instance == null)
        {
            ShowMessage("No InventoryManager exists in the scene.");
            return;
        }

        if (!HasAnyOwnedTool())
        {
            ShowMessage("You do not have any tools.");
            return;
        }

        activeDoor = door;
        activePlayer = player;
        CachePlayerComponents();
        BuildOwnedToolButtons();

        isOpen = true;
        if (interfaceRoot != null)
        {
            interfaceRoot.SetActive(true);
        }

        if (playerInteractUI != null)
        {
            playerInteractUI.Hide();
        }

        if (playerInteractor != null)
        {
            playerInteractor.enabled = false;
        }

        if (firstPersonController != null)
        {
            firstPersonController.DisableController();
            firstPersonController.enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void SelectTool(ToolSelectionButtonUI selectedButton)
    {
        if (!isOpen || activeDoor == null || selectedButton == null)
        {
            return;
        }

        ItemData selectedTool = selectedButton.Item;
        if (selectedTool == null ||
            InventoryManager.Instance == null ||
            !InventoryManager.Instance.Has(selectedTool, 1))
        {
            ShowMessage("You no longer have that tool.");
            BuildOwnedToolButtons();
            return;
        }

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            spawnedButtons[i].SetSelected(spawnedButtons[i] == selectedButton);
        }

        if (activeDoor.IsCorrectTool(selectedTool))
        {
            DoorToolInteractable completedDoor = activeDoor;
            Close();
            completedDoor.InvokeCorrectToolResponse();
        }
        else
        {
            activeDoor.InvokeWrongToolResponse();
        }
    }

    public void Close()
    {
        CloseInternal(true);
    }

    public void CloseForExternalSequence()
    {
        CloseInternal(false);
    }

    private void CloseInternal(bool restorePlayerControl)
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;

        if (interfaceRoot != null)
        {
            interfaceRoot.SetActive(false);
        }

        DestroySpawnedButtons();

        if (restorePlayerControl)
        {
            if (firstPersonController != null)
            {
                firstPersonController.enabled = true;
                firstPersonController.EnableController();
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
        }

        activeDoor = null;
        activePlayer = null;
        firstPersonController = null;
        playerInteractor = null;
        playerInteractUI = null;
    }

    private bool HasAnyOwnedTool()
    {
        for (int i = 0; i < toolItems.Count; i++)
        {
            if (toolItems[i] != null && InventoryManager.Instance.Has(toolItems[i], 1))
            {
                return true;
            }
        }

        return false;
    }

    private void BuildOwnedToolButtons()
    {
        DestroySpawnedButtons();

        if (buttonContainer == null || buttonPrefab == null ||
            InventoryManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < toolItems.Count; i++)
        {
            ItemData item = toolItems[i];
            if (item == null || !InventoryManager.Instance.Has(item, 1))
            {
                continue;
            }

            ToolSelectionButtonUI button = Instantiate(buttonPrefab, buttonContainer);
            button.Initialize(this, item);
            spawnedButtons.Add(button);
        }
    }

    private void DestroySpawnedButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
            {
                Destroy(spawnedButtons[i].gameObject);
            }
        }

        spawnedButtons.Clear();
    }

    private void CachePlayerComponents()
    {
        if (activePlayer == null)
        {
            return;
        }

        firstPersonController = activePlayer.GetComponent<FirstPersonController>();
        playerInteractor = activePlayer.GetComponent<PlayerInteractor>();
        playerInteractUI = activePlayer.GetComponent<PlayerInteractUI>();
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
