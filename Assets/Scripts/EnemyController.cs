using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public VisionSystem vision;
    private NavMeshAgent agent;

    private EnemyFSM fsm;

    private IdleState idle;
    private InvestigateState investigate;
    private ChaseState chase;

    public Vector3 lastHeardPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

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
        agent.SetDestination(target);
    }

    public void OnHearNoise(Vector3 pos)
    {
        lastHeardPosition = pos;
        fsm.ChangeState(investigate);
    }

    public void SwitchToChase() => fsm.ChangeState(chase);
    public void SwitchToIdle() => fsm.ChangeState(idle);
}