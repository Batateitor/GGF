public class ChaseState : IState
{
    private EnemyController enemy;

    public ChaseState(EnemyController enemy)
    {
        this.enemy = enemy;
    }

    public void Enter() { }

    public void Update()
    {
        if (enemy.CanSeePlayer())
        {
            enemy.MoveTowards(enemy.player.position);
        }
        else
        {
            enemy.SwitchToIdle();
        }
    }

    public void Exit() { }
}
