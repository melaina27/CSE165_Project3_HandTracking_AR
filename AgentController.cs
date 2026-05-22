using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class AgentController : MonoBehaviour
{
    [Header("References")]
    public GestureController gestureController;
    public NavMeshAgent navAgent;
    public Animator animator;
    public NavMeshSurface navMeshSurface;

    [Header("Tuning")]
    public float arrivalThreshold = 0.4f;
    public float navMeshBakeDelay = 1.0f;
    public float stuckTimeout = 3.0f;

    [Header("Debug path visualization")]
    public bool showPathLine = true;
    public Color pathColor = new Color(1f, 0.5f, 0f, 1f);   // orange
    public float pathWidth = 0.02f;

    bool _navMeshBuilt = false;
    float _stuckTimer = 0f;
    LineRenderer _pathLine;

    void Start()
    {
        Invoke(nameof(BuildNavMesh), navMeshBakeDelay);

        if (showPathLine)
        {
            var go = new GameObject("AgentPath");
            _pathLine = go.AddComponent<LineRenderer>();
            _pathLine.material = new Material(Shader.Find("Unlit/Color"));
            _pathLine.material.color = pathColor;
            _pathLine.startWidth = pathWidth;
            _pathLine.endWidth = pathWidth;
            _pathLine.positionCount = 0;
            _pathLine.useWorldSpace = true;
        }
    }

    void BuildNavMesh()
    {
        if (navMeshSurface == null)
        {
            Debug.LogError("[AgentController] No NavMeshSurface assigned!");
            return;
        }

        navMeshSurface.BuildNavMesh();
        _navMeshBuilt = true;
        Debug.Log("[AgentController] NavMesh baked.");

        // Snap the agent onto the NavMesh
        if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
        {
            navAgent.Warp(hit.position);
            Debug.Log($"[AgentController] Agent warped to NavMesh at {hit.position}");
        }
        else
        {
            Debug.LogWarning("[AgentController] Couldn't find NavMesh near agent. Move character closer to the floor.");
        }
    }

    void Update()
    {
        if (!_navMeshBuilt || gestureController == null || navAgent == null) return;
        if (!navAgent.isOnNavMesh) return;

        // STOP signal (right fist)
        if (gestureController.ShouldStop)
        {
            navAgent.ResetPath();
            animator?.SetBool("Walking", false);
            _stuckTimer = 0f;
            Debug.Log("[AgentController] Stop signal received.");
            return;
        }

        // MOVE: send destination
        if (gestureController.ShouldMove)
        {
            if (NavMesh.SamplePosition(gestureController.Destination, out var clamped, 1.5f, NavMesh.AllAreas))
            {
                navAgent.SetDestination(clamped.position);
            }
        }

        // Animator: walking when actually moving
        bool isMoving = navAgent.velocity.magnitude > 0.05f && !navAgent.isStopped;
        animator?.SetBool("Walking", isMoving);

        // Arrival detection
        if (!navAgent.pathPending && navAgent.remainingDistance <= arrivalThreshold)
        {
            if (gestureController.CurrentState == GestureController.State.Moving)
            {
                Debug.Log("[AgentController] Arrived at destination.");
                gestureController.NotifyArrival();
                navAgent.ResetPath();
                _stuckTimer = 0f;
            }
        }

        // Stuck detection: while supposed to be moving but velocity is near zero
        if (gestureController.CurrentState == GestureController.State.Moving)
        {
            if (navAgent.velocity.magnitude < 0.05f && !navAgent.pathPending)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > stuckTimeout)
                {
                    Debug.LogWarning("[AgentController] Agent stuck, resetting state.");
                    navAgent.ResetPath();
                    gestureController.NotifyArrival();
                    _stuckTimer = 0f;
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
        }

        // Path visualization
        UpdatePathLine();
    }

    void UpdatePathLine()
    {
        if (_pathLine == null) return;

        if (navAgent.hasPath)
        {
            var corners = navAgent.path.corners;
            _pathLine.positionCount = corners.Length;
            _pathLine.SetPositions(corners);
        }
        else
        {
            _pathLine.positionCount = 0;
        }
    }
}