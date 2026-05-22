using UnityEngine;

public class GestureController : MonoBehaviour
{
    public enum State { Idle, Targeting, TargetLocked, Moving }

    [Header("Hands")]
    public OVRHand rightHand;
    public OVRSkeleton rightSkeleton;
    public OVRHand leftHand;
    public OVRSkeleton leftSkeleton;

    [Header("Pointing")]
    public LayerMask surfaceMask;
    public float pointMaxDistance = 10f;

    [Header("Marker")]
    public GameObject markerPrefab;
    public Color targetingColor = Color.yellow;
    public Color targetLockedColor = Color.green;

    [Header("Pointer line")]
    public bool showPointerLine = true;
    public Material pointerLineMaterial;
    public Color pointerLineColor = new Color(0.4f, 1f, 1f, 0.8f);
    public float pointerLineWidth = 0.005f;

    [Header("Gesture debounce (seconds)")]
    public float gestureHoldTime = 0.2f;

    // ====== Public state for Task 4's agent code to read ======
    public State CurrentState { get; private set; } = State.Idle;
    public Vector3 Destination { get; private set; }
    public bool ShouldMove => CurrentState == State.Moving;
    public bool ShouldStop { get; private set; }

    // ====== Private ======
    GameObject _marker;
    Renderer _markerRenderer;
    LineRenderer _pointerLine;
    float _leftThumbsUpHeld = 0f;
    float _leftThumbsDownHeld = 0f;
    float _rightThumbsUpHeld = 0f;
    float _rightFistHeld = 0f;
    State _lastLoggedState = State.Idle;

    void Start()
    {
        if (markerPrefab != null)
        {
            _marker = Instantiate(markerPrefab);
            _markerRenderer = _marker.GetComponentInChildren<Renderer>();
            _marker.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[GestureController] markerPrefab not assigned!");
        }

        if (showPointerLine)
        {
            var go = new GameObject("PointerLine");
            _pointerLine = go.AddComponent<LineRenderer>();
            _pointerLine.material = pointerLineMaterial != null
                ? pointerLineMaterial
                : new Material(Shader.Find("Unlit/Color"));
            _pointerLine.material.color = pointerLineColor;
            _pointerLine.startWidth = pointerLineWidth;
            _pointerLine.endWidth = pointerLineWidth;
            _pointerLine.positionCount = 2;
            _pointerLine.useWorldSpace = true;
            _pointerLine.enabled = false;
        }
    }

