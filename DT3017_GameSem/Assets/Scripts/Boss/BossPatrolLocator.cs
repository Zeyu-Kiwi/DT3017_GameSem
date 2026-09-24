using System.Collections.Generic;
using UnityEngine;

public class BossPatrolLocator : MonoBehaviour
{
    [Tooltip("Disable this for entrance/exit nodes that should only be used for routing.")]
    [SerializeField] private bool canBeRoamingTarget = true;

    [Header("Runtime Connections (Debug)")]
    [SerializeField] private List<BossPatrolLocator> connectedLocators =
        new List<BossPatrolLocator>();

    public bool CanBeRoamingTarget => canBeRoamingTarget;
    public IReadOnlyList<BossPatrolLocator> ConnectedLocators => connectedLocators;

    internal void ClearConnections()
    {
        connectedLocators.Clear();
    }

    internal void AddConnection(BossPatrolLocator locator)
    {
        if (locator != null && locator != this && !connectedLocators.Contains(locator))
        {
            connectedLocators.Add(locator);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = canBeRoamingTarget ? Color.cyan : Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.12f);

        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        for (int i = 0; i < connectedLocators.Count; i++)
        {
            if (connectedLocators[i] != null)
            {
                Gizmos.DrawLine(transform.position, connectedLocators[i].transform.position);
            }
        }
    }
}
