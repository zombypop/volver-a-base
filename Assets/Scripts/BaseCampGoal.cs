using UnityEngine;

/// <summary>
/// The goal at the foot of the mountain. Put this on the GameObject holding the (trigger)
/// Box Collider 2D (e.g. "base-camp-all"). When the player reaches it, they've made it back
/// to base and the game is won. Fires once.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BaseCampGoal : MonoBehaviour
{
    private bool won;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (won) return;

        // The player's body collider sits on the same object as PlayerMovement, but
        // check the parents too in case a child collider is what entered the trigger.
        PlayerMovement player = other.GetComponent<PlayerMovement>()
                                ?? other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        won = true;
        Debug.Log($"{player.name} reached base camp — you win!");
    }
}
