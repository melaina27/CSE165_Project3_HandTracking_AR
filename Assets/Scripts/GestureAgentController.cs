using UnityEngine;

public class GestureAgentController : MonoBehaviour
{
    public AgentMovement agent;
    public Transform indexTip;
    public Transform wrist;
    public float gestureThreshold = 0.15f;

    void Update()
    {
        if (agent == null || indexTip == null || wrist == null) return;

        Vector3 pointingDirection = indexTip.position - wrist.position;

        if (pointingDirection.magnitude > gestureThreshold)
        {
            agent.MoveInDirection(pointingDirection);
        }
        else
        {
            agent.StopMoving();
        }
    }
}