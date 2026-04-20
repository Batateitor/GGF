using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float normalSpeed = 5f;
    [SerializeField] private float stealthSpeedMultiplier = 0.66f;

    [Header("Noise")]
    [SerializeField] private NoiseEmitter noiseEmitter;
    [SerializeField] private float normalNoiseRadius = 5f;
    [SerializeField] private float stealthNoiseMultiplier = 0.5f;

    private bool isStealth;

    private void Update()
    {
        HandleStealthInput();
        HandleMovement();
    }

    private void HandleStealthInput()
    {
        isStealth = Input.GetKey(KeyCode.LeftShift);
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v);

        float currentSpeed = isStealth
            ? normalSpeed * stealthSpeedMultiplier
            : normalSpeed;

        transform.Translate(move.normalized * currentSpeed * Time.deltaTime);

        HandleNoise(move);
    }

    private void HandleNoise(Vector3 move)
    {
        if (move.magnitude <= 0) return;

        float currentNoise = isStealth
            ? normalNoiseRadius * stealthNoiseMultiplier
            : normalNoiseRadius;

        noiseEmitter.EmitNoise(currentNoise);
    }
}