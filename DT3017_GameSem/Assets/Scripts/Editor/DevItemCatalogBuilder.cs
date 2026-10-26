#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public sealed class DevItemCatalogBuilder : IPreprocessBuildWithReport
{
    private const string CatalogPath = "Assets/Resources/DevItemCatalog.asset";
    public int callbackOrder => 0;
    static DevItemCatalogBuilder() { QueueRebuild(); }
    private static bool rebuildQueued;
    public static void QueueRebuild()
    {
        if (rebuildQueued) return;
        rebuildQueued = true;
        EditorApplication.delayCall += () => { rebuildQueued = false; Rebuild(); };
    }
    public void OnPreprocessBuild(BuildReport report) { Rebuild(); }

    [MenuItem("Tools/Developer/Rebuild Item Catalog")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        var items = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets" })
            .Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(i => i != null).OrderBy(i => i.DisplayName).ThenBy(i => i.ItemId).ToArray();
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab/Inventory" })
            .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).Where(p => p != null).ToArray();
        var sections = Object.FindObjectsByType<CraftingMaterialSection>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var catalog = AssetDatabase.LoadAssetAtPath<DevItemCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<DevItemCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        var so = new SerializedObject(catalog);
        var entries = so.FindProperty("entries");
        entries.arraySize = items.Length;
        string Normalize(string value) => new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        for (int i = 0; i < items.Length; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            var previousItem = entry.FindPropertyRelative("item").objectReferenceValue;
            var previousModel = entry.FindPropertyRelative("worldModel").objectReferenceValue;
            var section = sections.FirstOrDefault(s => s.Item == items[i] && s.ModelPrefab != null);
            GameObject model = section != null ? section.ModelPrefab : prefabs.FirstOrDefault(p =>
                Normalize(p.name).Replace("inventory", "") == Normalize(items[i].ItemId) ||
                Normalize(p.name).Replace("inventory", "").Replace("fbxfolded", "") == Normalize(items[i].name));
            if (model == null && previousItem == items[i]) model = previousModel as GameObject;
            entry.FindPropertyRelative("item").objectReferenceValue = items[i];
            entry.FindPropertyRelative("worldModel").objectReferenceValue = model;
        }
        if (so.ApplyModifiedPropertiesWithoutUndo()) AssetDatabase.SaveAssets();
    }
}

public sealed class DevItemCatalogPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        bool Relevant(string path) => path != "Assets/Resources/DevItemCatalog.asset" &&
            (path.StartsWith("Assets/GameData/Items/") || path.StartsWith("Assets/Prefab/Inventory/") ||
             path.EndsWith(".asset") && AssetDatabase.LoadAssetAtPath<ItemData>(path) != null);
        if (imported.Any(Relevant) || deleted.Any(Relevant) || moved.Any(Relevant) || movedFrom.Any(Relevant))
            DevItemCatalogBuilder.QueueRebuild();
    }
}
#endif