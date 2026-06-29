using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    [Header("A* Connections")]
    public List<PathNode> neighbors = new List<PathNode>();
    public bool autoConnectWhenEmpty = true;
    public float autoConnectDistance = 7f;

    public Vector3 Position => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.9f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.08f, 0.2f);

        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
        foreach (PathNode neighbor in neighbors)
        {
            if (neighbor != null)
            {
                Gizmos.DrawLine(transform.position + Vector3.up * 0.08f, neighbor.transform.position + Vector3.up * 0.08f);
            }
        }
    }
}
