using UnityEngine;

public class InvestigateState : IState
{
    private EnemyController enemy;

    public InvestigateState(EnemyController enemy)
    {
        this.enemy = enemy;
    }

    public void Enter() { }

    public void Update()
    {
        enemy.MoveTowards(enemy.lastHeardPosition);

        if (enemy.CanSeePlayer())
        {
            enemy.SwitchToChase();
        }

        if (Vector3.Distance(enemy.transform.position, enemy.lastHeardPosition) < enemy.investigateStopDistance)
        {
            enemy.SwitchToIdle();
        }
    }

    public void Exit() { }
}
