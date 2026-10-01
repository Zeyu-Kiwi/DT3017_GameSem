using Game.Visuals;
using UnityEditor;
using UnityEngine;

internal static class PSXEffectsMenu
{
    private const string MenuPath = "Tools/PSX Effects/Enabled";

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += ApplySettings;
        Undo.undoRedoPerformed -= ApplySettings;
        Undo.undoRedoPerformed += ApplySettings;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            ApplySettings();
    }

    private static void ApplySettings()
    {
        var settings = Resources.Load<PSXEffectsSettings>("PSXEffectsSettings");
        PSXEffects.SetEnabled(settings == null || settings.EffectsEnabled);
        SceneView.RepaintAll();
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        if (EditorApplication.isPlaying)
        {
            PSXEffects.Toggle();
        }
        else
        {
            var settings = Resources.Load<PSXEffectsSettings>("PSXEffectsSettings");
            if (settings == null)
            {
                Debug.LogError("Missing Resources/PSXEffectsSettings asset.");
                return;
            }

            Undo.RecordObject(settings, "Toggle PSX Effects");
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("effectsEnabled").boolValue = !settings.EffectsEnabled;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            ApplySettings();
        }
        SceneView.RepaintAll();
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateToggle()
    {
        Menu.SetChecked(MenuPath, PSXEffects.Enabled);
        return true;
    }
}
