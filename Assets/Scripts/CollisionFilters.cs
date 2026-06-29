using UnityEngine;

public static class CollisionFilters
{
    public static LayerMask DefaultObstacleMask()
    {
        int mask = LayerMask.GetMask("Default", "Obstacle");
        return mask != 0 ? mask : Physics.DefaultRaycastLayers;
    }

    public static bool IsSelf(Collider hitCollider, Transform owner, Rigidbody ownerRigidbody = null)
    {
        if (hitCollider == null || owner == null)
        {
            return false;
        }

        if (hitCollider.transform == owner || hitCollider.transform.IsChildOf(owner))
        {
            return true;
        }

        return ownerRigidbody != null && hitCollider.attachedRigidbody == ownerRigidbody;
    }

    public static bool IsEnemy(Collider hitCollider)
    {
        return hitCollider != null &&
               (hitCollider.GetComponentInParent<AdvancedEnemyAgent>() != null ||
                hitCollider.GetComponentInParent<EnemyController>() != null ||
                hitCollider.GetComponentInParent<EnemyCollision>() != null);
    }

    public static bool IsPlayer(Collider hitCollider)
    {
        return hitCollider != null &&
               (hitCollider.GetComponentInParent<PlayerController>() != null ||
                hitCollider.GetComponentInParent<NoiseEmitter>() != null);
    }

    public static bool BlocksNavigation(Collider hitCollider)
    {
        if (hitCollider == null || hitCollider.isTrigger)
        {
            return false;
        }

        return !IsEnemy(hitCollider) && !IsPlayer(hitCollider);
    }
}
