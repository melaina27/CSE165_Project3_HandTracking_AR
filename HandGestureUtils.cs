using UnityEngine;

/// <summary>
/// Static helpers that compute hand pose features directly from OVRSkeleton bones.
/// All detections are algorithmic (vector/angle math); no calls to OVRHand's built-in detectors.
/// </summary>
public static class HandGestureUtils
{
    const float STRAIGHT_THRESHOLD = 0.75f;
    const float CURLED_THRESHOLD = 0.5f;

    public static bool IsFingerExtended(OVRSkeleton skel, OVRSkeleton.BoneId proximal,
                                                          OVRSkeleton.BoneId intermediate,
                                                          OVRSkeleton.BoneId tip)
    {
        if (!TryGetBone(skel, proximal, out var p) ||
            !TryGetBone(skel, intermediate, out var i) ||
            !TryGetBone(skel, tip, out var t)) return false;

        Vector3 dir1 = (i.position - p.position).normalized;
        Vector3 dir2 = (t.position - i.position).normalized;
        return Vector3.Dot(dir1, dir2) > STRAIGHT_THRESHOLD;
    }

    public static bool IsFingerCurled(OVRSkeleton skel, OVRSkeleton.BoneId proximal,
                                                        OVRSkeleton.BoneId intermediate,
                                                        OVRSkeleton.BoneId tip)
    {
        if (!TryGetBone(skel, proximal, out var p) ||
            !TryGetBone(skel, intermediate, out var i) ||
            !TryGetBone(skel, tip, out var t)) return false;

        Vector3 dir1 = (i.position - p.position).normalized;
        Vector3 dir2 = (t.position - i.position).normalized;
        return Vector3.Dot(dir1, dir2) < CURLED_THRESHOLD;
    }

    // ============ Gesture-level helpers ============

    public static bool IsIndexPointing(OVRSkeleton skel)
    {
        return IsFingerExtended(skel,
            OVRSkeleton.BoneId.XRHand_IndexProximal,
            OVRSkeleton.BoneId.XRHand_IndexIntermediate,
            OVRSkeleton.BoneId.XRHand_IndexTip);
    }

    public static bool IsThumbsUp(OVRSkeleton skel)
    {
        if (!IsThumbAndFistShape(skel)) return false;

        if (!TryGetBone(skel, OVRSkeleton.BoneId.XRHand_ThumbProximal, out var p)) return false;
        if (!TryGetBone(skel, OVRSkeleton.BoneId.XRHand_ThumbTip, out var t)) return false;

        Vector3 thumbDir = (t.position - p.position).normalized;
        return Vector3.Dot(thumbDir, Vector3.up) > 0.5f;
    }

    public static bool IsThumbsDown(OVRSkeleton skel)
    {
        if (!IsThumbAndFistShape(skel)) return false;

        if (!TryGetBone(skel, OVRSkeleton.BoneId.XRHand_ThumbProximal, out var p)) return false;
        if (!TryGetBone(skel, OVRSkeleton.BoneId.XRHand_ThumbTip, out var t)) return false;

        Vector3 thumbDir = (t.position - p.position).normalized;
        return Vector3.Dot(thumbDir, Vector3.down) > 0.5f;
    }

    /// <summary>
    /// Fist: four fingers curled, thumb NOT extended.
    /// Matches how humans actually make fists (thumb tucked across palm, not curled backward).
    /// </summary>
    public static bool IsFist(OVRSkeleton skel)
    {
        bool index = IsFingerCurled(skel, OVRSkeleton.BoneId.XRHand_IndexProximal,
                                           OVRSkeleton.BoneId.XRHand_IndexIntermediate,
                                           OVRSkeleton.BoneId.XRHand_IndexTip);
        bool middle = IsFingerCurled(skel, OVRSkeleton.BoneId.XRHand_MiddleProximal,
                                            OVRSkeleton.BoneId.XRHand_MiddleIntermediate,
                                            OVRSkeleton.BoneId.XRHand_MiddleTip);
        bool ring = IsFingerCurled(skel, OVRSkeleton.BoneId.XRHand_RingProximal,
                                          OVRSkeleton.BoneId.XRHand_RingIntermediate,
                                          OVRSkeleton.BoneId.XRHand_RingTip);
        bool pinky = IsFingerCurled(skel, OVRSkeleton.BoneId.XRHand_LittleProximal,
                                           OVRSkeleton.BoneId.XRHand_LittleIntermediate,
                                           OVRSkeleton.BoneId.XRHand_LittleTip);
        if (!(index && middle && ring && pinky)) return false;

        // Disambiguate from thumbs-up/down: thumb must NOT be extended
        bool thumbExtended = IsFingerExtended(skel, OVRSkeleton.BoneId.XRHand_ThumbProximal,
                                                     OVRSkeleton.BoneId.XRHand_ThumbDistal,
                                                     OVRSkeleton.BoneId.XRHand_ThumbTip);
        return !thumbExtended;
    }

    /// <summary>
    /// Shared shape check: thumb extended, all four fingers curled.
    /// Used by both ThumbsUp and ThumbsDown; the direction check happens in those methods.
    /// </summary>
    static bool IsThumbAndFistShape(OVRSkeleton skel)
    {
        bool thumb = IsFingerExtended(skel, OVRSkeleton.BoneId.XRHand_ThumbProximal,
                                             OVRSkeleton.BoneId.XRHand_ThumbDistal,
                                             OVRSkeleton.BoneId.XRHand_ThumbTip);
        bool index = IsFingerCurled(skel, OVRSkeleton.BoneId.XRHand_IndexProximal,
                                           OVRSkeleton.BoneId.XRHand_IndexIntermediate,
                                           OVRSkeleton.BoneId.XRHand_IndexTip);
        bool middle = IsFingerCurled(skel, OVRSkeleton.BoneId.XRHand_MiddleProximal,
                                            OVRSkeleton.BoneId.XRHand_MiddleIntermediate,
                                            OVRSkeleton.BoneId.XRHand_MiddleTip);
        bool ring = IsFingerCurled(skel, OVRSkeleton.BoneId.XRHand_RingProximal,
                                          OVRSkeleton.BoneId.XRHand_RingIntermediate,
                                          OVRSkeleton.BoneId.XRHand_RingTip);
        bool pinky = IsFingerCurled(skel, OVRSkeleton.BoneId.XRHand_LittleProximal,
                                           OVRSkeleton.BoneId.XRHand_LittleIntermediate,
                                           OVRSkeleton.BoneId.XRHand_LittleTip);
        return thumb && index && middle && ring && pinky;
    }

    // ============ Utilities ============

    static bool TryGetBone(OVRSkeleton skel, OVRSkeleton.BoneId id, out Transform t)
    {
        t = null;
        if (skel == null || skel.Bones == null || skel.Bones.Count == 0) return false;
        foreach (var bone in skel.Bones)
        {
            if (bone.Id == id) { t = bone.Transform; return true; }
        }
        return false;
    }
}