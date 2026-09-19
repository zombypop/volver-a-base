using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invulnerabilityDuration = 0.5f; // ignore further hits right after one lands

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    public event System.Action Died;

    private float invulnerableUntil = -1f;

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f || Time.time < invulnerableUntil) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        invulnerableUntil = Time.time + invulnerabilityDuration;

        Debug.Log($"{name} took {amount:F1} damage — health now {CurrentHealth:F1}/{maxHealth:F1}");

        if (IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died.");
        Died?.Invoke();
        gameObject.SetActive(false);
    }
}
