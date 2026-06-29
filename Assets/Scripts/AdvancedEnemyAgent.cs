using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SteeringAgent))]
public class AdvancedEnemyAgent : MonoBehaviour
{
    public enum EnemyStyle
    {
        FastLowHearing,
        SlowListener,
        Balanced
    }

    private enum EnemyState
    {
        Patrol,
        Investigate,
        Chase,
        Search,
        Evade,
        Attack
    }

    [Header("Identity")]
    public EnemyStyle style = EnemyStyle.Balanced;
    public string displayName = "Enemy";

    [Header("References")]
    public Transform player;
    public Transform[] patrolPoints;

    [Header("Senses")]
    public float hearingRadius = 5f;
    public float viewDistance = 7f;
    [Range(20f, 180f)] public float viewAngle = 95f;
    public LayerMask visionBlockMask = -1;
    public LayerMask pathObstacleMask = (1 << 0) | (1 << 3);

    [Header("Movement Speeds")]
    public float patrolSpeed = 2.5f;
    public float investigateSpeed = 3f;
    public float chaseSpeed = 4.5f;
    public float evadeSpeed = 4f;

    [Header("Decision Tuning")]
    public float pathRefreshInterval = 0.45f;
    public float waypointReachDistance = 0.65f;
    public float searchDuration = 2f;
    public float attackRange = 1f;
    public float evadeDistance = 1.6f;
    public bool usesPursueInChase = true;
    public bool evadesWhenCornered;

    private readonly List<Vector3> currentPath = new List<Vector3>();
    private SteeringAgent steering;
    private EnemyState state;
    private Vector3 lastHeardPosition;
    private Vector3 lastSeenPosition;
    private Vector3 previousPlayerPosition;
    private int patrolIndex;
    private int pathIndex;
    private float nextPathRefreshTime;
    private float searchTimer;
    private float attackCooldown;

