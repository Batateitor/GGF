using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class AStarPathfinder
{
    [System.Serializable]
    public struct PathSettings
    {
        [Min(0.25f)] public float gridCellSize;
        [Min(0.05f)] public float agentClearance;
        [Min(0.05f)] public float castHeight;
        [Min(1f)] public float searchPadding;
        [Min(0.1f)] public float navMeshSampleRadius;
        [Min(0.01f)] public float goalTolerance;
        [Min(0.001f)] public float minSegmentLength;

        public static PathSettings Default => new PathSettings
        {
            gridCellSize = 1.25f,
            agentClearance = 0.35f,
            castHeight = 0.7f,
            searchPadding = 8f,
            navMeshSampleRadius = 2.5f,
            goalTolerance = 0.15f,
            minSegmentLength = 0.01f
        };

        public PathSettings Validated()
        {
            PathSettings fallback = Default;
            PathSettings settings = this;

            if (settings.gridCellSize <= 0f) settings.gridCellSize = fallback.gridCellSize;
            if (settings.agentClearance <= 0f) settings.agentClearance = fallback.agentClearance;
            if (settings.castHeight <= 0f) settings.castHeight = fallback.castHeight;
            if (settings.searchPadding <= 0f) settings.searchPadding = fallback.searchPadding;
            if (settings.navMeshSampleRadius <= 0f) settings.navMeshSampleRadius = fallback.navMeshSampleRadius;
            if (settings.goalTolerance <= 0f) settings.goalTolerance = fallback.goalTolerance;
            if (settings.minSegmentLength <= 0f) settings.minSegmentLength = fallback.minSegmentLength;

            return settings;
        }
    }

    private sealed class SearchRecord
    {
        public PathNode Node;
        public SearchRecord CameFrom;
        public float CostSoFar;
        public float EstimatedTotalCost;
    }

    private sealed class GridRecord
    {
        public Vector2Int Cell;
        public GridRecord CameFrom;
        public float CostSoFar;
        public float EstimatedTotalCost;
    }

    public static List<Vector3> FindPath(Vector3 start, Vector3 goal, LayerMask obstacleMask)
    {
        return FindPath(start, goal, obstacleMask, PathSettings.Default);
    }

    public static List<Vector3> FindPath(Vector3 start, Vector3 goal, LayerMask obstacleMask, PathSettings settings)
    {
        settings = settings.Validated();
        obstacleMask = NormalizeObstacleMask(obstacleMask);

        if (!PathBlockedBetween(start, goal, obstacleMask, settings))
        {
            return new List<Vector3> { goal };
        }

        if (TryFindNavMeshPath(start, goal, settings, out List<Vector3> navMeshPath))
        {
            return navMeshPath;
        }

        PathNode[] nodes = Object.FindObjectsByType<PathNode>(FindObjectsInactive.Exclude);
        List<Vector3> nodePath = FindPath(start, goal, nodes, obstacleMask, settings);
        if (nodePath.Count > 1 || !PathBlockedBetween(start, nodePath[0], obstacleMask, settings))
        {
            return nodePath;
        }

        return FindGridPath(start, goal, obstacleMask, settings);
    }

    public static List<Vector3> FindPath(Vector3 start, Vector3 goal, IReadOnlyList<PathNode> nodes, LayerMask obstacleMask)
    {
        return FindPath(start, goal, nodes, obstacleMask, PathSettings.Default);
    }

    public static List<Vector3> FindPath(Vector3 start, Vector3 goal, IReadOnlyList<PathNode> nodes, LayerMask obstacleMask, PathSettings settings)
    {
        settings = settings.Validated();
        obstacleMask = NormalizeObstacleMask(obstacleMask);
        List<Vector3> directPath = new List<Vector3> { goal };

        if (nodes == null || nodes.Count == 0)
        {
            return directPath;
        }

        PathNode startNode = FindNearestNode(start, nodes, obstacleMask, settings);
        PathNode goalNode = FindNearestNode(goal, nodes, obstacleMask, settings);

        if (startNode == null || goalNode == null)
        {
            return directPath;
        }

        List<PathNode> nodePath = Search(startNode, goalNode, nodes, obstacleMask, settings);
        if (nodePath.Count == 0)
        {
            return directPath;
        }

        List<Vector3> path = new List<Vector3>();
        for (int i = 0; i < nodePath.Count; i++)
        {
            path.Add(nodePath[i].Position);
        }

        path.Add(goal);
        return path;
    }

    private static List<PathNode> Search(PathNode startNode, PathNode goalNode, IReadOnlyList<PathNode> allNodes, LayerMask obstacleMask, PathSettings settings)
    {
        List<SearchRecord> open = new List<SearchRecord>();
        HashSet<PathNode> closed = new HashSet<PathNode>();

        open.Add(new SearchRecord
        {
            Node = startNode,
            CostSoFar = 0f,
            EstimatedTotalCost = Heuristic(startNode, goalNode)
        });

        while (open.Count > 0)
        {
            SearchRecord current = TakeBest(open);
            if (current.Node == goalNode)
            {
                return BuildNodePath(current);
            }

            closed.Add(current.Node);

            foreach (PathNode neighbor in GetNeighbors(current.Node, allNodes, obstacleMask, settings))
            {
                if (neighbor == null || closed.Contains(neighbor))
                {
                    continue;
                }

                float newCost = current.CostSoFar + Vector3.Distance(current.Node.Position, neighbor.Position);
                SearchRecord existing = open.Find(record => record.Node == neighbor);

                if (existing == null)
                {
                    open.Add(new SearchRecord
                    {
                        Node = neighbor,
                        CameFrom = current,
                        CostSoFar = newCost,
                        EstimatedTotalCost = newCost + Heuristic(neighbor, goalNode)
                    });
                }
                else if (newCost < existing.CostSoFar)
                {
                    existing.CameFrom = current;
                    existing.CostSoFar = newCost;
                    existing.EstimatedTotalCost = newCost + Heuristic(neighbor, goalNode);
                }
            }
        }

        return new List<PathNode>();
    }

    private static SearchRecord TakeBest(List<SearchRecord> open)
    {
        int bestIndex = 0;
        for (int i = 1; i < open.Count; i++)
        {
            if (open[i].EstimatedTotalCost < open[bestIndex].EstimatedTotalCost)
            {
                bestIndex = i;
            }
        }

        SearchRecord best = open[bestIndex];
        open.RemoveAt(bestIndex);
        return best;
    }

    private static List<PathNode> BuildNodePath(SearchRecord endRecord)
    {
        List<PathNode> path = new List<PathNode>();
        SearchRecord current = endRecord;

        while (current != null)
        {
            path.Add(current.Node);
            current = current.CameFrom;
        }

        path.Reverse();
        return path;
    }

    private static IEnumerable<PathNode> GetNeighbors(PathNode node, IReadOnlyList<PathNode> allNodes, LayerMask obstacleMask, PathSettings settings)
    {
        if (node.neighbors != null && node.neighbors.Count > 0)
        {
            return node.neighbors;
        }

        if (!node.autoConnectWhenEmpty)
        {
            return new List<PathNode>();
        }

        List<PathNode> autoNeighbors = new List<PathNode>();
        for (int i = 0; i < allNodes.Count; i++)
        {
            PathNode candidate = allNodes[i];
            if (candidate == null || candidate == node)
            {
                continue;
            }

            float distance = Vector3.Distance(node.Position, candidate.Position);
            if (distance <= node.autoConnectDistance && !PathBlockedBetween(node.Position, candidate.Position, obstacleMask, settings))
            {
                autoNeighbors.Add(candidate);
            }
        }

        return autoNeighbors;
    }

    private static PathNode FindNearestNode(Vector3 point, IReadOnlyList<PathNode> nodes, LayerMask obstacleMask, PathSettings settings)
    {
        PathNode best = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < nodes.Count; i++)
        {
            PathNode node = nodes[i];
            if (node == null)
            {
                continue;
            }

            float distance = Vector3.Distance(point, node.Position);
            float blockedPenalty = PathBlockedBetween(point, node.Position, obstacleMask, settings) ? 1000f : 0f;
            float score = distance + blockedPenalty;

            if (score < bestScore)
            {
                best = node;
                bestScore = score;
            }
        }

        return best;
    }

    private static float Heuristic(PathNode from, PathNode to)
    {
        return Vector3.Distance(from.Position, to.Position);
    }

    private static bool TryFindNavMeshPath(Vector3 start, Vector3 goal, PathSettings settings, out List<Vector3> path)
    {
        path = new List<Vector3>();

        if (!NavMesh.SamplePosition(start, out NavMeshHit startHit, settings.navMeshSampleRadius, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(goal, out NavMeshHit goalHit, settings.navMeshSampleRadius, NavMesh.AllAreas))
        {
            return false;
        }

        NavMeshPath navMeshPath = new NavMeshPath();
        if (!NavMesh.CalculatePath(startHit.position, goalHit.position, NavMesh.AllAreas, navMeshPath))
        {
            return false;
        }

        if (navMeshPath.status == NavMeshPathStatus.PathInvalid || navMeshPath.corners == null || navMeshPath.corners.Length == 0)
        {
            return false;
        }

        for (int i = 1; i < navMeshPath.corners.Length; i++)
        {
            path.Add(navMeshPath.corners[i]);
        }

        if (path.Count == 0 || Vector3.Distance(path[path.Count - 1], goalHit.position) > settings.goalTolerance)
        {
            path.Add(goalHit.position);
        }

        return path.Count > 0;
    }

    private static List<Vector3> FindGridPath(Vector3 start, Vector3 goal, LayerMask obstacleMask, PathSettings settings)
    {
        Vector2Int startCell = Vector2Int.zero;
        Vector2Int goalCell = WorldToCell(goal, start, settings.gridCellSize);

        int padding = Mathf.CeilToInt(settings.searchPadding / settings.gridCellSize);
        int minX = Mathf.Min(startCell.x, goalCell.x) - padding;
        int maxX = Mathf.Max(startCell.x, goalCell.x) + padding;
        int minY = Mathf.Min(startCell.y, goalCell.y) - padding;
        int maxY = Mathf.Max(startCell.y, goalCell.y) + padding;

        List<GridRecord> open = new List<GridRecord>
        {
            new GridRecord
            {
                Cell = startCell,
                CostSoFar = 0f,
                EstimatedTotalCost = GridHeuristic(startCell, goalCell)
            }
        };

        HashSet<Vector2Int> closed = new HashSet<Vector2Int>();

        while (open.Count > 0)
        {
            GridRecord current = TakeBest(open);
            if (current.Cell == goalCell)
            {
                return SmoothPath(BuildGridPath(current, start, goal, settings), obstacleMask, settings);
            }

            closed.Add(current.Cell);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    Vector2Int neighbor = current.Cell + new Vector2Int(x, y);
                    if (neighbor.x < minX || neighbor.x > maxX || neighbor.y < minY || neighbor.y > maxY || closed.Contains(neighbor))
                    {
                        continue;
                    }

                    Vector3 currentWorld = CellToWorld(current.Cell, start, settings.gridCellSize);
                    Vector3 neighborWorld = CellToWorld(neighbor, start, settings.gridCellSize);
                    if (CellBlocked(neighborWorld, obstacleMask, settings) || PathBlockedBetween(currentWorld, neighborWorld, obstacleMask, settings))
                    {
                        continue;
                    }

                    float moveCost = x != 0 && y != 0 ? Mathf.Sqrt(2f) : 1f;
                    float newCost = current.CostSoFar + moveCost;
                    GridRecord existing = open.Find(record => record.Cell == neighbor);

                    if (existing == null)
                    {
                        open.Add(new GridRecord
                        {
                            Cell = neighbor,
                            CameFrom = current,
                            CostSoFar = newCost,
                            EstimatedTotalCost = newCost + GridHeuristic(neighbor, goalCell)
                        });
                    }
                    else if (newCost < existing.CostSoFar)
                    {
                        existing.CameFrom = current;
                        existing.CostSoFar = newCost;
                        existing.EstimatedTotalCost = newCost + GridHeuristic(neighbor, goalCell);
                    }
                }
            }
        }

        return new List<Vector3> { goal };
    }

    private static GridRecord TakeBest(List<GridRecord> open)
    {
        int bestIndex = 0;
        for (int i = 1; i < open.Count; i++)
        {
            if (open[i].EstimatedTotalCost < open[bestIndex].EstimatedTotalCost)
            {
                bestIndex = i;
            }
        }

        GridRecord best = open[bestIndex];
        open.RemoveAt(bestIndex);
        return best;
    }

    private static List<Vector3> BuildGridPath(GridRecord endRecord, Vector3 origin, Vector3 goal, PathSettings settings)
    {
        List<Vector3> path = new List<Vector3>();
        GridRecord current = endRecord;

        while (current != null)
        {
            path.Add(CellToWorld(current.Cell, origin, settings.gridCellSize));
            current = current.CameFrom;
        }

        path.Reverse();
        if (path.Count > 0)
        {
            path.RemoveAt(0);
        }

        path.Add(goal);
        return path;
    }

    private static List<Vector3> SmoothPath(List<Vector3> path, LayerMask obstacleMask, PathSettings settings)
    {
        if (path.Count <= 2)
        {
            return path;
        }

        List<Vector3> smoothed = new List<Vector3>();
        int index = 0;

        while (index < path.Count)
        {
            int next = path.Count - 1;
            while (next > index + 1 && PathBlockedBetween(path[index], path[next], obstacleMask, settings))
            {
                next--;
            }

            smoothed.Add(path[next]);
            if (next >= path.Count - 1)
            {
                break;
            }

            index = next;
        }

        return smoothed;
    }

    private static Vector2Int WorldToCell(Vector3 world, Vector3 origin, float gridCellSize)
    {
        Vector3 offset = world - origin;
        return new Vector2Int(Mathf.RoundToInt(offset.x / gridCellSize), Mathf.RoundToInt(offset.z / gridCellSize));
    }

    private static Vector3 CellToWorld(Vector2Int cell, Vector3 origin, float gridCellSize)
    {
        return new Vector3(origin.x + cell.x * gridCellSize, origin.y, origin.z + cell.y * gridCellSize);
    }

    private static float GridHeuristic(Vector2Int from, Vector2Int to)
    {
        return Vector2Int.Distance(from, to);
    }

    private static bool CellBlocked(Vector3 point, LayerMask obstacleMask, PathSettings settings)
    {
        Collider[] hits = Physics.OverlapSphere(point + Vector3.up * settings.castHeight, settings.agentClearance, obstacleMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsBlockingCollider(hits[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PathBlockedBetween(Vector3 from, Vector3 to, LayerMask obstacleMask, PathSettings settings)
    {
        Vector3 a = from + Vector3.up * settings.castHeight;
        Vector3 b = to + Vector3.up * settings.castHeight;
        Vector3 direction = b - a;
        float distance = direction.magnitude;

        if (distance <= settings.minSegmentLength)
        {
            return false;
        }

        RaycastHit[] hits = Physics.SphereCastAll(a, settings.agentClearance, direction.normalized, distance, obstacleMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsBlockingCollider(hits[i].collider))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBlockingCollider(Collider hit)
    {
        return CollisionFilters.BlocksNavigation(hit);
    }

    private static LayerMask NormalizeObstacleMask(LayerMask obstacleMask)
    {
        return obstacleMask.value != 0 ? obstacleMask : CollisionFilters.DefaultObstacleMask();
    }
}
