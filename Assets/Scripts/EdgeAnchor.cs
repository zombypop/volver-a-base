using UnityEngine;

/// <summary>
/// Marks a GameObject as an edge the player can rappel from. Put this on the
/// object that holds the (trigger) Circle Collider 2D. The player detects it on
/// overlap and, while the right mouse button is held, descends on a rope anchored here.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EdgeAnchor : MonoBehaviour
{
    // The rope hangs from this point. Defaults to the object's own position,
    // but you can offset it (e.g. to the lip of the ledge) via ropePoint.
    [SerializeField] private Transform ropePoint;

    public Vector2 RopeOrigin => ropePoint != null ? (Vector2)ropePoint.position : (Vector2)transform.position;
}
