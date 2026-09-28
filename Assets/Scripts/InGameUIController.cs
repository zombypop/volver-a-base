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

    private Label altitudeLabel;
    private Label healthLabel;
    private VisualElement gameOverScreen;
    private List<VisualElement> ropeIcons = new List<VisualElement>();
    private PlayerMovement movement;
    private PlayerHealth health;
    private float startY;
    private float lastShownAltitude = float.NaN;
    private int lastShownHealth = -1;

    void OnEnable()
    {
        // The UI tree is (re)built whenever the UIDocument enables, so look the labels up here.
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        altitudeLabel = root.Q<Label>("altitude-label");
        healthLabel = root.Q<Label>("Health");
        gameOverScreen = root.Q<VisualElement>("GameOverScreen");

        // The rope icons are the images inside the "rope-icons" container; we hide them
        // from the right as ropes are spent.
        VisualElement ropeContainer = root.Q<VisualElement>("rope-icons");
        ropeIcons = ropeContainer != null ? ropeContainer.Children().ToList()
                                          : new List<VisualElement>();

        // Restart button reloads the current scene from scratch.
        Button restartButton = root.Q<Button>("RestartButton");
        if (restartButton != null) restartButton.clicked += RestartScene;

        lastShownAltitude = float.NaN;
        lastShownHealth = -1;

        // Hidden until the player dies (also re-applied if the UI gets rebuilt mid-game).
        SetGameOverVisible(health != null && health.IsDead);

        // Re-sync the icons to the live count in case the UI tree was rebuilt mid-game.
        if (movement != null) UpdateRopeIcons(movement.RopesRemaining);
    }

    void OnDestroy()
    {
        if (health != null) health.Died -= OnPlayerDied;
        if (movement != null) movement.RopesChanged -= UpdateRopeIcons;
    }

    // Show one icon per rope still in the pack; spent ropes disappear from the right.
    private void UpdateRopeIcons(int remaining)
    {
        for (int i = 0; i < ropeIcons.Count; i++)
        {
            bool show = i < remaining;
            ropeIcons[i].style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
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
