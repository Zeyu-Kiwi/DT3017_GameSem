using System;
using UnityEngine;

/// <summary>Local, permanent achievements shared across new games and scene changes.</summary>
public sealed class AchievementManager : MonoBehaviour
{
    private const string KeyPrefix = "Factory.Achievements.v1.";
    private static AchievementManager instance;
    private AchievementCatalog catalog;
    public static event Action<AchievementDefinition> Unlocked;
    public static AchievementCatalog Catalog => EnsureInstance().catalog;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; Unlocked = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() { EnsureInstance(); }

    private static AchievementManager EnsureInstance()
    {
        if (instance != null) return instance;
        var root = new GameObject("AchievementSystem");
        instance = root.AddComponent<AchievementManager>();
        DontDestroyOnLoad(root);
        instance.catalog = Resources.Load<AchievementCatalog>("AchievementCatalog");
        var toast = Resources.Load<GameObject>("AchievementToast");
        if (toast != null) Instantiate(toast, root.transform);
        return instance;
    }

    public static bool IsUnlocked(AchievementId id) => PlayerPrefs.GetInt(KeyPrefix + (int)id, 0) == 1;

    /// <returns>True only for a newly earned achievement.</returns>
    public static bool Unlock(AchievementId id)
    {
        var manager = EnsureInstance();
        var definition = manager.catalog == null ? null : manager.catalog.Find(id);
        if (definition == null || IsUnlocked(id)) return false;
        PlayerPrefs.SetInt(KeyPrefix + (int)id, 1);
        PlayerPrefs.Save();
        Unlocked?.Invoke(definition);
        return true;
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}