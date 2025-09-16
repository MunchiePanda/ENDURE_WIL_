using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Panels/containers on UI_Canvas prefab
    public RectTransform group_PlayerStats;     // Container holding player stat sliders
    public PlayerUIManager playerUIManager;
    public GameObject panel_Inventory;          // Inventory panel root
    public InventoryUIManager inventoryUIManager;
    public GameObject panel_CraftingMenu;          // Crafting panel root
    public CraftingUIManager craftingUIManager;

    // @Mik $ PlayerSystems - impliment the toggle UI visibilty for inventory and keybinds

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
    }

    public void EnableCraftingUI(bool enable)
    {
        panel_CraftingMenu.SetActive(enable);
    }
}
