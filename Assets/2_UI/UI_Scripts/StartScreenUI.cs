using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreenUI : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Name of the tutorial scene to load when starting a new game.")]
    public string tutorialSceneName = "TutorialScene";

    [Tooltip("If true, load the tutorial scene by build index instead of name.")]
    public bool useBuildIndex = false;

    [Tooltip("Build index of the tutorial scene (used if Use Build Index is true).")]
    public int tutorialSceneBuildIndex = 1;

    [Header("Scene Loader")]
    [Tooltip("Optional: reference to a SceneLoader component. If null, a new SceneLoader will be created at runtime.")]
    public SceneLoader sceneLoader;

    private void Awake()
    {
        if (sceneLoader == null)
        {
            sceneLoader = SceneLoader.Instance;
        }

        if (sceneLoader == null)
        {
            GameObject loaderObj = new GameObject("SceneLoader");
            sceneLoader = loaderObj.AddComponent<SceneLoader>();
        }
    }

    /// <summary>
    /// Called by the Start button to load the tutorial scene.
    /// </summary>
    public void StartGame()
    {
        if (sceneLoader == null)
        {
            Debug.LogError("StartScreenUI: SceneLoader is missing, cannot start game.");
            return;
        }

        if (useBuildIndex)
        {
            if (tutorialSceneBuildIndex < 0)
            {
                Debug.LogError("StartScreenUI: Invalid tutorial scene build index.");
                return;
            }

            sceneLoader.LoadScene(tutorialSceneBuildIndex);
        }
        else
        {
            if (string.IsNullOrEmpty(tutorialSceneName))
            {
                Debug.LogError("StartScreenUI: Tutorial scene name is empty.");
                return;
            }

            sceneLoader.LoadScene(tutorialSceneName);
        }
    }

    /// <summary>
    /// Called by the Exit/Quit button to close the application.
    /// </summary>
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

