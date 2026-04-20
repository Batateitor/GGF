using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    public void EmitNoise(float noiseRadius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, noiseRadius);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyController>(out var enemy))
            {
                enemy.OnHearNoise(transform.position);
            }
        }
    }
}