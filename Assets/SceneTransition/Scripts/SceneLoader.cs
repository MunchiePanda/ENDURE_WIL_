using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton manager for async scene loading with loading screen support.
/// Works independently and can be used from anywhere in the project.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Loading Screen")]
    [Tooltip("Reference to the in-scene LoadingScreenUI controller (auto-found if not set).")]
    public LoadingScreenUI loadingScreenUI;

    [TextArea]
    [Tooltip("Lore or helper text to display while loading when no dynamic text is provided.")]
    public string defaultLoreText = "Traversing to the next area...";

    private bool isLoading;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        if (!CanStartLoading()) return;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneLoader: Scene name is null or empty!");
            return;
        }

        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public void LoadScene(int sceneBuildIndex)
    {
        if (!CanStartLoading()) return;
        if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"SceneLoader: Invalid scene build index: {sceneBuildIndex}");
            return;
        }

        StartCoroutine(LoadSceneAsync(sceneBuildIndex));
    }

    bool CanStartLoading()
    {
        if (isLoading)
        {
            Debug.LogWarning("SceneLoader: Already loading a scene. Ignoring request.");
            return false;
        }
        return true;
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        isLoading = true;
        ShowLoadingScreen();
        yield return null;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        yield return RunLoadLoop(asyncLoad);

        HideLoadingScreen();
        isLoading = false;
    }

    IEnumerator LoadSceneAsync(int sceneBuildIndex)
    {
        isLoading = true;
        ShowLoadingScreen();
        yield return null;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneBuildIndex);
        asyncLoad.allowSceneActivation = false;

        yield return RunLoadLoop(asyncLoad);

        HideLoadingScreen();
        isLoading = false;
    }

    IEnumerator RunLoadLoop(AsyncOperation asyncLoad)
    {
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            UpdateLoadingProgress(progress);

            if (asyncLoad.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.1f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        yield return null;
    }

    void ShowLoadingScreen()
    {
        var ui = EnsureLoadingScreenUI();
        if (ui == null) return;
        ui.Show();
        if (!string.IsNullOrWhiteSpace(defaultLoreText))
        {
            ui.SetLore(defaultLoreText);
        }
    }

    void UpdateLoadingProgress(float progress)
    {
        loadingScreenUI?.UpdateProgress(progress);
    }

    void HideLoadingScreen()
    {
        loadingScreenUI?.Hide();
    }

    LoadingScreenUI EnsureLoadingScreenUI()
    {
        if (loadingScreenUI != null) return loadingScreenUI;

#if UNITY_2023_1_OR_NEWER
        loadingScreenUI = Object.FindFirstObjectByType<LoadingScreenUI>(FindObjectsInactive.Include);
#else
        loadingScreenUI = FindObjectOfType<LoadingScreenUI>(true);
#endif

        if (loadingScreenUI == null)
        {
            Debug.LogWarning("SceneLoader: No LoadingScreenUI found in the scene. Loading will proceed without visual feedback.");
        }

        return loadingScreenUI;
    }

    public bool IsLoading()
    {
        return isLoading;
    }
}
