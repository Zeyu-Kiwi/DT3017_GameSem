using System;
using TMPro;
using UnityEngine;

/// <summary>Main-menu achievement browser. Reads the same permanent unlocks as the in-game popup.</summary>
public sealed class AchievementMenuController : MonoBehaviour
{
    [Serializable]
    public sealed class Row
    {
        public AchievementId id;
        public TMP_Text title;
        public TMP_Text description;
        public TMP_Text status;
        public UnityEngine.UI.RawImage icon;
        public GameObject lockIcon;
    }
    [SerializeField] private AchievementCatalog catalog;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject[] mainMenuObjects;
    [SerializeField] private Row[] rows;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private UnityEngine.UI.Button openButton;
    [SerializeField] private UnityEngine.UI.Button backButton;
    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake() { panel.SetActive(false); }
    private void OnEnable() { AchievementManager.Unlocked += OnUnlocked; }
    private void OnDisable() { AchievementManager.Unlocked -= OnUnlocked; }
    private void Update() { if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close(); }
    private void OnUnlocked(AchievementDefinition definition) { if (IsOpen) Refresh(); }
    public void Open()
    {
        Refresh();
        foreach (var item in mainMenuObjects) if (item != null) item.SetActive(false);
        panel.SetActive(true);
        backButton.Select();
    }
    public void Close()
    {
        panel.SetActive(false);
        foreach (var item in mainMenuObjects) if (item != null) item.SetActive(true);
        openButton.Select();
    }
    public void Refresh()
    {
        int unlockedCount = 0;
        foreach (var row in rows)
        {
            var achievement = catalog.Find(row.id);
            if (achievement == null) continue;
            bool unlocked = AchievementManager.IsUnlocked(row.id);
            if (unlocked) unlockedCount++;
            row.title.text = achievement.title;
            row.title.color = unlocked ? new Color(.894f,.467f,.243f) : new Color(.55f,.55f,.55f);
            row.description.text = achievement.description;
            row.description.gameObject.SetActive(unlocked);
            row.status.gameObject.SetActive(unlocked);
            if (row.lockIcon != null) row.lockIcon.SetActive(!unlocked);
            row.status.text = unlocked ? "UNLOCKED" : "LOCKED";
            row.icon.texture = achievement.icon;
            row.icon.color = unlocked ? Color.white : new Color(.48f,.48f,.48f,1);
            row.status.color = unlocked ? new Color(.894f,.467f,.243f) : new Color(.65f,.65f,.65f);
        }
        progressText.text = unlockedCount + " / " + rows.Length + " UNLOCKED";
    }
}