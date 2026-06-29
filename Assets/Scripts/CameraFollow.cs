using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private float height = 13f;
    [SerializeField] private float zOffset = -6f;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 rotationEuler = new Vector3(60f, 0f, 0f);

    private void LateUpdate()
    {
        if (target == null) return;

        FollowTarget();
        ApplyRotation();
    }

    private void FollowTarget()
    {
        Vector3 desiredPosition = new Vector3(
            target.position.x,
            height,
            target.position.z + zOffset
        );

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(rotationEuler);
    }
}
