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
        if (!isQuestComplete) return false;

        JournalEntryManager.Instance.UnlockEntry(); //Unlocks a Journal Entry Upon Completion of a Quest

        foreach (QuestObjective objective in quest.questObjectives)
        {
            inventory.RemoveItem(objective.item, objective.quantity);
        }
        
        return inventory.AddItem(quest.rewardItem, quest.rewardQuantity);
    }
}
