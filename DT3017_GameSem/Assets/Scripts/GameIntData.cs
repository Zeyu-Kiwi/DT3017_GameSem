using UnityEngine;

[CreateAssetMenu(fileName = "New Game Integer", menuName = "Game/State/Game Integer")]
public class GameIntData : ScriptableObject
{
    [Tooltip("Stable name used by code and Dialogue Editor parameters. Avoid renaming it after use.")]
    [SerializeField] private string variableId = "new_integer";

    [SerializeField] private string displayName = "New Integer";
    [SerializeField] private int startingValue;

    [Header("Optional Limits")]
    [SerializeField] private bool useMinimum = true;
    [SerializeField] private int minimumValue;
    [SerializeField] private bool useMaximum;
    [SerializeField] private int maximumValue = 100;

    public string VariableId => variableId;
    public string DisplayName => displayName;
    public int StartingValue => Clamp(startingValue);

    public int Clamp(long value)
    {
        long result = value;
        if (useMinimum) result = System.Math.Max(result, minimumValue);
        if (useMaximum) result = System.Math.Min(result, maximumValue);
        return (int)System.Math.Max(int.MinValue, System.Math.Min(int.MaxValue, result));
    }

    private void OnValidate()
    {
        variableId = variableId == null ? string.Empty : variableId.Trim();
        if (useMinimum && useMaximum && maximumValue < minimumValue)
            maximumValue = minimumValue;
        startingValue = Clamp(startingValue);
    }
}
