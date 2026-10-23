using System.Collections.Generic;
using DialogueEditor;
using UnityEngine;

public class DialogueVariableBridge : MonoBehaviour
{
    public static DialogueVariableBridge Instance { get; private set; }

    [SerializeField] private bool warnAboutUnmatchedParameters = true;
    [SerializeField] private DayCycleManager dayCycleManager;
    [SerializeField] private string currentDayParameterName = "CurrentDay";

    private readonly HashSet<string> activeInventoryIntParameters = new HashSet<string>();
    private readonly HashSet<string> activeGameIntParameters = new HashSet<string>();
    private readonly HashSet<string> activeBoolParameters = new HashSet<string>();
    private bool conversationIsActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one DialogueVariableBridge exists in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        if (dayCycleManager == null)
            dayCycleManager = FindFirstObjectByType<DayCycleManager>();
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.QuantityChanged += OnQuantityChanged;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FlagChanged += OnFlagChanged;
            GameStateManager.Instance.IntChanged += OnIntChanged;
        }
    }

    private void OnEnable()
    {
        ConversationManager.OnConversationEnded += OnConversationEnded;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationEnded -= OnConversationEnded;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.QuantityChanged -= OnQuantityChanged;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FlagChanged -= OnFlagChanged;
            GameStateManager.Instance.IntChanged -= OnIntChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartConversation(NPCConversation npcConversation)
    {
        if (npcConversation == null)
        {
            Debug.LogWarning("Cannot start a null NPC conversation.", this);
            return;
        }

        if (ConversationManager.Instance == null)
        {
            Debug.LogError("No ConversationManager exists in the scene.", this);
            return;
        }

        activeInventoryIntParameters.Clear();
        activeGameIntParameters.Clear();
        activeBoolParameters.Clear();

        // StartConversation deserializes the asset and creates its runtime parameters.
        ConversationManager.Instance.StartConversation(npcConversation);
        conversationIsActive = true;

        SynchronizeParameters(npcConversation);
    }

    private void SynchronizeParameters(NPCConversation npcConversation)
    {
        if (npcConversation.ParameterList == null)
        {
            return;
        }

        for (int i = 0; i < npcConversation.ParameterList.Count; i++)
        {
            EditableParameter parameter = npcConversation.ParameterList[i];
            string parameterName = parameter.ParameterName;
            bool matched = false;

            if (parameter is EditableIntParameter)
            {
                if (parameterName == currentDayParameterName &&
                    dayCycleManager != null)
                {
                    ConversationManager.Instance.SetInt(
                        parameterName,
                        dayCycleManager.CurrentDay);
                    matched = true;
                }
                else if (InventoryManager.Instance != null &&
                         InventoryManager.Instance.TryGetItemById(
                             parameterName, out ItemData item))
                {
                    ConversationManager.Instance.SetInt(
                        parameterName,
                        InventoryManager.Instance.GetQuantity(item));
                    activeInventoryIntParameters.Add(parameterName);
                    matched = true;
                }
                else if (GameStateManager.Instance != null &&
                         GameStateManager.Instance.TryGetIntById(
                             parameterName, out GameIntData variable))
                {
                    ConversationManager.Instance.SetInt(
                        parameterName,
                        GameStateManager.Instance.GetInt(variable));
                    activeGameIntParameters.Add(parameterName);
                    matched = true;
                }
            }
            else if (parameter is EditableBoolParameter && GameStateManager.Instance != null)
            {
                if (GameStateManager.Instance.TryGetFlagById(parameterName, out GameFlagData flag))
                {
                    ConversationManager.Instance.SetBool(parameterName, GameStateManager.Instance.GetFlag(flag));
                    activeBoolParameters.Add(parameterName);
                    matched = true;
                }
            }

            if (!matched && warnAboutUnmatchedParameters)
            {
                Debug.LogWarning(
                    "Dialogue parameter '" + parameterName +
                    "' does not match CurrentDay, a registered item, game integer, or game flag. " +
                    "It will remain local to this conversation.",
                    npcConversation);
            }
        }
    }

    private void OnQuantityChanged(ItemData item, int newQuantity)
    {
        if (conversationIsActive &&
            activeInventoryIntParameters.Contains(item.ItemId))
        {
            ConversationManager.Instance.SetInt(item.ItemId, newQuantity);
        }
    }

    private void OnIntChanged(GameIntData variable, int newValue)
    {
        if (conversationIsActive &&
            activeGameIntParameters.Contains(variable.VariableId))
        {
            ConversationManager.Instance.SetInt(variable.VariableId, newValue);
        }
    }

    private void OnFlagChanged(GameFlagData flag, bool newValue)
    {
        if (conversationIsActive && activeBoolParameters.Contains(flag.FlagId))
        {
            ConversationManager.Instance.SetBool(flag.FlagId, newValue);
        }
    }

    private void OnConversationEnded()
    {
        conversationIsActive = false;
        activeInventoryIntParameters.Clear();
        activeGameIntParameters.Clear();
        activeBoolParameters.Clear();
    }
}
