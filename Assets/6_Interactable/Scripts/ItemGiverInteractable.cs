using UnityEngine;

/// <summary>
///    This is the interactable object that gives an item to the player.
///    Impliment later: multiple item types, can take one item at a time.
///</summary>

public class ItemGiverInteractable : InteractableBase
{
    [Header("Interaction - Item Giver")]
    public Animation onInteractAnimation;   //the animation that is played when the interaction is successful
    public ItemBase item;                   //the item that is given to the player
    public int itemAmount = 1;              //the amount of the item that is given to the player

    [Header("Visuals After Successful Interact")]
    public GameObject VisualAsset;          //parent of all the different visual variants
    public GameObject beforeInteractModel;  //the model that is shown before the interaction
    public GameObject afterInteractModel;   //the model that is shown after the interaction

    [Header("Interactable Colour thingy")]
    [SerializeField] GameObject highlightedObject;
    [SerializeField] private Material[] orignalMats;
    [SerializeField] private Material[] outlineMats;
    private Renderer rd;

    /// <summary>
    /// Implimentation of the Interact method from the IInteractable interface.
    /// Adds the item to the player's inventory and changes the visuals.
    /// </summary>
    public override void Interact(Interactor interactor)
    {
        if (onInteractAnimation != null) onInteractAnimation.Play();    //play the animation

        // Find the interactor's inventory and add the item; else log a warning
        var inventory = interactor.GetComponentInChildren<Inventory>();
        if (inventory != null)
        {
            bool success = inventory.AddItem(item, itemAmount);   //add the item to the inventory
            isInteractable = !success;                            //set isInteractable to the inverse of the returned bool (successful or not)
            ChangeVisuals(isInteractable);                        //change visuals to suit new isInteractable state
            
            if (success)
            {
                // Play pickup sound
                var uiManager = FindObjectOfType<UIManager>(true);
                if (uiManager != null)
                {
                    uiManager.PlayItemPickupSound();
                }
            }
            else
            {
                Debug.Log($"ItemGiverInteractable Interact(): Failed to add {itemAmount} {item.itemName} to inventory. (D5, D6)", interactor);
            }
        }
        else
        {
            Debug.LogWarning($"ItemGiverInteractable Interact(): No Inventory found on '{interactor.gameObject.name}' or its children. (D5, D6)", interactor);
        }
    }

    private void Start()
    {
        rd = highlightedObject.GetComponent<Renderer>();
        ChangeVisuals(isInteractable); //set the initial visual state based on isInteractable
    }

    // Enables the target child under VisualAsset and disables all others
    private void ChangeVisuals(bool isInteractable)
    {
        beforeInteractModel.SetActive(isInteractable);
        afterInteractModel.SetActive(!isInteractable);  //opposite of isInteractable
    }


    //<<<Mouse Hovers Over Item>>>

    // Runs once when the mouse first moves over this object
    private void OnMouseEnter()
    {
        if(outlineMats == null) return;

        rd.materials = outlineMats;
    }

    // Runs once when the mouse leaves this object
    private void OnMouseExit()
    {
        if (orignalMats == null) return;

        rd.materials = orignalMats;
    }

}


