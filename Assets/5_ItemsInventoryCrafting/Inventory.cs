using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("If true, uses PersistentInventoryManager to keep items across scenes. If false, uses local storage (items lost on scene change).")]
    [SerializeField] private bool usePersistentInventory = true;
    
    [SerializeField] private int maxInventorySize = 50;
    
    [Header("Inventory Contents (Local - only used if usePersistentInventory is false)")]
    [SerializeField] private Dictionary<ItemBase, int> items = new Dictionary<ItemBase, int>();
    
    private PersistentInventoryManager persistentManager;
    
    // Events for inventory changes (Delegates)
    // @Ang 2 UI - These can be used to update the UI
    public System.Action<ItemBase, int> OnItemAdded;
    public System.Action<ItemBase, int> OnItemRemoved;
    public System.Action<ItemBase, int, int> OnItemQuantityChanged; // item, oldQuantity, newQuantity
    
    // Properties
    public int CurrentItemCount => usePersistentInventory && persistentManager != null 
        ? persistentManager.CurrentItemCount 
        : items.Count;
    public int TotalItemQuantity => usePersistentInventory && persistentManager != null
        ? persistentManager.TotalItemQuantity
        : items.Values.Sum();
    public bool IsFull => usePersistentInventory && persistentManager != null
        ? persistentManager.IsFull
        : TotalItemQuantity >= maxInventorySize;
    
#if UNITY_EDITOR
    // Editor-only read-only view of items for custom inspector display
    public System.Collections.Generic.IReadOnlyDictionary<ItemBase, int> DebugItems => usePersistentInventory && persistentManager != null
        ? persistentManager.DebugItems
        : items;
#endif

    private void Awake()
    {
        if (usePersistentInventory)
        {
            persistentManager = PersistentInventoryManager.Instance;
            
            // Subscribe to persistent manager events and forward them
            persistentManager.OnItemAdded += (item, qty) => OnItemAdded?.Invoke(item, qty);
            persistentManager.OnItemRemoved += (item, qty) => OnItemRemoved?.Invoke(item, qty);
            persistentManager.OnItemQuantityChanged += (item, oldQty, newQty) => OnItemQuantityChanged?.Invoke(item, oldQty, newQty);
            
            Debug.Log($"Inventory: Connected to PersistentInventoryManager. Current items: {persistentManager.CurrentItemCount}, Total quantity: {persistentManager.TotalItemQuantity}");
        }
        else
        {
            Debug.Log("Inventory: Using local storage (items will be lost on scene change).");
        }
    }
    
    // Add items to inventory
    public bool AddItem(ItemBase item, int quantity = 1)
    {
        if (usePersistentInventory && persistentManager != null)
        {
            return persistentManager.AddItem(item, quantity);
        }
        
        // Local inventory logic (fallback)
        if (item == null || quantity <= 0) return false;
        
        // Check if there is space
        if (TotalItemQuantity + quantity > maxInventorySize)
        {
            Debug.LogWarning($"Inventory AddItem(): Cannot add {quantity} {item.itemName}. Inventory would exceed max size of {maxInventorySize}.");
            return false;
        }
        
        //If item is already in inventory, increase quantity
        if (items.ContainsKey(item))
        {
            int oldQuantity = items[item];
            items[item] += quantity;
            OnItemQuantityChanged?.Invoke(item, oldQuantity, items[item]);
        }
        //If item is not in inventory, add item and quantity
        else
        {
            items[item] = quantity;
            OnItemAdded?.Invoke(item, quantity);
        }
        
        return true;
    }
    
    // Remove items from inventory
    public bool RemoveItem(ItemBase item, int quantity = 1)
    {
        if (usePersistentInventory && persistentManager != null)
        {
            return persistentManager.RemoveItem(item, quantity);
        }
        
        // Local inventory logic (fallback)
        if (item == null || quantity <= 0 || !items.ContainsKey(item)) return false;
        
        //If item is in inventory, decrease quantity
        if (items[item] >= quantity)   //Shorthand for: if (items[item].quantity >= quantity) ~Sio
        {
            int oldQuantity = items[item];
            items[item] -= quantity;
            
            //If quantity is less than 0, remove item
            if (items[item] <= 0)
            {
                items.Remove(item);
                OnItemRemoved?.Invoke(item, oldQuantity);
            }
            else    //If quantity is greater than 0, update quantity
            {
                OnItemQuantityChanged?.Invoke(item, oldQuantity, items[item]);
            }
            
            return true;
        }
        
        return false;
    }
    
    // Get quantity of specific item
    public int GetItemQuantity(ItemBase item)
    {
        if (usePersistentInventory && persistentManager != null)
        {
            return persistentManager.GetItemQuantity(item);
        }
        return items.ContainsKey(item) ? items[item] : 0;
    }
    
    // Check if inventory contains item
    public bool HasItem(ItemBase item, int quantity = 1)
    {
        if (usePersistentInventory && persistentManager != null)
        {
            return persistentManager.HasItem(item, quantity);
        }
        return GetItemQuantity(item) >= quantity;
    }
    
    // Get all items as a list (for UI display)
    public List<KeyValuePair<ItemBase, int>> GetAllItems()
    {
        if (usePersistentInventory && persistentManager != null)
        {
            return persistentManager.GetAllItems();
        }
        return items.ToList();
    }

    // Clear entire inventory (use with caution - typically only for new game)
    public void ClearInventory()
    {
        if (usePersistentInventory && persistentManager != null)
        {
            persistentManager.ClearInventory();
        }
        else
        {
            items.Clear();
            OnItemRemoved?.Invoke(null, 0); // Signal complete clear
        }
    }
    
    // Get items by type (filter by ItemType)
    public List<KeyValuePair<ItemBase, int>> GetItemsByType(ItemType type)
    {
        if (usePersistentInventory && persistentManager != null)
        {
            return persistentManager.GetItemsByType(type);
        }
        return items.Where(kvp => kvp.Key.itemType == type).ToList();
    }
    
    // Debug method to print inventory contents
    [ContextMenu("Print Inventory")]
    public void PrintInventory()
    {
        Debug.Log($"Inventory Contents ({CurrentItemCount} unique items, {TotalItemQuantity} total):");
        foreach (var kvp in items)
        {
            Debug.Log($"- {kvp.Key.itemName}: {kvp.Value}");
        }
    }
}
