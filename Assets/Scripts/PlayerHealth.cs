using System.Collections;
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

    [Header("Hurt flash")]
    [SerializeField] private SpriteRenderer hurtSprite;       // sprite to tint on a hit; auto-found in children if empty
    [SerializeField] private Color hurtColor = new Color(1f, 0.3f, 0.3f); // tint at the peak of the flash
    [SerializeField] private float hurtFlashDuration = 0.3f;  // seconds for the tint to fade back to normal


    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    public event System.Action Died;

    private float invulnerableUntil = -1f;
    private Color baseColor = Color.white;   // the sprite's untinted color, restored after each flash
    private Coroutine hurtFlashRoutine;

    void Awake()
    {
        CurrentHealth = maxHealth;
        UpdateHealthBar();

        if (hurtSprite == null) hurtSprite = GetComponentInChildren<SpriteRenderer>();
        if (hurtSprite != null) baseColor = hurtSprite.color;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f || Time.time < invulnerableUntil) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        invulnerableUntil = Time.time + invulnerabilityDuration;
        UpdateHealthBar();

        // A little sideways camera kick so a hit is felt, not just seen.
        if (impulseSource != null) impulseSource.GenerateImpulse(Vector3.right * hurtShakeForce);

        // Flash the sprite red so the hit reads visually too.
        if (hurtSprite != null)
        {
            if (hurtFlashRoutine != null) StopCoroutine(hurtFlashRoutine);
            hurtFlashRoutine = StartCoroutine(HurtFlash());
        }

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

    // Snap the sprite to hurtColor, then ease it back to its normal color over hurtFlashDuration.
    private IEnumerator HurtFlash()
    {
        float elapsed = 0f;
        while (elapsed < hurtFlashDuration)
        {
            hurtSprite.color = Color.Lerp(hurtColor, baseColor, elapsed / hurtFlashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        hurtSprite.color = baseColor;
        hurtFlashRoutine = null;
    }

    private void UpdateHealthBar()
    {
        if (healthFill == null || maxHealth <= 0f) return;

        healthFill.fillAmount = Mathf.Clamp01(CurrentHealth / maxHealth);
    }
}
