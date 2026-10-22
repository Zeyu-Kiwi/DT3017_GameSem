using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Tooltip("Add every global true/false flag used by the game.")]
    [SerializeField] private List<GameFlagData> flags = new List<GameFlagData>();

    [Tooltip("Add every global non-inventory integer used by the game.")]
    [SerializeField] private List<GameIntData> integers = new List<GameIntData>();

    public event Action<GameFlagData, bool> FlagChanged;
    public event Action<GameIntData, int> IntChanged;

    private readonly Dictionary<GameFlagData, bool> values = new Dictionary<GameFlagData, bool>();
    private readonly Dictionary<string, GameFlagData> flagsById = new Dictionary<string, GameFlagData>();
    private readonly Dictionary<GameIntData, int> intValues =
        new Dictionary<GameIntData, int>();
    private readonly Dictionary<string, GameIntData> integersById =
        new Dictionary<string, GameIntData>();

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
        InitializeIntegers();
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

    private void InitializeIntegers()
    {
        intValues.Clear();
        integersById.Clear();

        for (int i = 0; i < integers.Count; i++)
        {
            GameIntData variable = integers[i];
            if (variable == null)
            {
                Debug.LogWarning("GameStateManager contains an empty integer entry.", this);
                continue;
            }

            string id = variable.VariableId;
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("A game integer has an empty ID.", variable);
                continue;
            }

            if (integersById.ContainsKey(id))
            {
                Debug.LogError("Duplicate game integer ID: " + id, variable);
                continue;
            }

            if (flagsById.ContainsKey(id))
            {
                Debug.LogError(
                    "Game integer ID '" + id + "' is already used by a Boolean flag.",
                    variable);
                continue;
            }

            integersById.Add(id, variable);
            intValues.Add(variable, variable.StartingValue);
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

    public int GetInt(GameIntData variable)
    {
        return variable != null && intValues.TryGetValue(variable, out int value)
            ? value
            : 0;
    }

    public void SetInt(GameIntData variable, int value)
    {
        if (variable == null || !intValues.ContainsKey(variable))
        {
            Debug.LogWarning(
                "Tried to change an integer that is not registered in GameStateManager.",
                variable);
            return;
        }

        int clamped = variable.Clamp(value);
        if (intValues[variable] == clamped) return;
        intValues[variable] = clamped;
        IntChanged?.Invoke(variable, clamped);
    }

    public void AddInt(GameIntData variable, int amount)
    {
        if (!TryGetRegisteredInt(variable, out int current)) return;
        SetIntFromLong(variable, (long)current + Mathf.Max(0, amount));
    }

    public void SubtractInt(GameIntData variable, int amount)
    {
        if (!TryGetRegisteredInt(variable, out int current)) return;
        SetIntFromLong(variable, (long)current - Mathf.Max(0, amount));
    }

    public bool TryGetIntById(string variableId, out GameIntData variable)
    {
        if (string.IsNullOrEmpty(variableId))
        {
            variable = null;
            return false;
        }

        return integersById.TryGetValue(variableId, out variable);
    }

    private bool TryGetRegisteredInt(GameIntData variable, out int current)
    {
        if (variable != null && intValues.TryGetValue(variable, out current))
            return true;

        current = 0;
        Debug.LogWarning(
            "Tried to change an integer that is not registered in GameStateManager.",
            variable);
        return false;
    }

    private void SetIntFromLong(GameIntData variable, long value)
    {
        int clamped = variable.Clamp(value);
        if (intValues[variable] == clamped) return;
        intValues[variable] = clamped;
        IntChanged?.Invoke(variable, clamped);
    }
}
