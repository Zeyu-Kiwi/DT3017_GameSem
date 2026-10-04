using UnityEditor;
using UnityEngine;

public static class ItemShineSettingsMenu
{
    [MenuItem("Tools/Item Effects/Global Settings")]
    public static void Open()
    {
        var settings = ItemShineSettings.Global;
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }
}