    private void Awake()
    {
        steering = GetComponent<SteeringAgent>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                previousPlayerPosition = player.position;
            }
        }

        ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        attackCooldown -= Time.deltaTime;

        switch (state)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Investigate:
                UpdateInvestigate();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Search:
                UpdateSearch();
                break;
            case EnemyState.Evade:
                UpdateEvade();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
        }

        if (player != null)
        {
            previousPlayerPosition = player.position;
        }
    }

    public void OnHearNoise(Vector3 noisePosition, float emittedRadius)
    {
        float heardDistance = Vector3.Distance(transform.position, noisePosition);
        if (heardDistance > hearingRadius || heardDistance > emittedRadius)
        {
            return;
        }

        lastHeardPosition = noisePosition;
        if (state != EnemyState.Chase && state != EnemyState.Attack)
        {
            ChangeState(EnemyState.Investigate);
        }
    }

    private void UpdatePatrol()
    {
        if (TryReactToPlayer())
        {
            return;
        }

        steering.SetSpeed(patrolSpeed);

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            steering.Move(steering.Wander());
            return;
        }

        Transform patrolTarget = patrolPoints[patrolIndex];
        if (patrolTarget == null)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            return;
        }

        FollowPathTo(patrolTarget.position, false);

        if (Vector3.Distance(transform.position, patrolTarget.position) <= waypointReachDistance)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            ClearPath();
        }
    }

    private void UpdateInvestigate()
    {
        if (TryReactToPlayer())
        {
            return;
        }

        steering.SetSpeed(investigateSpeed);
        FollowPathTo(lastHeardPosition, false);

        if (Vector3.Distance(transform.position, lastHeardPosition) <= waypointReachDistance)
        {
            ChangeState(EnemyState.Search);
        }
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            ChangeState(EnemyState.Search);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        if (evadesWhenCornered && distanceToPlayer <= evadeDistance)
        {
            ChangeState(EnemyState.Evade);
            return;
        }

        if (CanSeePlayer())
        {
            lastSeenPosition = player.position;
            steering.SetSpeed(chaseSpeed);

            Vector3 playerVelocity = (player.position - previousPlayerPosition) / Mathf.Max(Time.deltaTime, 0.001f);
            Vector3 steeringForce = usesPursueInChase
                ? steering.Pursue(player, playerVelocity)
                : steering.Seek(player.position);

            FollowPathTo(player.position, false, steeringForce);
        }
        else
        {
            lastHeardPosition = lastSeenPosition;
            ChangeState(EnemyState.Investigate);
        }
    }

    private void UpdateSearch()
    {
        if (TryReactToPlayer())
        {
            return;
        }

        searchTimer -= Time.deltaTime;
        steering.SetSpeed(patrolSpeed * 0.75f);
        steering.Move(steering.Wander() * 0.5f);

        if (searchTimer <= 0f)
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    private void UpdateEvade()
    {
        if (player == null)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > evadeDistance * 1.8f)
        {
            ChangeState(CanSeePlayer() ? EnemyState.Chase : EnemyState.Search);
            return;
        }

        steering.SetSpeed(evadeSpeed);
        Vector3 playerVelocity = (player.position - previousPlayerPosition) / Mathf.Max(Time.deltaTime, 0.001f);
        steering.Move(steering.Evade(player, playerVelocity));
    }

    private void UpdateAttack()
    {
        steering.Stop();

        if (player == null)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        if (Vector3.Distance(transform.position, player.position) > attackRange * 1.25f)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (attackCooldown <= 0f)
        {
            attackCooldown = 1f;
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }

    private bool TryReactToPlayer()
    {
        if (!CanSeePlayer())
        {
            return false;
        }

        lastSeenPosition = player.position;
        ChangeState(EnemyState.Chase);
        return true;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 target = player.position + Vector3.up * 0.6f;
        Vector3 toPlayer = target - origin;
        float distance = toPlayer.magnitude;

        if (distance > viewDistance)
        {
            return false;
        }

        Vector3 flatDirection = toPlayer;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            float angle = Vector3.Angle(transform.forward, flatDirection.normalized);
            if (angle > viewAngle * 0.5f)
            {
                return false;
            }
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, toPlayer.normalized, distance, visionBlockMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            if (hitTransform == player || hitTransform.IsChildOf(player))
            {
                return true;
            }

            return false;
        }

        return true;
    }

    private void FollowPathTo(Vector3 target, bool forceRefresh)
    {
        FollowPathTo(target, forceRefresh, Vector3.zero);
    }

    private void FollowPathTo(Vector3 target, bool forceRefresh, Vector3 directSteeringOverride)
    {
        bool shouldRefresh = forceRefresh || Time.time >= nextPathRefreshTime || currentPath.Count == 0;
        if (shouldRefresh)
        {
            currentPath.Clear();
            currentPath.AddRange(AStarPathfinder.FindPath(transform.position, target, pathObstacleMask));
            pathIndex = 0;
            nextPathRefreshTime = Time.time + pathRefreshInterval;
        }

        Vector3 moveTarget = target;
        if (currentPath.Count > 0)
        {
            pathIndex = Mathf.Clamp(pathIndex, 0, currentPath.Count - 1);
            moveTarget = currentPath[pathIndex];

            if (Vector3.Distance(transform.position, moveTarget) <= waypointReachDistance && pathIndex < currentPath.Count - 1)
            {
                pathIndex++;
                moveTarget = currentPath[pathIndex];
            }
        }

        Vector3 steeringForce = directSteeringOverride.sqrMagnitude > 0.001f && IsFinalPathStep()
            ? directSteeringOverride
            : steering.Arrive(moveTarget);

        steering.Move(steeringForce);
    }

    private bool IsFinalPathStep()
    {
        return currentPath.Count == 0 || pathIndex >= currentPath.Count - 1;
    }

    private void ChangeState(EnemyState nextState)
    {
        if (state == nextState)
        {
            return;
        }

        state = nextState;
        ClearPath();

        if (state == EnemyState.Search)
        {
            searchTimer = searchDuration;
        }
    }

    private void ClearPath()
    {
        currentPath.Clear();
        pathIndex = 0;
        nextPathRefreshTime = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Gizmos.color = Color.cyan;
        for (int i = 0; i < currentPath.Count; i++)
        {
            Gizmos.DrawSphere(currentPath[i] + Vector3.up * 0.1f, 0.14f);
            if (i > 0)
            {
                Gizmos.DrawLine(currentPath[i - 1] + Vector3.up * 0.1f, currentPath[i] + Vector3.up * 0.1f);
            }
        }
    }
}
