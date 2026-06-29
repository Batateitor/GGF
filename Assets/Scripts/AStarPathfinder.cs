using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class AStarPathfinder
{
    private const float GridCellSize = 1.25f;
    private const float AgentClearance = 0.35f;
    private const float CastHeight = 0.7f;

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
        if (!PathBlockedBetween(start, goal, obstacleMask))
        {
            return new List<Vector3> { goal };
        }

        if (TryFindNavMeshPath(start, goal, out List<Vector3> navMeshPath))
        {
            return navMeshPath;
        }

        PathNode[] nodes = Object.FindObjectsByType<PathNode>(FindObjectsInactive.Exclude);
        List<Vector3> nodePath = FindPath(start, goal, nodes, obstacleMask);
        if (nodePath.Count > 1 || !PathBlockedBetween(start, nodePath[0], obstacleMask))
        {
            return nodePath;
        }

        return FindGridPath(start, goal, obstacleMask);
    }

    public static List<Vector3> FindPath(Vector3 start, Vector3 goal, IReadOnlyList<PathNode> nodes, LayerMask obstacleMask)
    {
        List<Vector3> directPath = new List<Vector3> { goal };

        if (nodes == null || nodes.Count == 0)
        {
            return directPath;
        }

        PathNode startNode = FindNearestNode(start, nodes, obstacleMask);
        PathNode goalNode = FindNearestNode(goal, nodes, obstacleMask);

        if (startNode == null || goalNode == null)
        {
            return directPath;
        }

        List<PathNode> nodePath = Search(startNode, goalNode, nodes, obstacleMask);
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

    private static List<PathNode> Search(PathNode startNode, PathNode goalNode, IReadOnlyList<PathNode> allNodes, LayerMask obstacleMask)
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

            foreach (PathNode neighbor in GetNeighbors(current.Node, allNodes, obstacleMask))
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

    private static IEnumerable<PathNode> GetNeighbors(PathNode node, IReadOnlyList<PathNode> allNodes, LayerMask obstacleMask)
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
            if (distance <= node.autoConnectDistance && !BlockedBetween(node.Position, candidate.Position, obstacleMask))
            {
                autoNeighbors.Add(candidate);
            }
        }

        return autoNeighbors;
    }

    private static PathNode FindNearestNode(Vector3 point, IReadOnlyList<PathNode> nodes, LayerMask obstacleMask)
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
            float blockedPenalty = BlockedBetween(point, node.Position, obstacleMask) ? 1000f : 0f;
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

    private static bool BlockedBetween(Vector3 from, Vector3 to, LayerMask obstacleMask)
    {
        return PathBlockedBetween(from, to, obstacleMask);
    }

    private static bool TryFindNavMeshPath(Vector3 start, Vector3 goal, out List<Vector3> path)
    {
        path = new List<Vector3>();

        if (!NavMesh.SamplePosition(start, out NavMeshHit startHit, 2.5f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(goal, out NavMeshHit goalHit, 2.5f, NavMesh.AllAreas))
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

        if (path.Count == 0 || Vector3.Distance(path[path.Count - 1], goalHit.position) > 0.15f)
        {
            path.Add(goalHit.position);
        }

        return path.Count > 0;
    }

    private static List<Vector3> FindGridPath(Vector3 start, Vector3 goal, LayerMask obstacleMask)
    {
        Vector2Int startCell = Vector2Int.zero;
        Vector2Int goalCell = WorldToCell(goal, start);

        int padding = Mathf.CeilToInt(8f / GridCellSize);
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
                return SmoothPath(BuildGridPath(current, start, goal), obstacleMask);
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

                    Vector3 currentWorld = CellToWorld(current.Cell, start);
                    Vector3 neighborWorld = CellToWorld(neighbor, start);
                    if (CellBlocked(neighborWorld, obstacleMask) || PathBlockedBetween(currentWorld, neighborWorld, obstacleMask))
                    {
                        continue;
                    }

                    float moveCost = x != 0 && y != 0 ? 1.4142f : 1f;
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

    private static List<Vector3> BuildGridPath(GridRecord endRecord, Vector3 origin, Vector3 goal)
    {
        List<Vector3> path = new List<Vector3>();
        GridRecord current = endRecord;

        while (current != null)
        {
            path.Add(CellToWorld(current.Cell, origin));
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

    private static List<Vector3> SmoothPath(List<Vector3> path, LayerMask obstacleMask)
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
            while (next > index + 1 && PathBlockedBetween(path[index], path[next], obstacleMask))
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

    private static Vector2Int WorldToCell(Vector3 world, Vector3 origin)
    {
        Vector3 offset = world - origin;
        return new Vector2Int(Mathf.RoundToInt(offset.x / GridCellSize), Mathf.RoundToInt(offset.z / GridCellSize));
    }

    private static Vector3 CellToWorld(Vector2Int cell, Vector3 origin)
    {
        return new Vector3(origin.x + cell.x * GridCellSize, origin.y, origin.z + cell.y * GridCellSize);
    }

    private static float GridHeuristic(Vector2Int from, Vector2Int to)
    {
        return Vector2Int.Distance(from, to);
    }

    private static bool CellBlocked(Vector3 point, LayerMask obstacleMask)
    {
        Collider[] hits = Physics.OverlapSphere(point + Vector3.up * CastHeight, AgentClearance, obstacleMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsBlockingCollider(hits[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PathBlockedBetween(Vector3 from, Vector3 to, LayerMask obstacleMask)
    {
        Vector3 a = from + Vector3.up * CastHeight;
        Vector3 b = to + Vector3.up * CastHeight;
        Vector3 direction = b - a;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            return false;
        }

        RaycastHit[] hits = Physics.SphereCastAll(a, AgentClearance, direction.normalized, distance, obstacleMask, QueryTriggerInteraction.Ignore);
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
        if (hit == null || hit.isTrigger)
        {
            return false;
        }

        if (hit.GetComponentInParent<AdvancedEnemyAgent>() != null)
        {
            return false;
        }

        Transform hitTransform = hit.transform;
        if (hitTransform.CompareTag("Player") || hitTransform.GetComponentInParent<NoiseEmitter>() != null)
        {
            return false;
        }

        return true;
    }
}
