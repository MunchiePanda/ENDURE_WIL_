using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemUIManager : MonoBehaviour
{
    // References to UI Elements
    public Image img_ItemIcon;                 // Icon for the item
    public TMP_Text txt_itemName;              // Item display name
    public TMP_Text txt_itemQuantity;          // Current quantity display
    public Button btn_itemAction;              // Action button (e.g., Use / Equip)
    public TMP_Text txt_itemActionText;        // Label inside the action button

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Update the UI to reflect the current item (called by Inventory/ InventoryUI)
    public void UpdateItemUI(ItemBase item, int itemQuantity)
    {
        img_ItemIcon.sprite = item.itemImage;
        txt_itemName.text = item.itemName;
        txt_itemQuantity.text = itemQuantity.ToString();

    /* @Ang 6 Interactable - this needs to be implimented
        if(item implimets IItemAction)  //if the item implements the IItemAction interface, set its Action text
        {
            txt_itemActionText.text = item.itemActionText;
        }
        else    //if the item does not implement the IItemAction interface, hide the action button
        {
            btn_itemAction.gameObject.SetActive(false);
        }
    */
    }  
}
