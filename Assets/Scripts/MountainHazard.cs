using UnityEngine;

/// <summary>
/// Marks a surface as the mountain wall the wind can slam the player into.
/// Put this on the mountain's (non-trigger) Collider2D. On impact,
/// PlayerMovement reads BaseDamage/ImpactSpeedDamageMultiplier to size the hit.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MountainHazard : MonoBehaviour
{
    [SerializeField] private float minImpactSpeed = 3f;      // below this, it's a graze — no damage at all
    [SerializeField] private float baseDamage = 5f;          // flat damage once minImpactSpeed is exceeded
    [SerializeField] private float impactSpeedDamageMultiplier = 1.5f; // extra damage per unit of speed past the threshold

    public float MinImpactSpeed => minImpactSpeed;
    public float BaseDamage => baseDamage;
    public float ImpactSpeedDamageMultiplier => impactSpeedDamageMultiplier;
}
