using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float normalSpeed = 5f;
    [SerializeField] private float stealthSpeedMultiplier = 0.66f;
    [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float collisionSkin = 0.05f;

    [Header("Noise")]
    [SerializeField] private NoiseEmitter noiseEmitter;
    [SerializeField] private float normalNoiseRadius = 5f;
    [SerializeField] private float stealthNoiseMultiplier = 0.5f;

    private bool isStealth;
    private Vector3 moveInput;
    private Rigidbody rb;
    private Collider bodyCollider;

    private const float MinMoveDistance = 0.0001f;

    private void Awake()
    {
        if (collisionMask.value == 0)
        {
            collisionMask = Physics.DefaultRaycastLayers;
        }

        collisionSkin = Mathf.Max(0.01f, collisionSkin);

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        bodyCollider = GetComponent<Collider>();
        if (bodyCollider == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            bodyCollider = capsule;
        }

        if (noiseEmitter == null)
        {
            noiseEmitter = GetComponent<NoiseEmitter>();
        }
    }

    private void Update()
    {
        HandleStealthInput();
        ReadMovementInput();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleStealthInput()
    {
        isStealth = Input.GetKey(KeyCode.LeftShift);
    }

    private void ReadMovementInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        moveInput = new Vector3(h, 0f, v);
    }

    private void HandleMovement()
    {
        float currentSpeed = isStealth
            ? normalSpeed * stealthSpeedMultiplier
            : normalSpeed;

        Vector3 movement = moveInput.normalized * currentSpeed * Time.fixedDeltaTime;
        MoveWithCollision(movement);

        HandleNoise(moveInput);
    }

    private void HandleNoise(Vector3 move)
    {
        if (move.sqrMagnitude <= MinMoveDistance || noiseEmitter == null) return;

        float currentNoise = isStealth
            ? normalNoiseRadius * stealthNoiseMultiplier
            : normalNoiseRadius;

        noiseEmitter.EmitNoise(currentNoise);
    }

    private void MoveWithCollision(Vector3 movement)
    {
        movement.y = 0f;
        if (movement.sqrMagnitude <= MinMoveDistance) return;

        Vector3 startPosition = rb.position;
        Vector3 finalMove = movement;

        if (TryGetBlockingHit(movement.normalized, movement.magnitude, out RaycastHit hit))
        {
            float allowedDistance = Mathf.Max(0f, hit.distance - collisionSkin);
            Vector3 allowedMove = movement.normalized * allowedDistance;
            Vector3 remainingMove = movement - allowedMove;
            Vector3 slideMove = Vector3.ProjectOnPlane(remainingMove, hit.normal);
            slideMove.y = 0f;

            finalMove = allowedMove;
            if (slideMove.sqrMagnitude > MinMoveDistance &&
                !TryGetBlockingHit(slideMove.normalized, slideMove.magnitude, out _))
            {
                finalMove += slideMove;
            }
        }

        rb.MovePosition(startPosition + finalMove);
    }

    private bool TryGetBlockingHit(Vector3 direction, float distance, out RaycastHit closestHit)
    {
        closestHit = default;
        if (distance <= 0f || bodyCollider == null) return false;

        RaycastHit[] hits = GetShapeCastHits(direction, distance + collisionSkin);
        float closestDistance = float.MaxValue;
        bool foundHit = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (!IsBlockingHit(hit)) continue;
            if (hit.distance >= closestDistance) continue;

            closestDistance = hit.distance;
            closestHit = hit;
            foundHit = true;
        }

        return foundHit;
    }

    private RaycastHit[] GetShapeCastHits(Vector3 direction, float distance)
    {
        if (bodyCollider is CapsuleCollider capsule)
        {
            GetCapsuleWorldPoints(capsule, out Vector3 pointA, out Vector3 pointB, out float radius);
            return Physics.CapsuleCastAll(pointA, pointB, radius, direction, distance, collisionMask, QueryTriggerInteraction.Ignore);
        }

        if (bodyCollider is BoxCollider box)
        {
            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size, Abs(box.transform.lossyScale)) * 0.5f;
            return Physics.BoxCastAll(center, halfExtents, direction, box.transform.rotation, distance, collisionMask, QueryTriggerInteraction.Ignore);
        }

        if (bodyCollider is SphereCollider sphere)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            float radius = sphere.radius * MaxAbsComponent(sphere.transform.lossyScale);
            return Physics.SphereCastAll(center, radius, direction, distance, collisionMask, QueryTriggerInteraction.Ignore);
        }

        return Physics.RaycastAll(transform.position, direction, distance, collisionMask, QueryTriggerInteraction.Ignore);
    }

    private bool IsBlockingHit(RaycastHit hit)
    {
        if (hit.collider == null || hit.collider == bodyCollider || hit.collider.isTrigger) return false;
        if (CollisionFilters.IsSelf(hit.collider, transform, rb)) return false;
        if (CollisionFilters.IsEnemy(hit.collider)) return false;

        return true;
    }

    private static void GetCapsuleWorldPoints(CapsuleCollider capsule, out Vector3 pointA, out Vector3 pointB, out float radius)
    {
        Transform capsuleTransform = capsule.transform;
        Vector3 center = capsuleTransform.TransformPoint(capsule.center);
        Vector3 scale = Abs(capsuleTransform.lossyScale);

        Vector3 axis;
        float heightScale;
        float radiusScale;

        if (capsule.direction == 0)
        {
            axis = capsuleTransform.right;
            heightScale = scale.x;
            radiusScale = Mathf.Max(scale.y, scale.z);
        }
        else if (capsule.direction == 2)
        {
            axis = capsuleTransform.forward;
            heightScale = scale.z;
            radiusScale = Mathf.Max(scale.x, scale.y);
        }
        else
        {
            axis = capsuleTransform.up;
            heightScale = scale.y;
            radiusScale = Mathf.Max(scale.x, scale.z);
        }

        radius = capsule.radius * radiusScale;
        float height = Mathf.Max(capsule.height * heightScale, radius * 2f);
        float pointOffset = Mathf.Max(0f, height * 0.5f - radius);

        pointA = center + axis * pointOffset;
        pointB = center - axis * pointOffset;
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private static float MaxAbsComponent(Vector3 value)
    {
        value = Abs(value);
        return Mathf.Max(value.x, Mathf.Max(value.y, value.z));
    }
}
