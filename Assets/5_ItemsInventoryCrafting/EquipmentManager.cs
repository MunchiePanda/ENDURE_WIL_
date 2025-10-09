using UnityEngine;
using System.Collections.Generic;
using ENDURE;

public class EquipmentManager : MonoBehaviour
{
    // Tracks currently equipped items; items remain in inventory
    private readonly HashSet<ItemBase> equippedItems = new HashSet<ItemBase>();

    public bool IsEquipped(ItemBase item) => item != null && equippedItems.Contains(item);

    // Toggle equip state; applies or removes attribute effects accordingly
    public bool ToggleEquip(ItemBase item, CharacterManager character)
    {
        if (item == null || character == null) return false;
        if (item.usageCategory != ItemUsageCategory.Equipable)
        {
            Debug.LogWarning($"EquipmentManager: Item '{item.itemName}' is not equipable.");
            return false;
        }

        bool isEquipped = equippedItems.Contains(item);
        if (isEquipped)
        {
            // Remove effect
            ApplyOrRemoveEquipmentEffect(item, character, remove: true);
            equippedItems.Remove(item);
            Debug.Log($"Unequipped '{item.itemName}'.");
            return true;
        }
        else
        {
            // Apply effect
            ApplyOrRemoveEquipmentEffect(item, character, remove: false);
            equippedItems.Add(item);
            Debug.Log($"Equipped '{item.itemName}'.");
            return true;
        }
    }

    private void ApplyOrRemoveEquipmentEffect(ItemBase item, CharacterManager character, bool remove)
    {
        int signedValue = remove ? -item.effectValue : item.effectValue;

        switch (item.effectType)
        {
            case ItemEffectType.Vitality:
                character.ApplyAttribute(new Attribute { type = AttributeType.Vitality, value = signedValue });
                break;
            case ItemEffectType.Agility:
                character.ApplyAttribute(new Attribute { type = AttributeType.Agility, value = signedValue });
                break;
            case ItemEffectType.Fitness:
                character.ApplyAttribute(new Attribute { type = AttributeType.Fitness, value = signedValue });
                break;
            case ItemEffectType.SystemResistance:
                character.ApplyAttribute(new Attribute { type = AttributeType.SystemResistance, value = signedValue });
                break;
            default:
                Debug.LogWarning($"EquipmentManager: EffectType '{item.effectType}' is not an attribute effect.");
                break;
        }
    }
}


