using UnityEngine;

/// <summary>
/// Runtime connection between ToolSelectionUI and an object that accepts tools.
/// Implementations decide what happens for correct, wrong, and missing tools.
/// </summary>
public interface IToolSelectionTarget
{
    bool KeepSelectionOpenAfterWrongTool { get; }

    bool IsCorrectTool(ItemData selectedTool);

    void HandleCorrectTool(GameObject player);

    void HandleWrongTool();

    void HandleNoToolsAvailable();
}
