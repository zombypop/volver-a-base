using UnityEngine;

/// <summary>
/// Game-wide settings and debug toggles. Put this on a single "GameManager" object in the
/// scene; it's reachable from anywhere via <see cref="Instance"/>.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Testing")]
    [Tooltip("When on, ropes and axes never run out — rappels cost nothing and axes never wear. For level testing.")]
    [SerializeField] private bool unlimitedSupplies = false;
    [Tooltip("When on, the player takes no damage and can't die. For level testing.")]
    [SerializeField] private bool invincible = false;

    /// <summary>Ropes/axes are free and never deplete (test mode).</summary>
    public bool UnlimitedSupplies => unlimitedSupplies;

    /// <summary>The player takes no damage and can't die (test mode).</summary>
    public bool Invincible => invincible;

    /// <summary>Convenience null-safe check for callers that may run without a GameManager in the scene.</summary>
    public static bool HasUnlimitedSupplies => Instance != null && Instance.unlimitedSupplies;

    /// <summary>Convenience null-safe check for callers that may run without a GameManager in the scene.</summary>
    public static bool IsInvincible => Instance != null && Instance.invincible;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
