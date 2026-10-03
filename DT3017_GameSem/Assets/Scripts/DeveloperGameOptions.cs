using UnityEngine;

[CreateAssetMenu(fileName = "DeveloperGameOptions", menuName = "Game/Developer Game Options")]
public class DeveloperGameOptions : ScriptableObject
{
    public const string ResourcePath = "DeveloperGameOptions";

    [Header("Boss Detection")]
    [Tooltip("Allow the boss to catch the player by seeing the player's Boss Vision Target. Empty-workstation and open-drawer checks remain active.")]
    [SerializeField] private bool bossVisionTargetTriggersDetection = true;

    private static DeveloperGameOptions current;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static bool? playerDetectionOverride;
    private static bool skipDailyQuota;

    public static void SetSkipDailyQuotaEnabled(bool enabled)
    {
        skipDailyQuota = enabled;
    }

    public static void SetPlayerVisionDetectionEnabled(bool enabled)
    {
        playerDetectionOverride = enabled;
    }
#endif

    public static bool SkipDailyQuotaEnabled
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return skipDailyQuota;
#else
            return false;
#endif
        }
    }

    public bool BossVisionTargetTriggersDetection => bossVisionTargetTriggersDetection;

    // Missing settings preserve the game's normal detection behavior.
    public static bool PlayerVisionDetectionEnabled
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (playerDetectionOverride.HasValue)
            {
                return playerDetectionOverride.Value;
            }
#endif
            if (current == null)
            {
                current = Resources.Load<DeveloperGameOptions>(ResourcePath);
            }

            return current == null || current.BossVisionTargetTriggersDetection;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        current = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        playerDetectionOverride = null;
        skipDailyQuota = false;
#endif
    }
}
