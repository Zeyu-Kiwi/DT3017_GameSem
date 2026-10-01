using UnityEngine;

namespace Game.Visuals
{
    [CreateAssetMenu(menuName = "Rendering/PSX Effects Settings")]
    public sealed class PSXEffectsSettings : ScriptableObject
    {
        [Tooltip("Enable all PSX visual effects globally. Individual effect settings are preserved.")]
        [SerializeField] private bool effectsEnabled = true;

        public bool EffectsEnabled => effectsEnabled;

        private void OnValidate()
        {
            PSXEffects.SetEnabled(effectsEnabled);
        }
    }

    /// <summary>One switch shared by PSX material shaders and full-screen effects.</summary>
    public static class PSXEffects
    {
        public const string ShaderProperty = "_PSXEffectsDisabled";
        private static readonly int DisabledId = Shader.PropertyToID(ShaderProperty);

        public static bool Enabled => Shader.GetGlobalFloat(DisabledId) < 0.5f;

        public static void SetEnabled(bool enabled)
        {
            // Zero preserves the original look even before the settings asset loads.
            Shader.SetGlobalFloat(DisabledId, enabled ? 0f : 1f);
        }

        public static void Toggle() => SetEnabled(!Enabled);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var settings = Resources.Load<PSXEffectsSettings>("PSXEffectsSettings");
            SetEnabled(settings == null || settings.EffectsEnabled);
        }
    }
}
