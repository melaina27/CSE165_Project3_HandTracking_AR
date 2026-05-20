using UnityEngine;

public class AgentMovement : MonoBehaviour
{
    public float moveSpeed = 1.0f;
    public float turnSpeed = 8.0f;
    public float obstacleCheckDistance = 0.6f;
    public LayerMask obstacleLayer;

    private Vector3 targetDirection = Vector3.zero;

    void Update()
    {
        if (targetDirection == Vector3.zero) return;

        Vector3 direction = targetDirection.normalized;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, obstacleCheckDistance, obstacleLayer))
        {
            StopMoving();
            return;
        }

        transform.position += direction * moveSpeed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    public void MoveInDirection(Vector3 direction)
    {
        direction.y = 0;
        if (direction.magnitude < 0.01f) return;
        targetDirection = direction.normalized;
    }

    public void StopMoving()
    {
        targetDirection = Vector3.zero;
    }
}