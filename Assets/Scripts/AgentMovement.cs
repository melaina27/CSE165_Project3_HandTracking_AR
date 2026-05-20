using UnityEngine;

public class AgentMovement : MonoBehaviour
{
    public float moveSpeed = 1.0f;
    public float turnSpeed = 8.0f;

    private Vector3 targetDirection = Vector3.zero;

    void Update()
    {
        if (targetDirection == Vector3.zero) return;

        Vector3 move = targetDirection.normalized * moveSpeed * Time.deltaTime;
        transform.position += move;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    public void MoveInDirection(Vector3 direction)
    {
        direction.y = 0;
        targetDirection = direction.normalized;
    }

    public void StopMoving()
    {
        targetDirection = Vector3.zero;
    }
}