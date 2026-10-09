#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Register session-only developer controls without changing the screen layout.</summary>
public static class DevModeOptions
{
    public sealed class Option
    {
        public string Id { get; internal set; }
        public string Label { get; internal set; }
        public Func<bool> Read { get; internal set; }
        public Action<bool> Write { get; internal set; }
        public Action Execute { get; internal set; }
        public Func<bool> CanExecute { get; internal set; }
    }

    private static readonly List<Option> options = new List<Option>();
    public static IReadOnlyList<Option> Options => options;
    public static event Action Changed;

    public static void RegisterToggle(string id, string label, Func<bool> read, Action<bool> write)
    {
        if (read == null || write == null) throw new ArgumentNullException("Toggle callbacks");
        Register(new Option { Id = id, Label = label, Read = read, Write = write });
    }

    public static void RegisterAction(string id, string label, Action execute, Func<bool> canExecute = null)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));
        Register(new Option { Id = id, Label = label, Execute = execute, CanExecute = canExecute });
    }

    private static void Register(Option option)
    {
        if (string.IsNullOrWhiteSpace(option.Id)) throw new ArgumentException("An option needs a stable ID.");
        int index = options.FindIndex(existing => existing.Id == option.Id);
        if (index >= 0) options[index] = option;
        else options.Add(option);
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        options.Clear();
        Changed = null;
        RegisterToggle("boss-target", "Boss target detection", () => DeveloperGameOptions.PlayerVisionDetectionEnabled, DeveloperGameOptions.SetPlayerVisionDetectionEnabled);
        RegisterToggle("psx-effects", "PSX shader effects", () => Game.Visuals.PSXEffects.Enabled, Game.Visuals.PSXEffects.SetEnabled);
        RegisterToggle("skip-quota", "Sleep without quota", () => DeveloperGameOptions.SkipDailyQuotaEnabled, DeveloperGameOptions.SetSkipDailyQuotaEnabled);
        RegisterAction("skip-day", "Skip day now (ignore quota)", () =>
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<DayCycleManager>();
            if (manager == null || !manager.isActiveAndEnabled || manager.TransitionRunning) return;
            UnityEngine.Object.FindFirstObjectByType<DevModeScreen>()?.SetOpen(false);
            manager.TryBeginNextDay(false);
        }, () =>
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<DayCycleManager>();
            return manager != null && manager.isActiveAndEnabled && !manager.TransitionRunning;
        });
    }
}
#endif
