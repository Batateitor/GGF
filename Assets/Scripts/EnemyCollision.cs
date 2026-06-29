using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyCollision : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (CollisionFilters.IsPlayer(collision.collider))
        {
            RestartScene();
        }
    }

    private void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
