using UnityEngine;

/// <summary>
/// Interactable component for quest giver NPCs.
/// Handles quest selection, completion checking, and UI opening.
/// </summary>
public class QuestgiverInteractable : InteractableBase
{
    [Header("Quest Giver Settings")]
    [Tooltip("Reference to QuestgiverNPCBinder component that holds quest array.")]
    public QuestgiverNPCBinder questgiverBinder;
    
    [Tooltip("UI GameObject to open when interacting with quest giver (e.g., quest UI panel).")]
    public GameObject questUIPanel;

    [Header("Interaction Animation")]
    public Animation onInteractAnimation;

    private void Awake()
    {
        // Auto-find binder if not assigned
        if (questgiverBinder == null)
        {
            questgiverBinder = GetComponent<QuestgiverNPCBinder>();
        }
    }

    public override void Interact(Interactor interactor)
    {
        if (onInteractAnimation != null) onInteractAnimation.Play();

        if (questgiverBinder == null)
        {
            Debug.LogWarning($"QuestgiverInteractable: No QuestgiverNPCBinder found on {gameObject.name}.");
            return;
        }

        // Get QuestManager to check current quest
        QuestManager questManager = null;
        if (QuestManager.TryGet(out questManager))
        {
            // Check if player has a current quest
            if (questManager.currentQuest != null && questManager.currentQuest.quest != null)
            {
                QuestBase currentQuestBase = questManager.currentQuest.quest;
                
                // Check if current quest belongs to this NPC's quest array
                bool isQuestFromThisNPC = IsQuestFromThisNPC(currentQuestBase);
                
                if (isQuestFromThisNPC)
                {
                    // Update quest progress
                    questManager.UpdateCurrentQuest();
                    
                    // Check if quest is complete (requirements met)
                    if (questManager.currentQuest.isQuestComplete)
                    {
                        // Complete the quest: grant rewards and mark as complete
                        Debug.Log($"QuestgiverInteractable: Completing quest '{currentQuestBase.questName}'");
                        CompleteQuest(questManager);
                        return; // Don't open quest UI if completing
                    }
                    else
                    {
                        // Player already has an active quest from this NPC that's not complete
                        // Don't offer a new quest - they need to complete the current one first
                        Debug.Log($"QuestgiverInteractable: Player already has active quest '{currentQuestBase.questName}' from this NPC. Progress: {questManager.currentQuest.questProgress * 100:F0}%");
                        return;
                    }
                }
                else
                {
                    Debug.Log($"QuestgiverInteractable: Player has quest '{currentQuestBase.questName}' but it's not from this NPC. Allowing new quest offer.");
                }
                // If current quest is not from this NPC, allow offering new quest
            }
        }

        // If no quest ready to complete and no active quest from this NPC, offer first incomplete quest
        QuestBase questToOffer = questgiverBinder.GetFirstIncompleteQuest();
        
        if (questToOffer == null)
        {
            Debug.Log("QuestgiverInteractable: No incomplete quests available.");
            return;
        }
        
        // Try to set quest on managers via binder (may store as pending if managers not found)
        questgiverBinder.SetQuestOnPanel();
        
        // Open UI first (panel must be active for OnEnable to run and pick up pending quest)
        GameObject panelInstance = OpenQuestUI(interactor);
        
        // After opening, set quest on the actual opened panel instance
        // Do this after panel is opened so OnEnable can run and we can set quest directly
        if (panelInstance != null && questToOffer != null)
        {
            // Try to set quest on the opened panel's manager
            QuestGiverUIManager uiManager = panelInstance.GetComponent<QuestGiverUIManager>();
            if (uiManager == null)
            {
                uiManager = panelInstance.GetComponentInChildren<QuestGiverUIManager>(true);
            }
            
            if (uiManager != null)
            {
                uiManager.SetQuest(questToOffer);
                // Force immediate refresh in case OnEnable already ran
                uiManager.RefreshQuestDescription();
                Debug.Log($"QuestgiverInteractable: Set quest '{questToOffer.questName}' on opened panel instance (QuestGiverUIManager).");
            }
            else
            {
                // Try QuestgiverManager as fallback
                QuestgiverManager manager = panelInstance.GetComponent<QuestgiverManager>();
                if (manager == null)
                {
                    manager = panelInstance.GetComponentInChildren<QuestgiverManager>(true);
                }
                
                if (manager != null)
                {
                    manager.SetQuest(questToOffer);
                    Debug.Log($"QuestgiverInteractable: Set quest '{questToOffer.questName}' on opened panel instance (QuestgiverManager).");
                }
            }
        }
    }

    private bool IsQuestFromThisNPC(QuestBase questToCheck)
    {
        if (questgiverBinder == null || questToCheck == null) return false;
        
        // Check if quest is in the quest array
        if (questgiverBinder.npcQuests != null && questgiverBinder.npcQuests.Length > 0)
        {
            foreach (var quest in questgiverBinder.npcQuests)
            {
                if (quest == questToCheck)
                {
                    return true;
                }
            }
        }
        
        // Check legacy single quest
        if (questgiverBinder.npcQuest == questToCheck)
        {
            return true;
        }
        
        return false;
    }

    private void CompleteQuest(QuestManager questManager)
    {
        // Use QuestManager's completion method
        bool success = questManager.CompleteCurrentQuest();
        
        if (success)
        {
            // Play pickup sound for reward
            var uiManager = FindObjectOfType<UIManager>(true);
            if (uiManager != null)
            {
                uiManager.PlayItemPickupSound();
            }
        }
    }

    private GameObject OpenQuestUI(Interactor interactor)
    {
        if (questUIPanel == null)
        {
            Debug.LogWarning($"QuestgiverInteractable: No questUIPanel assigned on {gameObject.name}.");
            return null;
        }

        // Delegate to the player's UI manager for screen handling
        var uiManager = interactor.GetComponentInChildren<UIManager>(true);
        if (uiManager == null)
        {
            Debug.LogWarning("QuestgiverInteractable Interact: UIManager not found under Interactor.", this);
            return null;
        }

        var instance = uiManager.OpenScreen(questUIPanel, false, false);
        if (instance == null)
        {
            Debug.LogWarning($"QuestgiverInteractable Interact: Could not open quest UI '{questUIPanel.name}'.", this);
        }
        
        return instance;
    }
}

