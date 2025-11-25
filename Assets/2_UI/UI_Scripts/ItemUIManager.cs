using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ENDURE;
using System.Collections.Generic;

public class ItemUIManager : MonoBehaviour
{
    // References to UI Elements
    public Image img_ItemIcon;                 // Icon for the item
    public TMP_Text txt_itemName;              // Item display name
    public TMP_Text txt_itemQuantity;          // Current quantity display
    public Button btn_itemAction;              // Action button (e.g., Use / Equip)
    public TMP_Text txt_itemActionText;        // Label inside the action button
    public ItemBase item;

    private InventoryUIManager owner;

    private Dictionary<string, bool> onceOffItems;

    private void Awake()
    {
        owner = GetComponentInParent<InventoryUIManager>();
        if (btn_itemAction != null)
        {
            btn_itemAction.onClick.AddListener(OnItemActionClicked);
        }

        //<<< I know this is not the best practice but I try>>>

        onceOffItems = new Dictionary<string, bool>();

        onceOffItems.Add("Pulse Stabilizer", true);
        onceOffItems.Add("Nano  Vitaliser", true);
        onceOffItems.Add("Iron Lung", true);

    }

    private void OnDestroy()
    {
        if (btn_itemAction != null)
        {
            btn_itemAction.onClick.RemoveListener(OnItemActionClicked);
        }
    }

    public void Initialize(InventoryUIManager inventoryOwner)
    {
        owner = inventoryOwner;
        UpdateActionButtonState();
    }

    // Update the UI to reflect the current item (called by Inventory/ InventoryUI)
    public void UpdateItemUI(ItemBase item, int itemQuantity)
    {
        this.item = item;
        if (img_ItemIcon != null) img_ItemIcon.sprite = item.itemImage;
        if (txt_itemName != null) txt_itemName.text = item.itemName;
        if (txt_itemQuantity != null) txt_itemQuantity.text = itemQuantity.ToString();

        UpdateActionButtonState();
    }

    private void UpdateActionButtonState()
    {
        if (btn_itemAction == null) return;

        bool isConsumable = item != null && item.itemType == ItemType.Consumable;
        bool isArmor = item != null && item.itemType == ItemType.Armor;
        bool shouldShowAction = isConsumable || isArmor;

        btn_itemAction.gameObject.SetActive(shouldShowAction);

        if (!shouldShowAction || item == null)
        {
            return;
        }

        string label = string.IsNullOrWhiteSpace(item.actionLabel)
            ? (isArmor ? "Equip" : "Use")
            : item.actionLabel;

        if (txt_itemActionText != null)
        {
            txt_itemActionText.text = label;
        }

        btn_itemAction.interactable = owner != null && owner.Inventory != null;
    }

    private void OnItemActionClicked()
    {
        // UI click SFX
        if (owner != null && owner.uiManager != null)
        {
            owner.uiManager.PlayClick();
        }
        else
        {
            var ui = Object.FindFirstObjectByType<UIManager>();
            if (ui != null) ui.PlayClick();
        }

        if (item == null || owner == null || owner.Inventory == null)
        {
            Debug.LogWarning("ItemUIManager: Cannot perform action because dependencies are missing.");
            return;
        }

        switch (item.itemType)
        {
            case ItemType.Consumable:
                if (TryApplyConsumableEffect())
                {
                    if (onceOffItems[item.itemName])
                    {
                        onceOffItems[item.itemName] = false;    //Change to false
                        owner.Inventory.RemoveItem(item, 1);

                    }

                }
                break;
            case ItemType.Armor:
                Debug.Log($"ItemUIManager: Equip logic for '{item.itemName}' not implemented yet.");
                break;
            default:
                Debug.Log($"ItemUIManager: Item '{item.itemName}' has no action.");
                break;
        }
    }

    private bool TryApplyConsumableEffect()
    {
        var playerManager = owner != null ? owner.PlayerManager : null;

        if (playerManager == null)
        {
            Debug.LogWarning("ItemUIManager: No PlayerManager found to apply consumable effect.");
            return false;
        }

        if (item.statEffects == null || item.statEffects.Length == 0)
        {
            Debug.LogWarning($"ItemUIManager: Consumable '{item.itemName}' has no stat effects defined.");
            return false;
        }

        bool appliedAny = false;
        foreach (var effect in item.statEffects)
        {
            if (effect.target == ItemStatTarget.None || Mathf.Approximately(effect.value, 0f))
            {
                continue;
            }

            bool applied = playerManager.ApplyItemStat(effect.target, effect.value);
            appliedAny |= applied;

            if (!applied)
            {
                Debug.LogWarning($"ItemUIManager: Failed to apply {effect.target} effect ({effect.value}) from '{item.itemName}'.");
            }
        }

        if (!appliedAny)
        {
            Debug.LogWarning($"ItemUIManager: No valid stat effects applied for '{item.itemName}'.");
        }

        return appliedAny;
    }
}
