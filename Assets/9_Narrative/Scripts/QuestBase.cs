using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestBase", menuName = "Scriptable Objects/QuestBase")]
public class QuestBase : ScriptableObject
{
    [Header("Quest Details")]
    public string questName;
    public string questDescription;
    public List<QuestObjective> questObjectives = new List<QuestObjective>();

    public ItemBase rewardItem;
    public int rewardQuantity;
    public CraftingRecipieBase rewardRecipie;
}

[System.Serializable]
public struct QuestObjective
{
    public ItemBase item;   //what item to get
    public int quantity;    //how many of said item to get
    public int currentQuantity;
    public bool objectiveComplete;

    public QuestObjective(ItemBase pItem, int pQuantity) 
    {
        item = pItem; 
        quantity = pQuantity;
        currentQuantity = 0;
        objectiveComplete = false;
    }

    public void UpdateQuestObjective(Inventory inventory)
    {
        if (inventory != null)
        {
            currentQuantity = inventory.GetItemQuantity(item);
            objectiveComplete = inventory.HasItem(item, quantity);
        }
    }

    public string GetQuestObjectiveText()
    {
        return ($"{currentQuantity}/{quantity} {item.itemName}");
    }
}