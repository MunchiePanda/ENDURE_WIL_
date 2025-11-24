using UnityEngine;

[CreateAssetMenu(fileName = "ItemBase", menuName = "Scriptable Objects/Items/_ItemBase")]
public class ItemBase : ScriptableObject
{
    [Header("Item Properties")]
    public int itemID;
    public string itemName;
    public Sprite itemImage;
    public ItemType itemType;   //@Mik, this can be replaced with interface type ~Sio

    [Header("Stat Effects")]
    [Tooltip("List of stat changes applied when this item is used/equipped. Leave empty for items without effects.")]
    public ItemStatEffect[] statEffects = new ItemStatEffect[0];
    [Tooltip("Action label shown on the inventory button when this item is usable.")]
    public string actionLabel = "Use";

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

        //if type is not consumable or weapon, clear stat effects
        if (itemType != ItemType.Consumable && itemType != ItemType.Weapon && statEffects != null && statEffects.Length > 0)
        {
            statEffects = new ItemStatEffect[0];
        }

        if (string.IsNullOrWhiteSpace(actionLabel))
        {
            actionLabel = itemType == ItemType.Armor ? "Equip" : "Use";
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

public enum ItemStatTarget
{
    None,
    Health,
    Stamina,
    Hunger,
    SystemExposure,
    MaxSystemExposure               //For Cybernetics
}

[System.Serializable]
public struct ItemStatEffect
{
    public ItemStatTarget target;
    public float value;
}

