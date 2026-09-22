using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CraftingWorkstationRange : MonoBehaviour
{
    [SerializeField] private CraftingStation craftingStation;

    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

    public bool IsPlayerInside => playerColliders.Count > 0;

    private void Awake()
    {
        Collider rangeCollider = GetComponent<Collider>();
        if (!rangeCollider.isTrigger)
        {
            Debug.LogWarning(
                "CraftingWorkstationRange Collider must have Is Trigger enabled.",
                this);
        }

        if (craftingStation == null)
        {
            craftingStation = FindFirstObjectByType<CraftingStation>();
        }

        if (craftingStation == null)
        {
            Debug.LogError("CraftingWorkstationRange has no CraftingStation assigned.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerColliders.Add(other);
        ReportState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (playerColliders.Remove(other))
        {
            ReportState();
        }
    }

    private void OnDisable()
    {
        playerColliders.Clear();
        ReportState();
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag("Player"))
        {
            return true;
        }

        Transform root = other.transform.root;
        return root != null && root.CompareTag("Player");
    }

    private void ReportState()
    {
        if (craftingStation != null)
        {
            craftingStation.SetPlayerInsideWorkstation(IsPlayerInside);
        }
    }
}
