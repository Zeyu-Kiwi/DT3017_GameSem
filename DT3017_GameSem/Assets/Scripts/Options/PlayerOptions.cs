using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameplayAction { Forward, Backward, Left, Right, Sprint, Jump, Interact }

/// <summary>Local player preferences shared by menu controls and gameplay input.</summary>
public static class PlayerOptions
{
    public const string Prefix = "Factory.Options.v1.";
    public static event Action Changed;
    private static readonly string[] keyPaths = Array.ConvertAll((GameplayAction[])Enum.GetValues(typeof(GameplayAction)), a => Prefix + "Key." + a);
    private static readonly HashSet<int> validKeys = new HashSet<int>(Array.ConvertAll((KeyCode[])Enum.GetValues(typeof(KeyCode)), k => (int)k));
    private static readonly GameplayAction[] movementActions = { GameplayAction.Forward, GameplayAction.Backward, GameplayAction.Left, GameplayAction.Right };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Changed = null; }
    public static float FieldOfView(float fallback = 60f) => ReadFloat("Fov", fallback, 50, 110);
    public static float Sensitivity(float fallback = 3f) => ReadFloat("Sensitivity", fallback, .1f, 10);
    public static bool InvertMouse => PlayerPrefs.GetInt(Prefix + "InvertMouse", 0) == 1;
    private static float ReadFloat(string key, float fallback, float min, float max)
    {
        float value = PlayerPrefs.GetFloat(Prefix + key, fallback);
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
    public static void SetFieldOfView(float value) { PlayerPrefs.SetFloat(Prefix + "Fov", Mathf.Clamp(value, 50, 110)); Changed?.Invoke(); }
    public static void SetSensitivity(float value) { PlayerPrefs.SetFloat(Prefix + "Sensitivity", Mathf.Clamp(value, .1f, 10)); Changed?.Invoke(); }
    public static void SetInvertMouse(bool value) { PlayerPrefs.SetInt(Prefix + "InvertMouse", value ? 1 : 0); Changed?.Invoke(); }
    public static KeyCode DefaultKey(GameplayAction action)
    {
        switch(action)
        {
            case GameplayAction.Forward: return KeyCode.W;
            case GameplayAction.Backward: return KeyCode.S;
            case GameplayAction.Left: return KeyCode.A;
            case GameplayAction.Right: return KeyCode.D;
            case GameplayAction.Sprint: return KeyCode.LeftShift;
            // The current character prefab has jumping unassigned.
            case GameplayAction.Jump: return KeyCode.None;
            default: return KeyCode.F;
        }
    }
    public static KeyCode GetKey(GameplayAction action) => GetKey(action, DefaultKey(action));
    public static KeyCode GetKey(GameplayAction action, KeyCode fallback)
    {
        int saved = PlayerPrefs.GetInt(keyPaths[(int)action], (int)fallback);
        return validKeys.Contains(saved) ? (KeyCode)saved : fallback;
    }
    public static bool IsBindable(KeyCode key) => key != KeyCode.None && key != KeyCode.Escape &&
        key != KeyCode.BackQuote && key != KeyCode.Quote && (int)key < (int)KeyCode.Mouse0;
    public static bool TryBind(GameplayAction action, KeyCode key, out string message)
    {
        if (!IsBindable(key)) { message = "Choose a keyboard key. Escape and the developer menu keys are reserved."; return false; }
        foreach(GameplayAction other in Enum.GetValues(typeof(GameplayAction)))
            if (other != action && GetKey(other) == key)
            { message = KeyName(key) + " is already assigned to " + ActionName(other) + "."; return false; }
        PlayerPrefs.SetInt(keyPaths[(int)action], (int)key);
        Save(); Changed?.Invoke(); message = ""; return true;
    }
    public static Vector2 ReadMovement()
    {
        bool custom = false;
        foreach (var action in movementActions)
            custom |= PlayerPrefs.HasKey(keyPaths[(int)action]);
        if (!custom) return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        float x = (Input.GetKey(GetKey(GameplayAction.Right)) ? 1 : 0) - (Input.GetKey(GetKey(GameplayAction.Left)) ? 1 : 0);
        float y = (Input.GetKey(GetKey(GameplayAction.Forward)) ? 1 : 0) - (Input.GetKey(GetKey(GameplayAction.Backward)) ? 1 : 0);
        return new Vector2(x,y);
    }
    public static string ActionName(GameplayAction action)
    {
        switch(action) { case GameplayAction.Forward: return "Move forward"; case GameplayAction.Backward: return "Move backward";
            case GameplayAction.Left: return "Move left"; case GameplayAction.Right: return "Move right"; default: return action.ToString(); }
    }
    public static string KeyName(KeyCode key)
    {
        switch(key) { case KeyCode.None: return "Unassigned"; case KeyCode.LeftShift: return "Left Shift";
            case KeyCode.RightShift: return "Right Shift"; case KeyCode.LeftControl: return "Left Ctrl";
            case KeyCode.RightControl: return "Right Ctrl"; case KeyCode.Space: return "Space"; default: return key.ToString(); }
    }
    public static void Save() { PlayerPrefs.Save(); }
    public static void RestoreDefaults()
    {
        foreach(var key in new[]{"Fov","Sensitivity","InvertMouse"})PlayerPrefs.DeleteKey(Prefix + key);
        foreach(GameplayAction action in Enum.GetValues(typeof(GameplayAction)))PlayerPrefs.DeleteKey(keyPaths[(int)action]);
        Save();Changed?.Invoke();
    }
}