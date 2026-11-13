using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Editor-only clipboard. SessionState keeps copied values across script recompiles.
public static class EnvironmentSettingsClipboard
{
    private const string ClipboardKey = "Game.EnvironmentSettingsClipboard.v1";
    private const string CopyMenu = "Tools/Environment/Copy Environment Settings";
    private const string PasteMenu = "Tools/Environment/Paste Environment Settings";

    [Serializable]
    private sealed class Snapshot
    {
        public string sourceScene;
        public bool fog;
        public Color fogColor;
        public FogMode fogMode;
        public float fogDensity, fogStart, fogEnd;
        public AmbientMode ambientMode;
        public Color ambientSky, ambientEquator, ambientGround, ambientLight;
        public float ambientIntensity;
        public string skyboxGuid, reflectionGuid, skyboxPath, skyboxName, reflectionPath, reflectionName;
        public DefaultReflectionMode reflectionMode;
        public int reflectionResolution, reflectionBounces;
        public float reflectionIntensity;
    }

    private static bool CanEdit()
    {
        var scene = SceneManager.GetActiveScene();
        return !EditorApplication.isPlayingOrWillChangePlaymode && scene.IsValid() && scene.isLoaded
            && PrefabStageUtility.GetCurrentPrefabStage() == null;
    }

    private static string AssetGuid(UnityEngine.Object asset)
    {
        return asset == null ? "" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
    }

    private static T LoadAsset<T>(string guid, string path, string name) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(guid) && string.IsNullOrEmpty(path)) return null;
        string resolvedPath = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(resolvedPath)) resolvedPath = path;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(resolvedPath))
            if (asset is T typed && asset.name == name) return typed;
        return AssetDatabase.LoadAssetAtPath<T>(resolvedPath);
    }

    [MenuItem(CopyMenu, false, 2000)]
    public static void Copy()
    {
        if (!CanEdit()) return;
        var snapshot = new Snapshot
        {
            sourceScene = SceneManager.GetActiveScene().name,
            fog = RenderSettings.fog, fogColor = RenderSettings.fogColor,
            fogMode = RenderSettings.fogMode, fogDensity = RenderSettings.fogDensity,
            fogStart = RenderSettings.fogStartDistance, fogEnd = RenderSettings.fogEndDistance,
            ambientMode = RenderSettings.ambientMode, ambientSky = RenderSettings.ambientSkyColor,
            ambientEquator = RenderSettings.ambientEquatorColor, ambientGround = RenderSettings.ambientGroundColor,
            ambientLight = RenderSettings.ambientLight, ambientIntensity = RenderSettings.ambientIntensity,
            skyboxGuid = AssetGuid(RenderSettings.skybox), reflectionGuid = AssetGuid(RenderSettings.customReflectionTexture),
            skyboxPath = AssetDatabase.GetAssetPath(RenderSettings.skybox), skyboxName = RenderSettings.skybox != null ? RenderSettings.skybox.name : "",
            reflectionPath = AssetDatabase.GetAssetPath(RenderSettings.customReflectionTexture), reflectionName = RenderSettings.customReflectionTexture != null ? RenderSettings.customReflectionTexture.name : "",
            reflectionMode = RenderSettings.defaultReflectionMode, reflectionResolution = RenderSettings.defaultReflectionResolution,
            reflectionBounces = RenderSettings.reflectionBounces, reflectionIntensity = RenderSettings.reflectionIntensity
        };
        SessionState.SetString(ClipboardKey, JsonUtility.ToJson(snapshot));
        Debug.Log("Copied environment settings from " + snapshot.sourceScene + ". Open the destination scene and use Tools > Environment > Paste Environment Settings.");
    }

    [MenuItem(CopyMenu, true)]
    private static bool ValidateCopy() => CanEdit();

    [MenuItem(PasteMenu, false, 2001)]
    public static void Paste()
    {
        if (!ValidatePaste()) return;
        var snapshot = JsonUtility.FromJson<Snapshot>(SessionState.GetString(ClipboardKey, ""));
        var skybox = LoadAsset<Material>(snapshot.skyboxGuid, snapshot.skyboxPath, snapshot.skyboxName);
        var reflection = LoadAsset<Texture>(snapshot.reflectionGuid, snapshot.reflectionPath, snapshot.reflectionName);
        if ((!string.IsNullOrEmpty(snapshot.skyboxGuid) && skybox == null)
            || (!string.IsNullOrEmpty(snapshot.reflectionGuid) && reflection == null))
        {
            Debug.LogError("A copied environment asset is missing. Copy the source environment again before pasting.");
            return;
        }

        // Unity exposes the values publicly, but the scene settings object needed for Undo is internal.
        var getSettings = typeof(RenderSettings).GetMethod("GetRenderSettings", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        var settings = getSettings?.Invoke(null, null) as UnityEngine.Object;
        if (settings == null)
        {
            Debug.LogError("Cannot access this Unity version's scene environment settings for Undo. No changes were made.");
            return;
        }
        Undo.RegisterCompleteObjectUndo(settings, "Paste Environment Settings");
        RenderSettings.fog = snapshot.fog;
        RenderSettings.fogColor = snapshot.fogColor;
        RenderSettings.fogMode = snapshot.fogMode;
        RenderSettings.fogDensity = snapshot.fogDensity;
        RenderSettings.fogStartDistance = snapshot.fogStart;
        RenderSettings.fogEndDistance = snapshot.fogEnd;
        RenderSettings.ambientMode = snapshot.ambientMode;
        RenderSettings.ambientSkyColor = snapshot.ambientSky;
        RenderSettings.ambientEquatorColor = snapshot.ambientEquator;
        RenderSettings.ambientGroundColor = snapshot.ambientGround;
        RenderSettings.ambientLight = snapshot.ambientLight;
        RenderSettings.ambientIntensity = snapshot.ambientIntensity;
        RenderSettings.skybox = skybox;
        RenderSettings.defaultReflectionMode = snapshot.reflectionMode;
        RenderSettings.defaultReflectionResolution = snapshot.reflectionResolution;
        RenderSettings.reflectionBounces = snapshot.reflectionBounces;
        RenderSettings.reflectionIntensity = snapshot.reflectionIntensity;
        RenderSettings.customReflectionTexture = reflection;
        EditorUtility.SetDirty(settings);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        DynamicGI.UpdateEnvironment();
        SceneView.RepaintAll();
        Debug.Log("Pasted environment settings from " + snapshot.sourceScene + " into " + SceneManager.GetActiveScene().name
            + ". Save the scene to keep them. Sun light, Light objects and baked lighting are unchanged.");
    }

    [MenuItem(PasteMenu, true)]
    private static bool ValidatePaste() => CanEdit() && !string.IsNullOrEmpty(SessionState.GetString(ClipboardKey, ""));
}