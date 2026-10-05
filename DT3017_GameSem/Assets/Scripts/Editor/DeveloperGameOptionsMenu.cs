using UnityEditor;
using UnityEngine;

public static class DeveloperGameOptionsMenu
{
    private const string AssetPath = "Assets/Resources/DeveloperGameOptions.asset";
    private const string ToggleMenu = "Tools/Developer Game Options/Boss Vision Target Triggers Detection";

    [MenuItem("Tools/Developer Game Options/Open Options")]
    public static void OpenOptions()
    {
        Selection.activeObject = GetOrCreateOptions();
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    [MenuItem(ToggleMenu)]
    private static void TogglePlayerDetection()
    {
        DeveloperGameOptions options = GetOrCreateOptions();
        SerializedObject serialized = new SerializedObject(options);
        SerializedProperty toggle = serialized.FindProperty("bossVisionTargetTriggersDetection");
        toggle.boolValue = !toggle.boolValue;
        serialized.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(options);
    }

    [MenuItem(ToggleMenu, true)]
    private static bool ValidatePlayerDetection()
    {
        Menu.SetChecked(ToggleMenu, DeveloperGameOptions.PlayerVisionDetectionEnabled);
        return true;
    }

    private static DeveloperGameOptions GetOrCreateOptions()
    {
        DeveloperGameOptions options = Resources.Load<DeveloperGameOptions>(DeveloperGameOptions.ResourcePath);
        if (options != null)
        {
            return options;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        options = ScriptableObject.CreateInstance<DeveloperGameOptions>();
        AssetDatabase.CreateAsset(options, AssetPath);
        AssetDatabase.SaveAssets();
        return options;
    }
}
