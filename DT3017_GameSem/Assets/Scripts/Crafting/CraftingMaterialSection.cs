using System.Collections.Generic;
using UnityEngine;

public class CraftingMaterialSection : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField] private GameObject modelPrefab;
    [Tooltip("One model is displayed at each point, up to the available quantity.")]
    [SerializeField] private List<Transform> storageSpawnPoints = new List<Transform>();

    private readonly List<GameObject> generatedModels = new List<GameObject>();
    private CraftingStation station;

    public ItemData Item => item;
    public GameObject ModelPrefab => modelPrefab;

    public void Initialize(CraftingStation owningStation)
    {
        station = owningStation;

        for (int i = 0; i < storageSpawnPoints.Count; i++)
        {
            Transform spawnPoint = storageSpawnPoints[i];
            if (spawnPoint == null || modelPrefab == null)
            {
                generatedModels.Add(null);
                continue;
            }

            GameObject model = Instantiate(
                modelPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                spawnPoint);

            CraftingItemView itemView = model.GetComponent<CraftingItemView>();
            if (itemView == null)
            {
                itemView = model.AddComponent<CraftingItemView>();
            }

            itemView.Configure(station, item, false, true);
            model.SetActive(false);
            generatedModels.Add(model);
        }
    }

    public void Refresh(int visibleQuantity)
    {
        int clampedQuantity = Mathf.Clamp(visibleQuantity, 0, generatedModels.Count);

        for (int i = 0; i < generatedModels.Count; i++)
        {
            if (generatedModels[i] != null)
            {
                generatedModels[i].SetActive(i < clampedQuantity);
            }
        }
    }
}
