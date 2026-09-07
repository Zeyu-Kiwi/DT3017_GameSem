using UnityEngine;

public class SetGameFlagAction : MonoBehaviour
{
    [SerializeField] private GameFlagData flag;
    [SerializeField] private bool value = true;

    public void Execute()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError("No GameStateManager exists in the scene.", this);
            return;
        }

        GameStateManager.Instance.SetFlag(flag, value);
    }
}