    void Update()
    {
        ShouldStop = false;

        // --- Read raw inputs ---
        bool rightOK = rightHand != null && rightHand.IsTracked &&
                       rightHand.HandConfidence == OVRHand.TrackingConfidence.High;
        bool leftOK = leftHand != null && leftHand.IsTracked &&
                       leftHand.HandConfidence == OVRHand.TrackingConfidence.High;

        bool rightPointing = rightOK && HandGestureUtils.IsIndexPointing(rightSkeleton);
        bool rightThumb = rightOK && HandGestureUtils.IsThumbsUp(rightSkeleton);
        bool rightFist = rightOK && HandGestureUtils.IsFist(rightSkeleton);
        bool leftThumb = leftOK && HandGestureUtils.IsThumbsUp(leftSkeleton);
        bool leftThumbDown = leftOK && HandGestureUtils.IsThumbsDown(leftSkeleton);

        // --- Debounce ---
        _leftThumbsUpHeld = leftThumb ? _leftThumbsUpHeld + Time.deltaTime : 0f;
        _leftThumbsDownHeld = leftThumbDown ? _leftThumbsDownHeld + Time.deltaTime : 0f;
        _rightThumbsUpHeld = rightThumb ? _rightThumbsUpHeld + Time.deltaTime : 0f;
        _rightFistHeld = rightFist ? _rightFistHeld + Time.deltaTime : 0f;

        bool leftThumbConfirmed = _leftThumbsUpHeld >= gestureHoldTime;
        bool leftThumbDownConfirmed = _leftThumbsDownHeld >= gestureHoldTime;
        bool rightThumbConfirmed = _rightThumbsUpHeld >= gestureHoldTime;
        bool rightFistConfirmed = _rightFistHeld >= gestureHoldTime;

        // --- Right-hand pointing ray ---
        Vector3? hitPoint = null;
        if (rightPointing && TryGetPointingRay(out Ray ray))
        {
            bool hitSurface = Physics.Raycast(ray, out RaycastHit hit, pointMaxDistance, surfaceMask);
            if (hitSurface) hitPoint = hit.point;

            if (_pointerLine != null)
            {
                _pointerLine.enabled = true;
                _pointerLine.SetPosition(0, ray.origin);
                _pointerLine.SetPosition(1, hitSurface ? hit.point : ray.origin + ray.direction * pointMaxDistance);
                _pointerLine.material.color = hitSurface
                    ? new Color(0.3f, 1f, 0.3f, 0.9f)
                    : new Color(1f, 0.3f, 0.3f, 0.5f);
            }
        }
        else if (_pointerLine != null)
        {
            _pointerLine.enabled = false;
        }

        // --- State machine ---
        switch (CurrentState)
        {
            case State.Idle:
                if (hitPoint.HasValue)
                {
                    ShowMarker(hitPoint.Value, targetingColor);
                    Destination = hitPoint.Value;
                    CurrentState = State.Targeting;
                }
                break;

            case State.Targeting:
                // Left thumbs-up locks immediately, no matter the right hand state
                if (leftThumbConfirmed)
                {
                    ShowMarker(Destination, targetLockedColor);
                    CurrentState = State.TargetLocked;
                    break;
                }

                if (rightPointing && hitPoint.HasValue)
                {
                    Destination = hitPoint.Value;
                    ShowMarker(hitPoint.Value, targetingColor);
                }
                else if (!rightPointing && !hitPoint.HasValue)
                {
                    HideMarker();
                    CurrentState = State.Idle;
                }
                // Otherwise: right hand drifted off briefly, keep last marker
                break;

            case State.TargetLocked:
                // Left thumbs-down unlocks
                if (leftThumbDownConfirmed)
                {
                    ShowMarker(Destination, targetingColor);
                    CurrentState = State.Targeting;
                    break;
                }

                if (rightThumbConfirmed)
                {
                    CurrentState = State.Moving;
                }
                break;

            case State.Moving:
                if (rightFistConfirmed)
                {
                    HideMarker();
                    ShouldStop = true;
                    CurrentState = State.Idle;
                }
                break;
        }

        // --- TEMP: log state transitions ---
        if (_lastLoggedState != CurrentState)
        {
            Debug.Log($"[GestureController] State: {_lastLoggedState} → {CurrentState}");
            _lastLoggedState = CurrentState;
        }
    }

    bool TryGetPointingRay(out Ray ray)
    {
        ray = default;
        if (rightSkeleton == null || rightSkeleton.Bones == null) return false;

        Transform proximal = null, tip = null;
        foreach (var b in rightSkeleton.Bones)
        {
            if (b.Id == OVRSkeleton.BoneId.XRHand_IndexProximal) proximal = b.Transform;
            if (b.Id == OVRSkeleton.BoneId.XRHand_IndexTip) tip = b.Transform;
        }
        if (proximal == null || tip == null) return false;

        Vector3 dir = (tip.position - proximal.position).normalized;
        ray = new Ray(tip.position, dir);
        return true;
    }

    void ShowMarker(Vector3 pos, Color c)
    {
        if (_marker == null) return;
        _marker.SetActive(true);
        _marker.transform.position = pos;
        if (_markerRenderer != null) _markerRenderer.material.color = c;
    }

    void HideMarker()
    {
        if (_marker != null) _marker.SetActive(false);
    }

    public void NotifyArrival()
    {
        HideMarker();
        CurrentState = State.Idle;
    }
}