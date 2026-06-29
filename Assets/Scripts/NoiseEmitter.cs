using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    public void EmitNoise(float noiseRadius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, noiseRadius);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<AdvancedEnemyAgent>(out var advancedEnemy))
            {
                advancedEnemy.OnHearNoise(transform.position, noiseRadius);
                continue;
            }

            if (hit.TryGetComponent<EnemyController>(out var enemy))
            {
                enemy.OnHearNoise(transform.position);
            }
        }
    }
}
