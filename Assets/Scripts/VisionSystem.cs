using UnityEngine;

public class VisionSystem : MonoBehaviour
{
    public float viewDistance = 5f;

    public bool CanSeeTarget(Transform target)
    {
        if (Vector3.Distance(transform.position, target.position) > viewDistance)
            return false;

        Vector3 dir = (target.position - transform.position).normalized;

        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, viewDistance))
        {
            return hit.transform == target;
        }

        return false;
    }
}