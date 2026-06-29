using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SteeringAgent : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 4f;
    public float maxForce = 14f;
    public float mass = 1f;
    public float rotationSpeed = 12f;
    public float arriveRadius = 2f;
    public float stopRadius = 0.25f;
    public float predictionTime = 0.5f;

    [Header("Wander")]
    public float wanderRadius = 1.8f;
    public float wanderDistance = 2.5f;
    public float wanderJitter = 35f;

    public Vector3 Velocity => velocity;

    private Rigidbody rb;
    private Vector3 velocity;
    private Vector3 wanderTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        wanderTarget = transform.forward * wanderRadius;
    }

    public void SetSpeed(float speed)
    {
        maxSpeed = Mathf.Max(0f, speed);
    }

    public Vector3 Seek(Vector3 target)
    {
        Vector3 desired = Flatten(target - transform.position).normalized * maxSpeed;
        return desired - velocity;
    }

    public Vector3 Arrive(Vector3 target)
    {
        Vector3 toTarget = Flatten(target - transform.position);
        float distance = toTarget.magnitude;

        if (distance <= stopRadius)
        {
            return -velocity;
        }

        float rampedSpeed = maxSpeed * Mathf.Clamp01(distance / arriveRadius);
        Vector3 desired = toTarget.normalized * rampedSpeed;
        return desired - velocity;
    }

    public Vector3 Flee(Vector3 threat)
    {
        Vector3 desired = Flatten(transform.position - threat).normalized * maxSpeed;
        return desired - velocity;
    }

    public Vector3 Pursue(Transform target, Vector3 targetVelocity)
    {
        Vector3 predictedTarget = target.position + Flatten(targetVelocity) * predictionTime;
        return Seek(predictedTarget);
    }

    public Vector3 Evade(Transform threat, Vector3 threatVelocity)
    {
        Vector3 predictedThreat = threat.position + Flatten(threatVelocity) * predictionTime;
        return Flee(predictedThreat);
    }

    public Vector3 Wander()
    {
        float jitter = wanderJitter * Time.deltaTime;
        wanderTarget += new Vector3(Random.Range(-1f, 1f) * jitter, 0f, Random.Range(-1f, 1f) * jitter);
        wanderTarget = wanderTarget.normalized * wanderRadius;

        Vector3 localTarget = wanderTarget + Vector3.forward * wanderDistance;
        Vector3 worldTarget = transform.TransformPoint(localTarget);
        return Seek(worldTarget);
    }

    public void Move(Vector3 steeringForce)
    {
        Vector3 force = Vector3.ClampMagnitude(Flatten(steeringForce), maxForce);
        Vector3 acceleration = force / Mathf.Max(0.001f, mass);
        velocity = Vector3.ClampMagnitude(velocity + acceleration * Time.deltaTime, maxSpeed);

        Vector3 nextPosition = rb.position + velocity * Time.deltaTime;
        rb.MovePosition(nextPosition);

        if (velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime));
        }
    }

    public void Stop()
    {
        velocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
