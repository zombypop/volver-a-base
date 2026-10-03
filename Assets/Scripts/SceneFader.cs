using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// A persistent full-screen black overlay used for scene transitions: fade to black, load the
/// next scene while the screen is covered, then fade back in. Survives scene loads so the same
/// overlay handles both halves of the transition.
///
/// Setup: put this on an empty GameObject in the FIRST scene that triggers a transition (the
/// main menu). A UIDocument is required — assign the project's PanelSettings asset to it. The
/// black overlay itself is built in code, so no UXML is needed. Call it via
/// <see cref="Instance"/>.FadeToScene("SceneName").
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [SerializeField] private float fadeDuration = 0.5f; // seconds for each half of the fade (0 = instant)

    private VisualElement overlay;
    private Coroutine routine;

    void Awake()
    {
        // Singleton: the first fader persists across scenes; later duplicates destroy themselves.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        UIDocument doc = GetComponent<UIDocument>();
        doc.sortingOrder = 1000; // draw on top of every other UIDocument sharing the panel

        VisualElement root = doc.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("SceneFader: no rootVisualElement — assign a PanelSettings asset to the UIDocument.");
            return;
        }

        // This panel sits on top (sortingOrder above), so make sure it never eats clicks meant for
        // the UI beneath it — only the fade overlay should ever draw here.
        root.pickingMode = PickingMode.Ignore;

        // A solid black rectangle covering the whole panel, transparent to start and to clicks.
        overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.top = 0;
        overlay.style.bottom = 0;
        overlay.style.backgroundColor = Color.black;
        overlay.style.opacity = 0f;
        overlay.pickingMode = PickingMode.Ignore; // never swallow input, even if left visible
        root.Add(overlay);
    }

    /// <summary>Fade to black, load <paramref name="sceneName"/>, then fade back in.</summary>
    public void FadeToScene(string sceneName)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        yield return Fade(1f);                                 // cover the screen
        yield return SceneManager.LoadSceneAsync(sceneName);   // swap scenes while hidden
        yield return Fade(0f);                                 // reveal the new scene
        routine = null;
    }

    // Unscaled time so a paused timeScale (menu / game over) doesn't freeze the fade.
    private IEnumerator Fade(float target)
    {
        if (overlay == null) yield break;

        if (fadeDuration <= 0f)
        {
            overlay.style.opacity = target;
            yield break;
        }

        float start = overlay.style.opacity.value;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            overlay.style.opacity = Mathf.Lerp(start, target, elapsed / fadeDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        overlay.style.opacity = target;
    }
}
