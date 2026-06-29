using UnityEngine;

public class VisionSystem : MonoBehaviour
{
    public float viewDistance = 5f;
    public LayerMask visionMask;
    public float eyeHeight = 0.8f;
    public float targetHeight = 0.6f;

    private void Awake()
    {
        NormalizeConfiguration();
    }

    private void OnValidate()
    {
        NormalizeConfiguration();
    }

    public bool CanSeeTarget(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPoint = target.position + Vector3.up * targetHeight;
        Vector3 direction = targetPoint - origin;
        float distance = direction.magnitude;

        if (distance > viewDistance)
        {
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance, visionMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (CollisionFilters.IsSelf(hitCollider, transform))
            {
                continue;
            }

            if (hitCollider.transform == target || hitCollider.transform.IsChildOf(target))
            {
                return true;
            }

            if (CollisionFilters.BlocksNavigation(hitCollider))
            {
                return false;
            }
        }

        return true;
    }

    private void NormalizeConfiguration()
    {
        if (visionMask.value == 0)
        {
            visionMask = CollisionFilters.DefaultObstacleMask();
        }

        viewDistance = Mathf.Max(0.1f, viewDistance);
        eyeHeight = Mathf.Max(0f, eyeHeight);
        targetHeight = Mathf.Max(0f, targetHeight);
    }
}
