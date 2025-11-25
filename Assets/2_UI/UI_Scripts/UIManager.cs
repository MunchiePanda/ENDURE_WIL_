using UnityEngine;
using UnityEngine.UI;
using ENDURE;

public class UIManager : MonoBehaviour
{
    // Panels/containers on UI_Canvas prefab
    public PlayerController playerController;

    // UI Audio
    [Header("Audio")]
    [Tooltip("AudioSource used to play UI click sounds. If not assigned, one will be created.")]
    public AudioSource uiAudioSource;
    [Tooltip("Audio clip to play on UI button click (e.g., 'Click_Mid-High').")]
    public AudioClip clickMidHighClip;
    [Tooltip("Audio clip to play when a UI panel is opened/enabled.")]
    public AudioClip panelOpenClip;
    [Tooltip("Audio clip to play when a UI panel is closed/disabled.")]
    public AudioClip panelCloseClip;
    [Tooltip("Audio clip to play when the inventory panel is opened/enabled.")]
    public AudioClip inventoryOpenClip;
    [Tooltip("Audio clip to play when the inventory panel is closed/disabled.")]
    public AudioClip inventoryCloseClip;
    [Tooltip("Audio clip to play when an item is added (e.g., crafting success).")]
    public AudioClip addItemSound;
    [Tooltip("Audio clip to play when an interactable item is picked up.")]
    public AudioClip itemPickupSound;

    public RectTransform group_PlayerStats;     // Container holding player stat sliders
    public PlayerUIManager playerUIManager;
    public GameObject panel_Inventory;          // Inventory panel root
    public InventoryUIManager inventoryUIManager;
    public GameObject panel_CraftingMenu;          // Crafting panel root
    public CraftingUIManager craftingUIManager;
    public GameObject panel_QuestOverviewUI;        //Quest overview
    public QuestOverviewUIManager questOverviewUIManager;
    public GameObject panel_QuestgiverUI;       //questgiver
    public QuestGiverUIManager questgiverUIManager;
    public PauseUIManager pauseUIManager;
    public DeathUIManager deathUIManager;

    // @Mik $ PlayerSystems - impliment the toggle UI visibilty for inventory and keybinds

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        if(playerController == null)
        {
            Debug.LogWarning("UIManager Start(): No PlayerController found in parent hierarchy (D2, D6)");
        }
        else
        {
            if (playerUIManager == null)
            {
                playerUIManager = GetComponentInChildren<PlayerUIManager>(true);
            }

            var playerManager = playerController.GetComponent<PlayerManager>();
            if (playerUIManager != null)
            {
                playerUIManager.SetPlayerManager(playerManager);
            }
            else
            {
                Debug.LogWarning("UIManager Start(): PlayerUIManager not found.");
            }
        }

