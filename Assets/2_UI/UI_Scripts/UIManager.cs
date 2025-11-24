using UnityEngine;
using UnityEngine.UI;
using ENDURE;

public class UIManager : MonoBehaviour
{
    // Panels/containers on UI_Canvas prefab
    public PlayerController playerController;

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
        EnableInventoryUI(false);
        EnableCraftingUI(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnableInventoryUI(bool enable)
    {
        panel_Inventory.SetActive(enable);
        EnableUI(enable);
    }

    public void EnableCraftingUI(bool enable)
    {
        panel_CraftingMenu.SetActive(enable);
        EnableUI(enable);
    }

    public void EnableQuestOverviewUI(bool enable)
    {
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
        if (enable) //if ui is enabled, set player state to UI
        {
            playerController.SetState(PlayerController.PlayerState.UI);
        }
        else if (!enable)    //else set player state to Playing
        {
            playerController.SetState(PlayerController.PlayerState.Playing);
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
}
