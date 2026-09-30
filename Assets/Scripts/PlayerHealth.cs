using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invulnerabilityDuration = 0.5f; // ignore further hits right after one lands
 
    [SerializeField] private Image healthFill;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float hurtShakeForce = 0.3f; // scales the impulse — small so getting hurt is a little jolt, not a quake


    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    public event System.Action Died;

    private float invulnerableUntil = -1f;

    void Awake()
    {
        CurrentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f || Time.time < invulnerableUntil) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        invulnerableUntil = Time.time + invulnerabilityDuration;
        UpdateHealthBar();

        // A little sideways camera kick so a hit is felt, not just seen.
        if (impulseSource != null) impulseSource.GenerateImpulse(Vector3.right * hurtShakeForce);

        Debug.Log($"{name} took {amount:F1} damage — health now {CurrentHealth:F1}/{maxHealth:F1}");

        if (IsDead)
        {
            Die();
        }
    }

    // Ignores invulnerability and remaining health — for unsurvivable events like a killing fall.
    public void Kill()
    {
        if (IsDead) return;

        CurrentHealth = 0f;
        UpdateHealthBar();
        Die();
    }

    private void Die()
    {
        Debug.Log($"{name} died.");
        Died?.Invoke();
        gameObject.SetActive(false);
    }

    private void UpdateHealthBar()
    {
        if (healthFill == null || maxHealth <= 0f) return;

        healthFill.fillAmount = Mathf.Clamp01(CurrentHealth / maxHealth);
    }
}
