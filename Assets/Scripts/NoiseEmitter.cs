using System.Collections.Generic;
using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    private readonly HashSet<AdvancedEnemyAgent> notifiedAdvancedAgents = new HashSet<AdvancedEnemyAgent>();
    private readonly HashSet<EnemyController> notifiedLegacyAgents = new HashSet<EnemyController>();

    public void EmitNoise(float noiseRadius)
    {
        notifiedAdvancedAgents.Clear();
        notifiedLegacyAgents.Clear();
        Collider[] hits = Physics.OverlapSphere(transform.position, noiseRadius);

        foreach (var hit in hits)
        {
            AdvancedEnemyAgent advancedEnemy = hit.GetComponentInParent<AdvancedEnemyAgent>();
            if (advancedEnemy != null && notifiedAdvancedAgents.Add(advancedEnemy))
            {
                advancedEnemy.OnHearNoise(transform.position, noiseRadius);
                continue;
            }

            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null && notifiedLegacyAgents.Add(enemy))
            {
                enemy.OnHearNoise(transform.position);
            }
        }
    }
}
