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
    private Inventory boundInventory;
    private ItemBase boundItem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Update the UI to reflect the current item (called by Inventory/ InventoryUI)
    public void Initialize(Inventory inventory)
    {
        boundInventory = inventory;
    }

    // Update the UI to reflect the current item (called by Inventory/ InventoryUI)
    public void UpdateItemUI(ItemBase item, int itemQuantity)
    {
        boundItem = item;
        img_ItemIcon.sprite = item.itemImage;
        txt_itemName.text = item.itemName;
        txt_itemQuantity.text = itemQuantity.ToString();

        // Configure action button based on usage category
        if (btn_itemAction != null && txt_itemActionText != null)
        {
            btn_itemAction.onClick.RemoveAllListeners();

            switch (item.usageCategory)
            {
                case ItemUsageCategory.Consumable:
                    txt_itemActionText.text = "Use";
                    btn_itemAction.gameObject.SetActive(true);
                    btn_itemAction.onClick.AddListener(OnActionClicked);
                    break;
                case ItemUsageCategory.Equipable:
                    txt_itemActionText.text = GetEquipButtonLabel();
                    btn_itemAction.gameObject.SetActive(true);
                    btn_itemAction.onClick.AddListener(OnActionClicked);
                    break;
                default:
                    btn_itemAction.gameObject.SetActive(false);
                    break;
            }
        }
    }  

    private void OnActionClicked()
    {
        if (boundInventory == null || boundItem == null) return;
        bool result = boundInventory.UseItem(boundItem);
        if (!result) return;

        // Update label after potential equip toggle
        if (boundItem.usageCategory == ItemUsageCategory.Equipable)
        {
            txt_itemActionText.text = GetEquipButtonLabel();
        }
    }

    private string GetEquipButtonLabel()
    {
        if (boundInventory == null || boundItem == null) return "Equip";
        var equipment = boundInventory.GetComponent<EquipmentManager>();
        if (equipment != null && equipment.IsEquipped(boundItem))
        {
            return "Unequip";
        }
        return "Equip";
    }
}
