using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "CraftingRecipieBase", menuName = "Scriptable Objects/CraftingRecipies/_CraftingRecipieBase")]
public class CraftingRecipieBase : ScriptableObject
{
    [Header("Recipe Output")]
    [SerializeField] private ItemBase craftedItem;
    [SerializeField] private int craftQuantity = 1;
    
    [Header("Required Ingredients")]
    [SerializeField] private List<RecipeIngredient> requiredIngredients = new List<RecipeIngredient>();
    
    // Properties
    public ItemBase CraftedItem => craftedItem;
    public int CraftQuantity => craftQuantity;
    public List<RecipeIngredient> RequiredIngredients => requiredIngredients;
    
    // Check if the given inventory has all required ingredients
    public bool CanCraft(Inventory inventory)
    {
        if (inventory == null) return false;
        
        // Loop through each ingredient and check if the inventory has the required quantity
        foreach (var ingredient in requiredIngredients)
        {
            if (!inventory.HasItem(ingredient.item, ingredient.quantity))
            {
                return false;
            }
        }
        
        return true;
    }
    
    // Attempt to craft the item using the given inventory
    public bool Craft(Inventory inventory)
    {
        // If the inventory does not have all the required ingredients, return false
        if (!CanCraft(inventory))
        {
            Debug.LogWarning($"Cannot craft {craftedItem.itemName}. Missing required ingredients.");
            return false;
        }
        
        // Check if inventory has space for the crafted item
        if (inventory.IsFull && !inventory.HasItem(craftedItem))
        {
            Debug.LogWarning($"Cannot craft {craftedItem.itemName}. Inventory is full and doesn't contain this item to stack.");
            return false;
        }
        
        // Remove required ingredients
        foreach (var ingredient in requiredIngredients)
        {
            if (!inventory.RemoveItem(ingredient.item, ingredient.quantity))
            {
                Debug.LogError($"Failed to remove {ingredient.quantity} {ingredient.item.itemName} from inventory during crafting.");
                return false;
            }
        }
        
        // Add crafted item to inventory
        if (!inventory.AddItem(craftedItem, craftQuantity))
        {
            Debug.LogError($"Failed to add {craftQuantity} {craftedItem.itemName} to inventory after crafting.");
            // Note: In a real game, you might want to restore the consumed ingredients here
            return false;
        }
        
        Debug.Log($"Successfully crafted {craftQuantity} {craftedItem.itemName}!");
        return true;
    }
    
    // Get missing ingredients for this recipe
    public List<RecipeIngredient> GetMissingIngredients(Inventory inventory)
    {
        var missingIngredients = new List<RecipeIngredient>();
        
        if (inventory == null) return requiredIngredients;
        
        foreach (var ingredient in requiredIngredients)
        {
            int availableQuantity = inventory.GetItemQuantity(ingredient.item);
            if (availableQuantity < ingredient.quantity)
            {
                missingIngredients.Add(new RecipeIngredient
                {
                    item = ingredient.item,
                    quantity = ingredient.quantity - availableQuantity
                });
            }
        }
        
        return missingIngredients;
    }
    
    // Get recipe info as a formatted string
    public string GetRecipeInfo()
    {
        string info = $"Recipe: {craftQuantity} {craftedItem.itemName}\n";
        info += "Required Ingredients:\n";
        
        foreach (var ingredient in requiredIngredients)
        {
            info += $"- {ingredient.quantity} {ingredient.item.itemName}\n";
        }
        
        return info;
    }
    
    // Validation method for the Unity Inspector
    private void OnValidate()
    {
        // Ensure craft quantity is positive
        if (craftQuantity <= 0)
        {
            craftQuantity = 1;
        }
        
        // Remove any null ingredients
        requiredIngredients = requiredIngredients.Where(ingredient => ingredient.item != null).ToList();
        
        // Remove any ingredients with zero or negative quantities
        requiredIngredients = requiredIngredients.Where(ingredient => ingredient.quantity > 0).ToList();
    }
}

[System.Serializable]
public struct RecipeIngredient
{
    public ItemBase item;
    public int quantity;
}
