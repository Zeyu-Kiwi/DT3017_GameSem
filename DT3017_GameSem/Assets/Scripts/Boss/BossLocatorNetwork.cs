using System.Collections.Generic;
using UnityEngine;

public class BossLocatorNetwork : MonoBehaviour
{
    [Header("Automatic Connection Test")]
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField, Min(0.01f)] private float bossRadius = 0.35f;
    [SerializeField, Min(0f)] private float testHeight = 1f;

    [Header("Runtime Locators (Debug)")]
    [SerializeField] private List<BossPatrolLocator> locators =
        new List<BossPatrolLocator>();

    public IReadOnlyList<BossPatrolLocator> Locators => locators;

    public void BuildConnections()
    {
        BossPatrolLocator[] found = GetComponentsInChildren<BossPatrolLocator>(true);
        locators.Clear();

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && !locators.Contains(found[i]))
            {
                locators.Add(found[i]);
                found[i].ClearConnections();
            }
        }

        for (int first = 0; first < locators.Count; first++)
        {
            for (int second = first + 1; second < locators.Count; second++)
            {
                if (HasClearRoute(locators[first], locators[second]))
                {
                    locators[first].AddConnection(locators[second]);
                    locators[second].AddConnection(locators[first]);
                }
            }
        }
    }

    public BossPatrolLocator GetRandomRoamingNeighbour(
        BossPatrolLocator current,
        BossPatrolLocator previous)
    {
        if (current == null)
        {
            return null;
        }

        List<BossPatrolLocator> candidates = new List<BossPatrolLocator>();
        IReadOnlyList<BossPatrolLocator> neighbours = current.ConnectedLocators;

        for (int i = 0; i < neighbours.Count; i++)
        {
            BossPatrolLocator neighbour = neighbours[i];
            if (neighbour != null &&
                neighbour != previous &&
                neighbour.CanBeRoamingTarget)
            {
                candidates.Add(neighbour);
            }
        }

        return candidates.Count == 0
            ? null
            : candidates[Random.Range(0, candidates.Count)];
    }

    public List<BossPatrolLocator> FindPath(
        BossPatrolLocator start,
        BossPatrolLocator goal)
    {
        List<BossPatrolLocator> emptyPath = new List<BossPatrolLocator>();
        if (start == null || goal == null)
        {
            return emptyPath;
        }

        Queue<BossPatrolLocator> frontier = new Queue<BossPatrolLocator>();
        Dictionary<BossPatrolLocator, BossPatrolLocator> cameFrom =
            new Dictionary<BossPatrolLocator, BossPatrolLocator>();

        frontier.Enqueue(start);
        cameFrom.Add(start, null);

        while (frontier.Count > 0)
        {
            BossPatrolLocator current = frontier.Dequeue();
            if (current == goal)
            {
                break;
            }

            IReadOnlyList<BossPatrolLocator> neighbours = current.ConnectedLocators;
            for (int i = 0; i < neighbours.Count; i++)
            {
                BossPatrolLocator neighbour = neighbours[i];
                if (neighbour == null || cameFrom.ContainsKey(neighbour))
                {
                    continue;
                }

                cameFrom.Add(neighbour, current);
                frontier.Enqueue(neighbour);
            }
        }

        if (!cameFrom.ContainsKey(goal))
        {
            return emptyPath;
        }

        List<BossPatrolLocator> path = new List<BossPatrolLocator>();
        BossPatrolLocator step = goal;

        while (step != null)
        {
            path.Add(step);
            step = cameFrom[step];
        }

        path.Reverse();
        return path;
    }

    private bool HasClearRoute(BossPatrolLocator first, BossPatrolLocator second)
    {
        Vector3 start = first.transform.position + Vector3.up * testHeight;
        Vector3 end = second.transform.position + Vector3.up * testHeight;
        Vector3 offset = end - start;
        float distance = offset.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return false;
        }

        return !Physics.SphereCast(
            start,
            bossRadius,
            offset / distance,
            out _,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Collide);
    }

    [ContextMenu("Rebuild Connections")]
    private void RebuildConnectionsFromMenu()
    {
        BuildConnections();
    }
}
