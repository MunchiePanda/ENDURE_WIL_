using UnityEngine;
using ENDURE;

public static class ItemUsageSystem
{
    // Applies instant consumable effects to the character; returns true on success
    public static bool ApplyConsumable(ItemBase item, CharacterManager character)
    {
        if (item == null || character == null) return false;

        switch (item.effectType)
        {
            case ItemEffectType.Health:
                character.Heal(item.effectValue);
                return true;
            case ItemEffectType.Stamina:
                character.RestoreStamina(item.effectValue);
                return true;
            case ItemEffectType.Hunger:
                if (character is PlayerManager pm)
                {
                    pm.IncreaseHunger(item.effectValue);
                    return true;
                }
                Debug.LogWarning("ItemUsageSystem.ApplyConsumable(): Hunger effect requires PlayerManager.");
                return false;
            case ItemEffectType.SystemExposure:
                if (character is PlayerManager pm2)
                {
                    pm2.ReduceSystemExposure(item.effectValue);
                    return true;
                }
                Debug.LogWarning("ItemUsageSystem.ApplyConsumable(): SystemExposure effect requires PlayerManager.");
                return false;
            case ItemEffectType.None:
                Debug.LogWarning($"ItemUsageSystem.ApplyConsumable(): Item '{item.itemName}' has no effect type.");
                return false;
            default:
                Debug.LogWarning($"ItemUsageSystem.ApplyConsumable(): Effect '{item.effectType}' is not a consumable stat effect.");
                return false;
        }
    }
}