        // Ensure there is an AudioSource to play UI sounds
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.playOnAwake = false;
            }
        }
        EnableInventoryUI(false);
        EnableCraftingUI(false);

        // Find pause and death UI managers if not assigned
        if (pauseUIManager == null)
        {
            pauseUIManager = GetComponentInChildren<PauseUIManager>(true);
        }
        if (deathUIManager == null)
        {
            deathUIManager = GetComponentInChildren<DeathUIManager>(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnableInventoryUI(bool enable)
    {
        if (panel_Inventory != null && panel_Inventory.activeSelf != enable)
        {
            if (enable)
            {
                PlayInventoryOpen();
            }
            else
            {
                PlayInventoryClose();
            }
        }
        panel_Inventory.SetActive(enable);
        EnableUI(enable);
    }

    public void EnableCraftingUI(bool enable)
    {
        if (panel_CraftingMenu != null && panel_CraftingMenu.activeSelf != enable)
        {
            if (enable)
            {
                PlayPanelOpen();
            }
            else
            {
                PlayPanelClose();
            }
        }
        panel_CraftingMenu.SetActive(enable);
        EnableUI(enable);
    }

    public void EnableQuestOverviewUI(bool enable)
    {
        if (panel_QuestOverviewUI != null && panel_QuestOverviewUI.activeSelf != enable)
        {
            if (enable)
            {
                PlayPanelOpen();
            }
            else
            {
                PlayPanelClose();
            }
        }
        if (panel_QuestOverviewUI != null)
        {
            panel_QuestOverviewUI.SetActive(enable);
        }

        EnableUI(enable);
    }

    public void ToggleQuestOverviewUI()
    {
        questOverviewUIManager.ToggleQuestOverviewUI();
    }

    public void EnableUI(bool enable)
    {
        Cursor.visible = enable;
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
        
        // Only set player state if playerController exists
        if (playerController != null)
        {
            if (enable) //if ui is enabled, set player state to UI
            {
                playerController.SetState(PlayerController.PlayerState.UI);
            }
            else //else set player state to Playing
            {
                playerController.SetState(PlayerController.PlayerState.Playing);
            }
        }
        else if (enable == false)
        {
            // If disabling UI but no playerController, try to find it
            if (this.playerController == null)
            {
                this.playerController = GetComponentInParent<PlayerController>();
                if (this.playerController == null)
                {
                    this.playerController = FindObjectOfType<PlayerController>(true);
                }
            }
            
            if (this.playerController != null)
            {
                this.playerController.SetState(PlayerController.PlayerState.Playing);
            }
        }
    }

    // Generic screen management (for UIScreenInteractable)
    public GameObject OpenScreen(GameObject screenPrefab, bool toggle, bool instantiateIfMissing = false)
    {
        if (screenPrefab == null) return null;

        //find screen instance in the UI hierarchy, if not found, instantiate it
        var instance = FindScreenInstance(screenPrefab);
        if (instance == null && instantiateIfMissing)
        {
            instance = Instantiate(screenPrefab, transform);
            instance.name = screenPrefab.name;
        }

        if (instance != null)
        {
            bool targetActive = true;
            if (toggle) //if toggle is true, if the screen is active, it gets deactivated; if inactive, it gets activated.
            {
                targetActive = !instance.activeSelf;    //invert the active state of the screen
                instance.SetActive(targetActive);
            }
            else //if toggle is false, the screen is always activated
            {
                targetActive = true;
                instance.SetActive(true);
            }

            // When opening/clsing a UI screen, enable mouse cursor for UI interaction
            EnableUI(targetActive);
            
        }

        return instance;
    }

    //find the screen instance in the UI hierarchy using the name of the screen prefab
    public GameObject FindScreenInstance(GameObject screenPrefab)
    {
        if (screenPrefab == null) return null;
        // Try direct name match at root level
        var t = transform.Find(screenPrefab.name);
        if (t != null) return t.gameObject;

        // Fallback: deep search
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == screenPrefab.name)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    public void PlayClick()
    {
        if (uiAudioSource != null && clickMidHighClip != null)
        {
            uiAudioSource.PlayOneShot(clickMidHighClip);
        }
    }

    public void PlayPanelOpen()
    {
        if (uiAudioSource != null && panelOpenClip != null)
        {
            uiAudioSource.PlayOneShot(panelOpenClip);
        }
    }

    public void PlayPanelClose()
    {
        if (uiAudioSource != null && panelCloseClip != null)
        {
            uiAudioSource.PlayOneShot(panelCloseClip);
        }
    }

    public void PlayAddItemSound()
    {
        if (uiAudioSource != null && addItemSound != null)
        {
            uiAudioSource.PlayOneShot(addItemSound);
        }
    }

    public void PlayInventoryOpen()
    {
        if (uiAudioSource != null && inventoryOpenClip != null)
        {
            uiAudioSource.PlayOneShot(inventoryOpenClip);
        }
    }

    public void PlayInventoryClose()
    {
        if (uiAudioSource != null && inventoryCloseClip != null)
        {
            uiAudioSource.PlayOneShot(inventoryCloseClip);
        }
    }

    public void PlayItemPickupSound()
    {
        if (uiAudioSource != null && itemPickupSound != null)
        {
            uiAudioSource.PlayOneShot(itemPickupSound);
        }
    }
}
