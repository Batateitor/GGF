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

        if (enemy.vision.CanSeeTarget(enemy.player))
        {
            enemy.SwitchToChase();
        }

        if (Vector3.Distance(enemy.transform.position, enemy.lastHeardPosition) < 1f)
        {
            enemy.SwitchToIdle();
        }
    }

    public void Exit() { }
}