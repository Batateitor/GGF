using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public VisionSystem vision;

    [Header("Movement")]
    public float fallbackMoveSpeed = 3f;
    public float investigateStopDistance = 1f;

    private NavMeshAgent agent;
    private Rigidbody rb;

    private EnemyFSM fsm;

    private IdleState idle;
    private InvestigateState investigate;
    private ChaseState chase;

    public Vector3 lastHeardPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        ConfigureRigidbody();

        if (vision == null)
        {
            vision = GetComponent<VisionSystem>();
        }

        ResolvePlayerReference();

        fsm = new EnemyFSM();

        idle = new IdleState(this);
        investigate = new InvestigateState(this);
        chase = new ChaseState(this);
    }

    private void Start()
    {
        fsm.ChangeState(idle);
    }

    private void Update()
    {
        fsm.Update();
    }

    public void MoveTowards(Vector3 target)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(target);
            return;
        }

        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 nextPosition = Vector3.MoveTowards(transform.position, transform.position + direction, fallbackMoveSpeed * Time.deltaTime);
        rb.MovePosition(nextPosition);
    }

    public void OnHearNoise(Vector3 pos)
    {
        lastHeardPosition = pos;
        fsm.ChangeState(investigate);
    }

    public void SwitchToChase() => fsm.ChangeState(chase);
    public void SwitchToIdle() => fsm.ChangeState(idle);

    public bool CanSeePlayer()
    {
        return player != null && vision != null && vision.CanSeeTarget(player);
    }

    private void ConfigureRigidbody()
    {
        rb.useGravity = false;
        rb.isKinematic = agent != null && agent.enabled;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void ResolvePlayerReference()
    {
        if (player != null)
        {
            return;
        }

        PlayerController playerController = Object.FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
            return;
        }

        NoiseEmitter noiseEmitter = Object.FindAnyObjectByType<NoiseEmitter>();
        if (noiseEmitter != null)
        {
            player = noiseEmitter.transform;
        }
    }
}
