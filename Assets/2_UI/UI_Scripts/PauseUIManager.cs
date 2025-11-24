using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel_PauseMenu;
    public Button btn_Exit;
    public Button btn_Resume;

    [Header("Settings")]
    [Tooltip("Scene name to load when exiting (e.g., main menu or village).")]
    public string exitSceneName = "Village";

    private UIManager uiManager;
    private bool isPaused = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (panel_PauseMenu != null)
        {
            panel_PauseMenu.SetActive(false);
        }

        // Get UIManager reference
        uiManager = GetComponentInParent<UIManager>();
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        // Wire up buttons
        if (btn_Resume != null)
        {
            btn_Resume.onClick.AddListener(OnResumeClicked);
        }

        if (btn_Exit != null)
        {
            btn_Exit.onClick.AddListener(OnExitClicked);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Toggle pause with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        // Don't allow pausing if other UI is open (inventory, crafting, etc.)
        if (uiManager != null)
        {
            if (uiManager.panel_Inventory != null && uiManager.panel_Inventory.activeSelf)
            {
                return; // Can't pause while inventory is open
            }
            if (uiManager.panel_CraftingMenu != null && uiManager.panel_CraftingMenu.activeSelf)
            {
                return; // Can't pause while crafting is open
            }
            if (uiManager.panel_QuestOverviewUI != null && uiManager.panel_QuestOverviewUI.activeSelf)
            {
                return; // Can't pause while quest overview is open
            }
        }

        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f; // Pause game time

        if (panel_PauseMenu != null)
        {
            panel_PauseMenu.SetActive(true);
        }

        if (uiManager != null)
        {
            uiManager.EnableUI(true);
        }

        Debug.Log("Game Paused");
    }

    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f; // Resume game time

        if (panel_PauseMenu != null)
        {
            panel_PauseMenu.SetActive(false);
        }

        if (uiManager != null)
        {
            uiManager.EnableUI(false);
        }

        Debug.Log("Game Resumed");
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
            Debug.LogWarning("PauseUIManager: Exit scene name not set!");
        }
    }

    void OnResumeClicked()
    {
        Resume();
    }

    private void OnDestroy()
    {
        // Ensure time scale is reset if this object is destroyed
        Time.timeScale = 1f;
    }
}
