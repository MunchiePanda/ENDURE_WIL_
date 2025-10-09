using UnityEngine;

[CreateAssetMenu(fileName = "ItemBase", menuName = "Scriptable Objects/Items/_ItemBase")]
public class ItemBase : ScriptableObject
{
    [Header("Item Properties")]
    public int itemID;
    public string itemName;
    public Sprite itemImage;
    public ItemType itemType;   //@Mik, this can be replaced with interface type ~Sio
    
    [Header("Item Usage & Effects")]
    public ItemUsageCategory usageCategory;   // Determines how this item can be used
    public ItemEffectType effectType;         // What stat/attribute this item affects
    public int effectValue;                   // Magnitude of the effect (e.g., +20 hunger, +3 vitality)
    
    // OnValidate runs in the Editor when scripts recompile, the asset is created/imported, or serialized values change in the Inspector. It does not run in builds.
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            itemName = "Item " + name;
        }
        // Generate hash-based ID from item name
        if (!string.IsNullOrEmpty(itemName))
        {
            itemID = itemName.GetHashCode();
        }
    }
}
public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Material,
    Misc
}

public enum ItemUsageCategory
{
    None,
    Interactable,
    Consumable,
    Equipable
}

public enum ItemEffectType
{
    None,
    // Instant stat effects (consumables)
    Health,
    Stamina,
    Hunger,
    SystemExposure,
    // Persistent attribute effects (equipment)
    Vitality,
    Agility,
    Fitness,
    SystemResistance
}

