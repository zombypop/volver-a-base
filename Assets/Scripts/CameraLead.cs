using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Leads the camera ahead of the player in the direction they're facing: shift right while
/// facing/moving right (more room to see where you're going), and gently back left when facing
/// left. Works by nudging the CinemachineFollow's horizontal FollowOffset, smoothed so the
/// camera eases over instead of snapping.
///
/// Put this on the CinemachineCamera object (or anywhere, and assign the references). The
/// vertical/z framing from the original FollowOffset is preserved.
/// </summary>
public class CameraLead : MonoBehaviour
{
    [SerializeField] private CinemachineFollow follow;   // auto-found on self/scene if empty
    [SerializeField] private PlayerMovement player;      // auto-found if empty

    [Header("Lead distance (world units)")]
    [SerializeField] private float rightLead = 2f;       // how far the camera leads when facing right
    [SerializeField] private float leftLead = 2f;        // how far it leads when facing left
    [SerializeField] private float smoothTime = 0.6f;    // easing time; larger = gentler drift

    private float baseOffsetX; // the authored horizontal offset we lead relative to
    private float currentLead; // current smoothed horizontal lead
    private float velocity;    // SmoothDamp state

    void Awake()
    {
        if (follow == null) follow = GetComponent<CinemachineFollow>();
        if (follow == null) follow = FindFirstObjectByType<CinemachineFollow>();
        if (player == null) player = FindFirstObjectByType<PlayerMovement>();

        if (follow != null) baseOffsetX = follow.FollowOffset.x;
    }

    void LateUpdate()
    {
        if (follow == null || player == null) return;

        // Target lead: positive (right) or negative (left) depending on facing.
        float target = player.FacingLeft ? -leftLead : rightLead;
        currentLead = Mathf.SmoothDamp(currentLead, target, ref velocity, smoothTime);

        Vector3 offset = follow.FollowOffset;
        offset.x = baseOffsetX + currentLead;
        follow.FollowOffset = offset;
    }
}
