using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipieUIManager : MonoBehaviour
{
    // UI Components
    public TMP_Text txt_ItemName;          // Item name label
    public TMP_Text txt_Ingredients;       // Ingredients multiline label
    public Button btn_Craft;               // Craft action button

    [SerializeField] private CraftingRecipieBase recipe; // Recipe data to display
    [SerializeField] private Inventory inventory;       // Inventory to craft from

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Wire up the craft button to call CraftItem when clicked
        if (btn_Craft != null)
        {
            btn_Craft.onClick.AddListener(() => {
                var ui = FindObjectOfType<UIManager>(true);
                if (ui != null) ui.PlayClick();
            });
            btn_Craft.onClick.AddListener(CraftItem);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Update the UI to reflect the current recipe (called by CraftingUI)
    public void UpdateRecipeUI(CraftingRecipieBase recipeData)
    {
        if (recipeData == null) return;
        
        recipe = recipeData;
        UpdateRecipeUI();
    }

    // Update the UI using the stored recipe
    public void UpdateRecipeUI()
    {
        if (recipe == null) return;
        
        // Set the item name
        if (txt_ItemName != null)
        {
            txt_ItemName.text = recipe.CraftedItem.itemName;
        }

        // Build ingredients text with smart formatting
        if (txt_Ingredients != null)
        {
            string ingredientsText = "";
            int totalIngredients = recipe.RequiredIngredients.Count;
            
            // Determine ingredients per line based on total count
            int ingredientsPerLine = totalIngredients <= 3 ? 1 : (totalIngredients <= 6 ? 2 : 3);   //Shortform for if total <= 3 then perLine = 1, if <=6 them per line 2, else per line = 3
            
            for (int i = 0; i < totalIngredients; i++)
            {
                var ingredient = recipe.RequiredIngredients[i];
                ingredientsText += $"[{ingredient.quantity}] {ingredient.item.itemName}";
                
                // Add spacing or line break based on position
                if (i < totalIngredients - 1) // Not the last ingredient
                {
                    if ((i + 1) % ingredientsPerLine == 0) // End of line if i+1 is evenly divisible by ingredients per line
                    {
                        ingredientsText += "\n";
                    }
                    else // Same line, add tabs for spacing
                    {
                        ingredientsText += "\t\t";
                    }
                }
            }
            
            txt_Ingredients.text = ingredientsText;
        }

        // Update craft button state/ interactability based on whether we can craft
        if (btn_Craft != null && inventory != null)
        {
            btn_Craft.interactable = recipe.CanCraft(inventory);
        }
    }

    // Called when the craft button is clicked
    public void CraftItem()
    {
        // UI click SFX (in case called from elsewhere)
        var ui = FindObjectOfType<UIManager>(true);
        if (ui != null) ui.PlayClick();

        if (recipe == null || inventory == null)
        {
            Debug.LogWarning("RecipieUIManager CraftItem(): Missing recipe or inventory (D2, D5)");
            return;
        }

        // Attempt to craft the item
        bool success = recipe.Craft(inventory);
        
        if (success)
        {
            // Play add item sound
            if (ui != null) ui.PlayAddItemSound();
            // Update UI to reflect new inventory state
            UpdateRecipeUI();
        }
    }

    // Set the inventory reference (called by CraftingUIManager)
    public void SetInventory(Inventory inventoryRef)
    {
        inventory = inventoryRef;
        // Refresh UI to update craftability
        if (recipe != null)
        {
            UpdateRecipeUI();
        }
    }
}
