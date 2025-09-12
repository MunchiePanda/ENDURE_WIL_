using UnityEngine;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    // References to UI elements, assign in inspector
    // panel_Inventory root and its background image
    public GameObject panel_Inventory;          // Root panel
    public Image panel_InventoryBackground;     // Background image on the panel

    // Scroll view container
    public ScrollRect scrollView_Inventory;     // ScrollRect component on the scroll view
    public Image scrollView_Background;         // Optional background image on the scroll view

    // Viewport and content
    public RectTransform viewport;              // Viewport RectTransform
    public Image viewport_Image;                // Viewport Image (for raycast & visuals)
    public Mask viewport_Mask;                  // Viewport Mask to clip children
    public RectTransform content;               // Content RectTransform (items parent)

    // Vertical scrollbar and visuals
    public Scrollbar verticalScrollbar;         // Vertical Scrollbar component
    public Image verticalScrollbar_Image;       // Scrollbar background image
    public RectTransform verticalScrollbar_Handle;   // Handle RectTransform
    public Image verticalScrollbar_HandleImage; // Handle image

    public GameObject itemUIPrefab;
    [SerializeField] private Inventory inventory;    // Source inventory to listen to

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void OnEnable()
    {
        // Subscribe to inventory add events so UI reflects changes
        if (inventory != null)
        {
            inventory.OnItemAdded += AddItem;
        }
    }

    void OnDisable()
    {
        // Unsubscribe to avoid memory leaks and duplicate bindings
        if (inventory != null)
        {
            inventory.OnItemAdded -= AddItem;
        }
    }

    // Add a UI entry when an item is added to the Inventory (listener for Inventory.AddItem)
    // WHY: Keeps UI list in sync by instantiating an item row under the scroll content
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
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
