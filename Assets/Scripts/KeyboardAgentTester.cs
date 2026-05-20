using UnityEngine;

public class KeyboardAgentTester : MonoBehaviour
{
    public AgentMovement agent;

    void Update()
    {
        if (agent == null) return;

        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) direction += Vector3.back;
        if (Input.GetKey(KeyCode.A)) direction += Vector3.left;
        if (Input.GetKey(KeyCode.D)) direction += Vector3.right;

        if (direction == Vector3.zero)
            agent.StopMoving();
        else
            agent.MoveInDirection(direction);
    }
}