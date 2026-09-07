using UnityEngine;

[CreateAssetMenu(fileName = "New Game Flag", menuName = "Game/State/Game Flag")]
public class GameFlagData : ScriptableObject
{
    [Tooltip("Stable name used by code and Dialogue Editor parameters. Avoid renaming it after use.")]
    [SerializeField] private string flagId = "new_flag";

    [SerializeField] private string displayName = "New Flag";
    [SerializeField] private bool startingValue;

    public string FlagId => flagId;
    public string DisplayName => displayName;
    public bool StartingValue => startingValue;

    private void OnValidate()
    {
        flagId = flagId == null ? string.Empty : flagId.Trim();
    }
}
