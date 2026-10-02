using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Drives the main-menu screen (MainMen.uxml): "Play" loads the gameplay scene and "Quit"
/// exits the application. Put this on the GameObject holding the menu's UIDocument.
///
/// Looks the buttons up by exact name, so they must stay "PlayButton" and "QuitButton" in
/// the UXML (same name-matching convention as InGameUIController).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "FirstScene"; // scene Play loads; must be in Build Settings

    private Button playButton;
    private Button quitButton;

    void OnEnable()
    {
        // The UI tree is (re)built whenever the UIDocument enables, so wire the buttons up here.
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        playButton = root.Q<Button>("PlayButton");
        quitButton = root.Q<Button>("QuitButton");

        if (playButton != null) playButton.clicked += PlayGame;
        if (quitButton != null) quitButton.clicked += QuitGame;
    }

    void OnDisable()
    {
        if (playButton != null) playButton.clicked -= PlayGame;
        if (quitButton != null) quitButton.clicked -= QuitGame;
    }

    private void PlayGame()
    {
        // A menu scene may have paused time; make sure gameplay starts running.
        Time.timeScale = 1f;

        // Transition through the fader if one is present; otherwise load straight away.
        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene(gameSceneName);
        else
            SceneManager.LoadScene(gameSceneName);
    }

    private void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        // Application.Quit is a no-op in the editor, so stop play mode there instead.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
