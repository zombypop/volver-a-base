using UnityEngine;

/// <summary>
/// An ice-axe item lying in the world. Put this on the GameObject holding the (trigger)
/// Box Collider 2D. When the player walks into it, a fresh axe is added to their pack
/// (up to capacity) and the item is consumed.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AxePickup : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        // The player's body collider sits on the same object as PlayerMovement, but
        // check the parents too in case a child collider is what entered the trigger.
        PlayerMovement player = other.GetComponent<PlayerMovement>()
                                ?? other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        player.AddAxe();
        Destroy(gameObject);
    }
}
