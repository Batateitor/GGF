public class IdleState : IState
{
    private EnemyController enemy;

    public IdleState(EnemyController enemy)
    {
        this.enemy = enemy;
    }

    public void Enter() { }

    public void Update()
    {
        if (enemy.CanSeePlayer())
        {
            enemy.SwitchToChase();
        }
    }

    public void Exit() { }
}
