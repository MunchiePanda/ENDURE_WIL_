using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel_DeathMenu;
    public TMP_Text txt_Title;          // "You Died" or "You Win!"
    public Button btn_Exit;
    public Button btn_Respawn;

    [Header("Settings")]
    [Tooltip("Scene name to load when exiting (e.g., main menu or village).")]
    public string exitSceneName = "Village";
    [Tooltip("Scene name to load when respawning (current scene or checkpoint scene).")]
    public string respawnSceneName = ""; // Empty = reload current scene

    private UIManager uiManager;
    private bool isDead = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (panel_DeathMenu != null)
        {
            panel_DeathMenu.SetActive(false);
        }

        // Get UIManager reference
        uiManager = GetComponentInParent<UIManager>();
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        // Wire up buttons
        if (btn_Respawn != null)
        {
            btn_Respawn.onClick.AddListener(OnRespawnClicked);
        }

        if (btn_Exit != null)
        {
            btn_Exit.onClick.AddListener(OnExitClicked);
        }

        // Set default title if not set
        if (txt_Title != null && string.IsNullOrEmpty(txt_Title.text))
        {
            txt_Title.text = "You Died";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowDeathScreen(string titleText = "You Died")
    {
        if (isDead) return;

        isDead = true;
        Time.timeScale = 0f; // Pause game time

        if (panel_DeathMenu != null)
        {
            panel_DeathMenu.SetActive(true);
        }

        if (txt_Title != null)
        {
            txt_Title.text = titleText;
        }

        if (uiManager != null)
        {
            uiManager.EnableUI(true);
        }

        Debug.Log("Death screen shown");
    }

    void OnExitClicked()
    {
        // Resume time before scene change
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(exitSceneName))
        {
            SceneManager.LoadScene(exitSceneName);
        }
        else
        {
            Debug.LogWarning("DeathUIManager: Exit scene name not set!");
        }
    }

    void OnRespawnClicked()
    {
        // Resume time before scene change
        Time.timeScale = 1f;

        string sceneToLoad = respawnSceneName;
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            // Reload current scene
            sceneToLoad = SceneManager.GetActiveScene().name;
        }

        SceneManager.LoadScene(sceneToLoad);
    }

    private void OnDestroy()
    {
        // Ensure time scale is reset if this object is destroyed
        Time.timeScale = 1f;
    }
}
