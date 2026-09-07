using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Tooltip("Add every global true/false flag used by the game.")]
    [SerializeField] private List<GameFlagData> flags = new List<GameFlagData>();

    public event Action<GameFlagData, bool> FlagChanged;

    private readonly Dictionary<GameFlagData, bool> values = new Dictionary<GameFlagData, bool>();
    private readonly Dictionary<string, GameFlagData> flagsById = new Dictionary<string, GameFlagData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one GameStateManager exists in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        InitializeFlags();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeFlags()
    {
        values.Clear();
        flagsById.Clear();

        for (int i = 0; i < flags.Count; i++)
        {
            GameFlagData flag = flags[i];

            if (flag == null)
            {
                Debug.LogWarning("GameStateManager contains an empty flag entry.", this);
                continue;
            }

            string id = flag.FlagId;
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("A game flag has an empty ID.", flag);
                continue;
            }

            if (flagsById.ContainsKey(id))
            {
                Debug.LogError("Duplicate game flag ID: " + id, flag);
                continue;
            }

            flagsById.Add(id, flag);
            values.Add(flag, flag.StartingValue);
        }
    }

    public bool GetFlag(GameFlagData flag)
    {
        if (flag != null && values.TryGetValue(flag, out bool value))
        {
            return value;
        }

        return false;
    }

    public void SetFlag(GameFlagData flag, bool value)
    {
        if (flag == null || !values.ContainsKey(flag))
        {
            Debug.LogWarning("Tried to change a flag that is not registered in GameStateManager.", flag);
            return;
        }

        if (values[flag] == value)
        {
            return;
        }

        values[flag] = value;
        FlagChanged?.Invoke(flag, value);
    }

    public bool TryGetFlagById(string flagId, out GameFlagData flag)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            flag = null;
            return false;
        }

        return flagsById.TryGetValue(flagId, out flag);
    }
}
