using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxInventorySize = 50;
    
    [Header("Inventory Contents")]
    [SerializeField] private Dictionary<ItemBase, int> items = new Dictionary<ItemBase, int>();
    
    // Events for inventory changes (Delegates)
    // @Ang 2 UI - These can be used to update the UI
    public System.Action<ItemBase, int> OnItemAdded;
    public System.Action<ItemBase, int> OnItemRemoved;
    public System.Action<ItemBase, int, int> OnItemQuantityChanged; // item, oldQuantity, newQuantity
    
    // Properties
    public int CurrentItemCount => items.Count;
    public int TotalItemQuantity => items.Values.Sum();
    public bool IsFull => TotalItemQuantity >= maxInventorySize;
    
#if UNITY_EDITOR
    // Editor-only read-only view of items for custom inspector display
    public System.Collections.Generic.IReadOnlyDictionary<ItemBase, int> DebugItems => items;
#endif
    
    // Add items to inventory
    public bool AddItem(ItemBase item, int quantity = 1)
    {
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
        return items.ContainsKey(item) ? items[item] : 0;
    }
    
    // Check if inventory contains item
    public bool HasItem(ItemBase item, int quantity = 1)
    {
        return GetItemQuantity(item) >= quantity;
    }
    
    // Get all items as a list (for UI display)
    public List<KeyValuePair<ItemBase, int>> GetAllItems()
    {
        return items.ToList();
    }
    
    // Clear entire inventory
    public void ClearInventory()
    {
        items.Clear();
        OnItemRemoved?.Invoke(null, 0); // Signal complete clear
    }
    
    // Get items by type (filter by ItemType)
    public List<KeyValuePair<ItemBase, int>> GetItemsByType(ItemType type)
    {
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
