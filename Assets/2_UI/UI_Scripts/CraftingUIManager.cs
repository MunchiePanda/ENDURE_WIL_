using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CraftingUIManager : MonoBehaviour
{
    [Header("Recipe List Management")]
    [SerializeField] private List<CraftingRecipieBase> availableRecipes = new List<CraftingRecipieBase>();
    [SerializeField] private GameObject recipeUIPrefab; // Prefab for individual recipe UI
    [SerializeField] private Transform content; // Content transform with VerticalLayoutGroup for recipe UI instances
    [SerializeField] private Inventory playerInventory; // Reference to player's inventory
    
    // Lookup: map each CraftingRecipieBase to its instantiated RecipieUIManager for fast updates
    private readonly Dictionary<CraftingRecipieBase, RecipieUIManager> recipeToUiLookup = new Dictionary<CraftingRecipieBase, RecipieUIManager>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Check if we have the required references
        if (recipeUIPrefab == null || content == null)
        {
            Debug.LogWarning("CraftingUIManager OnValidate(): Missing recipeUIPrefab or content references (D2, D5)");
            return;
        }
        
        // Create UI for any recipes that don't have UI yet
        foreach (var recipe in availableRecipes)
        {
            if (recipe != null && !recipeToUiLookup.ContainsKey(recipe))
            {
                CreateRecipeUI(recipe);
            }
        }
        RefreshRecipeList();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Add a recipe to the available recipes list
    public void AddRecipe(CraftingRecipieBase recipe)
    {
        if (recipe == null || availableRecipes.Contains(recipe)) return;
        
        availableRecipes.Add(recipe);
        CreateRecipeUI(recipe);
    }

    // Remove a recipe from the available recipes list
    public void RemoveRecipe(CraftingRecipieBase recipe)
    {
        if (recipe == null || !availableRecipes.Contains(recipe)) return;
        
        availableRecipes.Remove(recipe);
        
        // Remove the UI instance
        if (recipeToUiLookup.TryGetValue(recipe, out RecipieUIManager recipeUI) && recipeUI != null)
        {
            Destroy(recipeUI.gameObject);
            recipeToUiLookup.Remove(recipe);
        }
    }

    // Refresh the entire recipe list UI
    public void RefreshRecipeList()
    {
        // Clear existing UI
        foreach (var kvp in recipeToUiLookup)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        recipeToUiLookup.Clear();

        // Create UI for each available recipe
        foreach (var recipe in availableRecipes)
        {
            CreateRecipeUI(recipe);
        }
    }

    // Create UI instance for a single recipe
    private void CreateRecipeUI(CraftingRecipieBase recipe)
    {
        if (recipeUIPrefab == null || content == null || recipe == null)
        {
            Debug.LogWarning("CraftingUIManager CreateRecipeUI(): Missing references or recipe is null (D2, D5)");
            return;
        }

        // Instantiate the recipe UI prefab
        GameObject recipeUIObject = Instantiate(recipeUIPrefab, content);

        // Get the RecipieUIManager component and populate it
        RecipieUIManager recipeUIManager = recipeUIObject.GetComponent<RecipieUIManager>();
        if (recipeUIManager != null)
        {
            // Set the inventory reference for crafting functionality
            recipeUIManager.SetInventory(playerInventory);
            recipeUIManager.UpdateRecipeUI(recipe);
            recipeToUiLookup[recipe] = recipeUIManager;
        }
        
        // Set a meaningful name for the instantiated object
        recipeUIObject.name = $"RecipeUI_{recipe.CraftedItem.itemName}";
    }

    // Set the list of available recipes (useful for loading from ScriptableObjects)
    public void SetAvailableRecipes(List<CraftingRecipieBase> recipes)
    {
        availableRecipes.Clear();
        availableRecipes.AddRange(recipes);
        RefreshRecipeList();
    }

    // Get the current list of available recipes
    public List<CraftingRecipieBase> GetAvailableRecipes()
    {
        return new List<CraftingRecipieBase>(availableRecipes);
    }
}
