using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUIManager : MonoBehaviour
{
    // References to UI elements, assign in inspector
    public GameObject panel_Inventory;          // Root panel
    public RectTransform content;               // Content RectTransform (items parent)
    public Button btn_CloseInventory;           // Close button for inventory panel
    
    public GameObject itemUIPrefab;            // Prefab for individual item UI
    [SerializeField] private Inventory inventory;    // Source inventory to listen to
    // Lookup: map each ItemBase to its instantiated ItemUIManager for fast update/remove without searching children
    private readonly Dictionary<ItemBase, ItemUIManager> ItemUiMap = new Dictionary<ItemBase, ItemUIManager>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Wire up the close button to hide the inventory panel
        if (btn_CloseInventory != null)
        {
            btn_CloseInventory.onClick.AddListener(CloseInventory);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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
            inventory.OnItemAdded -= AddItem;
            inventory.OnItemRemoved -= RemoveItem;
            inventory.OnItemQuantityChanged -= ChangeItemQuantity;
        }
    }

    // Add a UI entry when an item is added to the Inventory (listener for Inventory.AddItem)
    public void AddItem(ItemBase item, int quantity)
    {
        if (itemUIPrefab == null || content == null || item == null)
        {
            Debug.LogWarning("InventoryUIManager.AddItem(): Missing references or item is null.");
            return;
        }

        // Instantiate the UI prefab as a child of the content container
        GameObject itemUiObject = Instantiate(itemUIPrefab, content);

        // If the prefab has an ItemUIManager, populate its fields
        ItemUIManager itemUiManager = itemUiObject.GetComponent<ItemUIManager>();
        if (itemUiManager != null)
        {
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
        if (panel_Inventory != null)
        {
            panel_Inventory.SetActive(false);
            Debug.Log("InventoryUIManager CloseInventory(): Inventory panel closed (D2, D5)");
        }
    }

    // Open the inventory panel (useful for external calls)
    public void OpenInventory()
    {
        if (panel_Inventory != null)
        {
            panel_Inventory.SetActive(true);
            Debug.Log("InventoryUIManager OpenInventory(): Inventory panel opened (D2, D5)");
        }
    }
}
