using UnityEngine;

[CreateAssetMenu(fileName = "ItemBase", menuName = "Scriptable Objects/ItemBase")]
public class ItemBase : ScriptableObject
{
    [Header("Item Properties")]
    public int itemID;
    public string itemName;
    public Sprite itemImage;
    public ItemType itemType;   //6 Mik, this can be replaced with interface type ~Sio
    
    //OnValidate is called when entering play mode, script is attched to a GameObject, and the scriptable object is changed
    private void OnValidate()
    {
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

