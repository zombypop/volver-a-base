using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on a trigger Box Collider 2D. While the player is inside, gusts of
/// wind push them back and forth along the mountain/away-from-mountain axis.
/// The more rope the player has paid out (see PlayerMovement.CurrentRopeLength),
/// the harder they get swung — a taut, short rope barely moves; a long one sways hard.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WindZone : MonoBehaviour
{
    [SerializeField] private Transform mountain;                // wind blows along the axis toward/away from this
    [SerializeField] private float windStrength = 6f;           // base wind acceleration, in units/s² (mass-independent)
    [SerializeField] private float gustFrequency = 0.5f;        // gusts per second (direction alternates as a sine wave)
    [SerializeField] private float ropeLengthWindFactor = 0.4f; // extra acceleration multiplier per meter of rope paid out

    private readonly List<Rigidbody2D> playersInZone = new List<Rigidbody2D>();
    private readonly Dictionary<Rigidbody2D, PlayerMovement> playerMovements = new Dictionary<Rigidbody2D, PlayerMovement>();

    void Awake()
    {
        if (mountain == null)
        {
            GameObject found = GameObject.Find("Mountain");
            if (found != null) mountain = found.transform;
        }
    }

    void FixedUpdate()
    {
        if (mountain == null || playersInZone.Count == 0) return;

        // Horizontal only: the rope's sway is left/right, and a vertical component would
        // just get overwritten every step by the player's rope-descent velocity anyway.
        Vector2 towardMountain = new Vector2(Mathf.Sign(mountain.position.x - transform.position.x), 0f);
        // Sways from +1 (toward the mountain) to -1 (away from it) over time, like real gusts.
        float sway = Mathf.Sin(Time.time * gustFrequency * Mathf.PI * 2f);

        for (int i = 0; i < playersInZone.Count; i++)
        {
            Rigidbody2D rb = playersInZone[i];
            if (rb == null) continue;

            // Wind only matters while roped — let go and you drop straight down, unaffected.
            PlayerMovement movement = playerMovements[rb];
            if (movement == null || !movement.IsOnRope) continue;

            // AddForce (not a direct velocity write) matters here: Unity applies it during its
            // physics step, which runs after every script's FixedUpdate — so it always lands on
            // top of whatever velocity PlayerMovement's rope code assigns, regardless of script
            // execution order. We scale by rb.mass so windStrength still reads as a plain
            // acceleration (units/s²) instead of being swallowed by the player's mass.
            float accel = windStrength * (1f + movement.CurrentRopeLength * ropeLengthWindFactor) * sway;

            rb.AddForce(towardMountain * (accel * rb.mass), ForceMode2D.Force);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null || playersInZone.Contains(rb)) return;

        playersInZone.Add(rb);
        playerMovements[rb] = other.GetComponent<PlayerMovement>();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        playersInZone.Remove(rb);
        playerMovements.Remove(rb);
    }
}
