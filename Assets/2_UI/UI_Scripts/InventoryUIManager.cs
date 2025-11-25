using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ENDURE;

public class InventoryUIManager : MonoBehaviour
{
    // References to UI elements, assign in inspector
    public GameObject panel_Inventory;          // Root panel
    public RectTransform content;               // Content RectTransform (items parent)
    public Button btn_CloseInventory;           // Close button for inventory panel
    public UIManager uiManager;               // Reference to UIManager for enabling/disabling panels

    public GameObject itemUIPrefab;            // Prefab for individual item UI
    [SerializeField] private Inventory inventory;    // Source inventory to listen to
    [SerializeField] private PlayerManager playerManager;
    // Lookup: map each ItemBase to its instantiated ItemUIManager for fast update/remove without searching children
    private readonly Dictionary<ItemBase, ItemUIManager> ItemUiMap = new Dictionary<ItemBase, ItemUIManager>();

    void Awake()
    {
        // Get Inventory from the attached player if not assigned
        if (inventory == null)
        {
            inventory = GetComponentInParent<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("InventoryUIManager Awake(): No player inventory found (D2, D6)");
                return;
            }
        }

        if (playerManager == null)
        {
            playerManager = GetComponentInParent<PlayerManager>();
        }

        // Subscribe to inventory events (happens even if panel is disabled)
        if (inventory != null)
        {
            inventory.OnItemAdded += AddItem;
            inventory.OnItemRemoved += RemoveItem;
            inventory.OnItemQuantityChanged += ChangeItemQuantity;
            Debug.Log($"InventoryUIManager subscribed to inventory on {inventory.gameObject.name}");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Wire up the close button
        if (btn_CloseInventory != null)
        {
            btn_CloseInventory.onClick.AddListener(CloseInventory);
        }

        // Get UIManager reference
        if (uiManager == null)
        {
            uiManager = GetComponentInParent<UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("InventoryUIManager Start(): No UIManager found in parent hierarchy (D2, D5)");
            }
        }

        if (playerManager == null && uiManager != null && uiManager.playerController != null)
        {
            playerManager = uiManager.playerController.GetComponent<PlayerManager>();
        }

        // Debug verification
        if (inventory != null)
        {
            Debug.Log($"InventoryUIManager Start(): Connected to inventory with {inventory.CurrentItemCount} items");
            
            // Refresh UI with existing items from persistent inventory (important for scene transitions)
            RefreshUIFromInventory();
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    /*
    void OnEnable()
    {
        // Subscribe to inventory add events so UI reflects changes
        if (inventory != null)
        {
            inventory.OnItemAdded += AddItem;
            inventory.OnItemRemoved += RemoveItem;
            inventory.OnItemQuantityChanged += ChangeItemQuantity;
        }
    }

    void OnDisable()
    {
        // Unsubscribe to avoid memory leaks and duplicate bindings
        if (inventory != null)
        {
            //inventory.OnItemAdded -= AddItem;
            //inventory.OnItemRemoved -= RemoveItem;
            //inventory.OnItemQuantityChanged -= ChangeItemQuantity;
        }
    }
    */

    // Add a UI entry when an item is added to the Inventory (listener for Inventory.AddItem)
    public void AddItem(ItemBase item, int quantity)
    {
        if (itemUIPrefab == null || content == null || item == null)
        {
            Debug.LogWarning("InventoryUIManager.AddItem(): Missing references or item is null.");
            return;
        }

        // Check if item already exists in UI - if so, just update quantity
        if (ItemUiMap.TryGetValue(item, out ItemUIManager existingManager) && existingManager != null)
        {
            existingManager.UpdateItemUI(item, quantity);
            return;
        }

        // Instantiate the UI prefab as a child of the content container
        GameObject itemUiObject = Instantiate(itemUIPrefab, content);

        // If the prefab has an ItemUIManager, populate its fields
        ItemUIManager itemUiManager = itemUiObject.GetComponent<ItemUIManager>();
        if (itemUiManager != null)
        {
            itemUiManager.Initialize(this);
            itemUiManager.UpdateItemUI(item, quantity);
            ItemUiMap[item] = itemUiManager;
        }
    }

    // Remove the item's UI entry when it is removed from the inventory
    public void RemoveItem(ItemBase item, int oldQuantity)
    {
        if (item == null) return;

        if (ItemUiMap.TryGetValue(item, out ItemUIManager itemUiManager) && itemUiManager != null)
        {
            Destroy(itemUiManager.gameObject);
            ItemUiMap.Remove(item);
        }
    }

    // Update the quantity display when an item's quantity changes
    public void ChangeItemQuantity(ItemBase item, int oldQuantity, int newQuantity)
    {
        if (item == null) return;

        if (newQuantity <= 0) // if Quantity dropped to zero or below; remove the UI entry
        {
            RemoveItem(item, oldQuantity);
            return;
        }

        if (ItemUiMap.TryGetValue(item, out ItemUIManager itemUiManager) && itemUiManager != null)
        {
            itemUiManager.UpdateItemUI(item, newQuantity);
        }
    }

    // Close the inventory panel when close button is clicked
    public void CloseInventory()
    {
        if (uiManager != null) uiManager.PlayClick();
        uiManager.EnableInventoryUI(false);
    }

    // Open the inventory panel (useful for external calls)
    public void OpenInventory()
    {
        if (uiManager != null) uiManager.PlayClick();
        uiManager.EnableInventoryUI(true);
    }

    /// <summary>
    /// Refreshes the UI to show all items currently in the inventory.
    /// Important for scene transitions where items persist but UI needs to be rebuilt.
    /// </summary>
    private void RefreshUIFromInventory()
    {
        if (inventory == null || itemUIPrefab == null || content == null)
        {
            return;
        }

        // Clear existing UI items
        foreach (var kvp in ItemUiMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        ItemUiMap.Clear();

        // Get all items from inventory and create UI for each
        var allItems = inventory.GetAllItems();
        foreach (var itemEntry in allItems)
        {
            if (itemEntry.Key != null && itemEntry.Value > 0)
            {
                AddItem(itemEntry.Key, itemEntry.Value);
            }
        }

        Debug.Log($"InventoryUIManager: Refreshed UI with {allItems.Count} items from inventory.");
    }

    public Inventory Inventory => inventory;
    public PlayerManager PlayerManager => playerManager;
}
