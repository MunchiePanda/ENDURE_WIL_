using System.Collections.Generic;
using UnityEngine;

public class Quest
{
    public QuestBase quest;
    public float questProgress; //0 to 100%
    public bool isQuestComplete;

    public Quest(QuestBase pQuest)
    {
        quest = pQuest;
        questProgress = 0;
        isQuestComplete = false;
    }

    public bool CheckQuestCompletion(Inventory inventory)
    {
        if (inventory == null) return false;

        UpdateQuestProgress(inventory);
        if (questProgress >= 1f) isQuestComplete = true;

        return isQuestComplete;
    }

    public void UpdateQuestProgress(Inventory inventory)
    {
        if (inventory == null) return;

        int completedObjectives = 0;
        for (int i = 0; i < quest.questObjectives.Count; i++)
        {
            var objective = quest.questObjectives[i];
            objective.UpdateQuestObjective(inventory);
            // write back the updated struct instance so changes persist on the list
            quest.questObjectives[i] = objective;
            if (objective.objectiveComplete) completedObjectives++;
        }
        questProgress = quest.questObjectives.Count > 0
            ? (float)completedObjectives / (float)quest.questObjectives.Count
            : 0f;
    }

    public bool GrantQuestReward(Inventory inventory)
    {
        if (!isQuestComplete || inventory == null || quest == null) return false;

        // Unlock journal entry if JournalEntryManager exists
        if (JournalEntryManager.Instance != null)
        {
            JournalEntryManager.Instance.UnlockEntry();
        }

        // Remove quest objective items from inventory
        if (quest.questObjectives != null)
        {
            foreach (QuestObjective objective in quest.questObjectives)
            {
                if (objective.item != null)
                {
                    inventory.RemoveItem(objective.item, objective.quantity);
                }
            }
        }
        
        // Grant reward item
        if (quest.rewardItem != null && quest.rewardQuantity > 0)
        {
            return inventory.AddItem(quest.rewardItem, quest.rewardQuantity);
        }
        
        return true; // Return true even if no reward item (quest completed successfully)
    }
}
