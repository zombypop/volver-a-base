using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InGameUIController : MonoBehaviour
{
    [SerializeField] private Transform player;                  // auto-found (PlayerMovement) if left empty
    [SerializeField] private float summitAltitude = 8848.86f;   // altitude shown at the player's starting height (meters)
    [SerializeField] private float metersPerUnit = 1f;          // altitude change per world unit of vertical travel

    [SerializeField] private CinemachineCamera followCamera;    // auto-found if left empty; stops following the player on game over

    [Header("HUD icons")]
    [SerializeField] private Sprite ropeIconSprite;             // optional; falls back to the sprite on the UXML placeholder icons
    [SerializeField] private Sprite axeIconSprite;              // optional; falls back to the sprite on the UXML placeholder icons

    private Label altitudeLabel;
    private Label healthLabel;
    private VisualElement gameOverScreen;
    private VisualElement ropeContainer;
    private VisualElement axeContainer;
    private List<VisualElement> ropeIcons = new List<VisualElement>();
    private List<VisualElement> axeIcons = new List<VisualElement>();
    private PlayerMovement movement;
    private PlayerHealth health;
    private float startY;
    private float lastShownAltitude = float.NaN;
    private int lastShownHealth = -1;
    private int lastShownAxes = -1;
    private float lastShownAxeLife = float.NaN;

    void OnEnable()
    {
        // The UI tree is (re)built whenever the UIDocument enables, so look the labels up here.
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        altitudeLabel = root.Q<Label>("altitude-label");
        healthLabel = root.Q<Label>("Health");
        gameOverScreen = root.Q<VisualElement>("GameOverScreen");

        // Rope / axe icons are generated at runtime, one per item, so the pack is uncapped. Grab
        // the sprite from the UXML placeholders (unless one is assigned in the Inspector), then
        // clear the containers — we rebuild their contents to match the live counts.
        ropeContainer = root.Q<VisualElement>("rope-icons");
        axeContainer = root.Q<VisualElement>("ice-axe-icons");

        if (ropeIconSprite == null) ropeIconSprite = GrabSprite(ropeContainer);
        if (axeIconSprite == null) axeIconSprite = GrabSprite(axeContainer);

        ropeContainer?.Clear();
        axeContainer?.Clear();
        ropeIcons.Clear();
        axeIcons.Clear();

        // Restart button reloads the current scene from scratch.
        Button restartButton = root.Q<Button>("RestartButton");
        if (restartButton != null) restartButton.clicked += RestartScene;

        lastShownAltitude = float.NaN;
        lastShownHealth = -1;
        lastShownAxes = -1;
        lastShownAxeLife = float.NaN;

        // Hidden until the player dies (also re-applied if the UI gets rebuilt mid-game).
        SetGameOverVisible(health != null && health.IsDead);

        // Re-sync the icons to the live counts in case the UI tree was rebuilt mid-game.
        if (movement != null)
        {
            UpdateRopeIcons(movement.RopesRemaining);
            UpdateAxes();
        }
    }

    void OnDestroy()
    {
        if (health != null) health.Died -= OnPlayerDied;
        if (movement != null) movement.RopesChanged -= UpdateRopeIcons;
    }

    // Grab the sprite off the first Image placeholder in a container, so generated icons reuse
    // whatever art the UXML wired up. Returns null if there's no placeholder to copy from.
    private static Sprite GrabSprite(VisualElement container)
    {
        Image img = container?.Children().OfType<Image>().FirstOrDefault();
        return img != null ? img.sprite : null;
    }

    // Grow or shrink a container's icon list so it holds exactly `count` images.
    private static void EnsureIconCount(VisualElement container, List<VisualElement> icons, Sprite sprite, int count)
    {
        if (container == null) return;
        count = Mathf.Max(0, count);

        while (icons.Count < count)
        {
            Image img = new Image { sprite = sprite };
            container.Add(img);
            icons.Add(img);
        }
        while (icons.Count > count)
        {
            int last = icons.Count - 1;
            container.Remove(icons[last]);
            icons.RemoveAt(last);
        }
    }

    // One icon per rope still in the pack (uncapped) — spent ropes drop off the right.
    private void UpdateRopeIcons(int remaining)
    {
        EnsureIconCount(ropeContainer, ropeIcons, ropeIconSprite, remaining);
    }

    // One icon per axe in the pack (uncapped); the current axe (rightmost) fades with its
    // remaining life while the spares stay full.
    private void UpdateAxeIcons(int remaining, float currentLife)
    {
        EnsureIconCount(axeContainer, axeIcons, axeIconSprite, remaining);
        for (int i = 0; i < axeIcons.Count; i++)
        {
            float opacity = (i == axeIcons.Count - 1) ? Mathf.Clamp01(currentLife) : 1f;
            axeIcons[i].style.opacity = opacity;
        }
    }

    private void RestartScene()
    {
        // Time may have been paused on the Game Over screen; make sure it's running again.
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    private void SetGameOverVisible(bool visible)
    {
        if (gameOverScreen == null) return;

        gameOverScreen.SetEnabled(visible);
        gameOverScreen.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnPlayerDied()
    {
        SetGameOverVisible(true);

        // Clearing the tracking target leaves the camera where it is instead of following the body.
        if (followCamera != null) followCamera.Target.TrackingTarget = null;
    }

    void Start()
    {
        if (movement == null) movement = FindFirstObjectByType<PlayerMovement>();
        if (player == null && movement != null) player = movement.transform;

        if (player != null)
        {
            // The player starts at the summit, so that's where the full altitude is shown.
            startY = player.position.y;
            health = player.GetComponent<PlayerHealth>();
        }

        if (movement != null)
        {
            movement.RopesChanged += UpdateRopeIcons;
            UpdateRopeIcons(movement.RopesRemaining);
        }

        if (followCamera == null) followCamera = FindFirstObjectByType<CinemachineCamera>();

        if (health != null)
        {
            health.Died += OnPlayerDied;
            SetGameOverVisible(health.IsDead);
        }
    }

    void Update()
    {
        UpdateAltitude();
        UpdateHealth();
        UpdateAxes();
    }

    // Axe life drains continuously while arresting, so poll it each frame (like health/altitude)
    // and only restyle the icons when the count or the current axe's life actually changes.
    private void UpdateAxes()
    {
        if (movement == null || axeIcons.Count == 0) return;

        int remaining = movement.AxesRemaining;
        float life = movement.CurrentAxeLife;

        // Round the life so sub-1% wobble doesn't restyle every frame.
        float roundedLife = Mathf.Round(life * 100f) / 100f;
        if (remaining == lastShownAxes && roundedLife == lastShownAxeLife) return;
        lastShownAxes = remaining;
        lastShownAxeLife = roundedLife;

        UpdateAxeIcons(remaining, life);
    }

    private void UpdateHealth()
    {
        if (healthLabel == null || health == null) return;

        // Round up so the label only reads 0 once the player is actually dead.
        int shown = Mathf.CeilToInt(health.CurrentHealth);
        if (shown == lastShownHealth) return;
        lastShownHealth = shown;

        healthLabel.text = shown.ToString(CultureInfo.InvariantCulture);
    }

    private void UpdateAltitude()
    {
        if (altitudeLabel == null || player == null) return;

        float descended = (startY - player.position.y) * metersPerUnit;
        float altitude = Mathf.Max(0f, summitAltitude - descended);

        // Only touch the label when the displayed value (2 decimals) actually changes.
        float rounded = Mathf.Round(altitude * 100f) / 100f;
        if (rounded == lastShownAltitude) return;
        lastShownAltitude = rounded;

        altitudeLabel.text = rounded.ToString("N2", CultureInfo.InvariantCulture) + " meters";
    }
}
