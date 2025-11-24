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
    [Tooltip("Prefab containing the loading screen UI (should have a Slider and TextMeshProUGUI)")]
    public GameObject loadingScreenPrefab;
    
    private GameObject loadingScreenInstance;
    private UnityEngine.UI.Slider progressBar;
    private TMPro.TextMeshProUGUI loadingText;
    private bool isLoading = false;

    private void Awake()
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

    /// <summary>
    /// Load a scene asynchronously with a loading screen by scene name
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("SceneLoader: Already loading a scene. Ignoring request.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneLoader: Scene name is null or empty!");
            return;
        }

        StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// Load a scene asynchronously with a loading screen by build index
    /// </summary>
    public void LoadScene(int sceneBuildIndex)
    {
        if (isLoading)
        {
            Debug.LogWarning("SceneLoader: Already loading a scene. Ignoring request.");
            return;
        }

        if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"SceneLoader: Invalid scene build index: {sceneBuildIndex}");
            return;
        }

        StartCoroutine(LoadSceneAsync(sceneBuildIndex));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        isLoading = true;
        ShowLoadingScreen();

        // Wait a frame to ensure loading screen is visible
        yield return null;

        // Start loading the scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Update progress while loading
        while (!asyncLoad.isDone)
        {
            // Unity's progress goes from 0-0.9, then jumps to 1 when ready
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            UpdateLoadingProgress(progress);

            // When loading is complete (0.9), allow scene activation
            if (asyncLoad.progress >= 0.9f)
            {
                // Small delay to show 100% before switching
                yield return new WaitForSeconds(0.1f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // Wait one more frame to ensure scene is fully loaded
        yield return null;

        HideLoadingScreen();
        isLoading = false;
    }

    private IEnumerator LoadSceneAsync(int sceneBuildIndex)
    {
        isLoading = true;
        ShowLoadingScreen();

        // Wait a frame to ensure loading screen is visible
        yield return null;

        // Start loading the scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneBuildIndex);
        asyncLoad.allowSceneActivation = false;

        // Update progress while loading
        while (!asyncLoad.isDone)
        {
            // Unity's progress goes from 0-0.9, then jumps to 1 when ready
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            UpdateLoadingProgress(progress);

            // When loading is complete (0.9), allow scene activation
            if (asyncLoad.progress >= 0.9f)
            {
                // Small delay to show 100% before switching
                yield return new WaitForSeconds(0.1f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // Wait one more frame to ensure scene is fully loaded
        yield return null;

        HideLoadingScreen();
        isLoading = false;
    }

    private void ShowLoadingScreen()
    {
        if (loadingScreenPrefab == null)
        {
            Debug.LogWarning("SceneLoader: No loading screen prefab assigned. Loading without visual feedback.");
            return;
        }

        if (loadingScreenInstance != null)
        {
            Destroy(loadingScreenInstance);
        }

        // Find or create a canvas for the loading screen
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("LoadingScreenCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // Ensure it's on top
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        loadingScreenInstance = Instantiate(loadingScreenPrefab, canvas.transform);
        loadingScreenInstance.name = "LoadingScreen";

        // Find progress bar and text components
        progressBar = loadingScreenInstance.GetComponentInChildren<UnityEngine.UI.Slider>();
        loadingText = loadingScreenInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
        if (loadingText != null)
        {
            loadingText.text = "Loading...";
        }
    }

    private void UpdateLoadingProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
        if (loadingText != null)
        {
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
        }
    }

    private void HideLoadingScreen()
    {
        if (loadingScreenInstance != null)
        {
            Destroy(loadingScreenInstance);
            loadingScreenInstance = null;
            progressBar = null;
            loadingText = null;
        }
    }

    /// <summary>
    /// Check if a scene is currently being loaded
    /// </summary>
    public bool IsLoading()
    {
        return isLoading;
    }
}


