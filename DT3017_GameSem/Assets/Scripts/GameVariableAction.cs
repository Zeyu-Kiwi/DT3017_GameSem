using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameVariableAction : MonoBehaviour
{
    public enum IntegerOperation
    {
        Set,
        Add,
        Subtract
    }

    [Serializable]
    private sealed class BooleanChange
    {
        public GameFlagData variable = null;
        public bool value = true;
    }

    [Serializable]
    private sealed class IntegerChange
    {
        public GameIntData variable = null;
        public IntegerOperation operation = IntegerOperation.Add;
        [Tooltip("Set uses this exact value. Add and Subtract use a non-negative amount.")]
        public int value = 1;
    }

    [Header("Trigger")]
    [Tooltip("Run these changes immediately when PlayerInteractor successfully interacts with this GameObject.")]
    [SerializeField] private bool runAutomaticallyOnInteraction;

    [Header("Boolean Changes")]
    [SerializeField] private List<BooleanChange> booleanChanges =
        new List<BooleanChange>();

    [Header("Integer Changes")]
    [SerializeField] private List<IntegerChange> integerChanges =
        new List<IntegerChange>();

    public void Execute()
    {
        GameStateManager state = GameStateManager.Instance;
        if (state == null)
        {
            Debug.LogError("GameVariableAction needs a GameStateManager in the scene.", this);
            return;
        }

        for (int i = 0; i < booleanChanges.Count; i++)
        {
            BooleanChange change = booleanChanges[i];
            if (change != null && change.variable != null)
                state.SetFlag(change.variable, change.value);
        }

        for (int i = 0; i < integerChanges.Count; i++)
        {
            IntegerChange change = integerChanges[i];
            if (change == null || change.variable == null) continue;

            switch (change.operation)
            {
                case IntegerOperation.Set:
                    state.SetInt(change.variable, change.value);
                    break;
                case IntegerOperation.Add:
                    state.AddInt(change.variable, Mathf.Max(0, change.value));
                    break;
                case IntegerOperation.Subtract:
                    state.SubtractInt(change.variable, Mathf.Max(0, change.value));
                    break;
            }
        }
    }

    public void ExecuteFromInteraction()
    {
        if (runAutomaticallyOnInteraction) Execute();
    }
}
