using System;
using UnityEngine;

public enum AchievementId
{
    CrowbarEscape = 1,
    ScrewdriverEscape = 2,
    CraftItem = 3,
    CoworkerTrade = 4,
    SickCoworkerDied = 5,
    SurviveFourDays = 6
}

[Serializable]
public sealed class AchievementDefinition
{
    public AchievementId id;
    public string title;
    [TextArea] public string description;
    public Texture2D icon;
}

[CreateAssetMenu(menuName = "Game/Achievement Catalog")]
public sealed class AchievementCatalog : ScriptableObject
{
    public AchievementDefinition[] achievements = Array.Empty<AchievementDefinition>();
    public AchievementDefinition Find(AchievementId id)
    {
        foreach (var entry in achievements)
            if (entry != null && entry.id == id) return entry;
        return null;
    }
}