using UnityEngine;
using UnityEngine.UI;

// Attach to a world NPC. Assign the NPC's quest array here.
// Call SetQuestOnPanel() right before opening the quest UI panel.
public class QuestgiverNPCBinder : MonoBehaviour
{
    [Header("Quest Array")]
    [Tooltip("Array of quests for this NPC. Will select first quest where isComplete = false.")]
    public QuestBase[] npcQuests = new QuestBase[0];
    
    [Header("Legacy Support")]
    [Tooltip("Single quest (legacy). Only used if npcQuests array is empty.")]
    public QuestBase npcQuest;
    public QuestgiverManager questgiverManager; // Reference to the UI panel's manager (e.g., from UI_Canvas)
    public QuestGiverUIManager questGiverUiManager; // Support older/new UI manager script names

    private void Start()
    {
        // Try to find managers early so they're ready when needed
        TryResolvePanelManagers();
    }

    /// <summary>
    /// Gets the first incomplete quest from the quest array (or legacy single quest).
    /// Returns null if all quests are complete or no quests available.
    /// </summary>
    public QuestBase GetFirstIncompleteQuest()
    {
        // Check quest array first
        if (npcQuests != null && npcQuests.Length > 0)
        {
            for (int i = 0; i < npcQuests.Length; i++)
            {
                var quest = npcQuests[i];
                if (quest != null)
                {
                    Debug.Log($"QuestgiverNPCBinder: Checking quest {i}: '{quest.questName}', isComplete: {quest.isComplete}");
                    if (!quest.isComplete)
                    {
                        Debug.Log($"QuestgiverNPCBinder: Selected first incomplete quest: '{quest.questName}'");
                        return quest;
                    }
                }
            }
        }
        
        // Fallback to legacy single quest
        if (npcQuest != null && !npcQuest.isComplete)
        {
            Debug.Log($"QuestgiverNPCBinder: Selected legacy quest: '{npcQuest.questName}'");
            return npcQuest;
        }
        
        Debug.Log("QuestgiverNPCBinder: No incomplete quests found.");
        return null;
    }

    public void SetQuestOnPanel()
    {
        QuestBase questToSet = GetFirstIncompleteQuest();
        
        // Try to resolve managers again in case they weren't available at Start
        if (!TryResolvePanelManagers())
        {
            // Defer assignment: panel may be instantiated on open; store pending quest for pickup in OnEnable
            SetPendingQuest(questToSet);
            Debug.Log($"QuestgiverNPCBinder: Could not find QuestgiverManager or QuestGiverUIManager. Quest '{questToSet?.questName}' stored as pending.");
            return; // no warning; this is a valid path when UI instantiates on demand
        }
        // Set quest on both managers if both exist (they share the same buttons, so both need the correct quest)
        if (questgiverManager != null)
        {
            questgiverManager.SetQuest(questToSet);
            Debug.Log($"QuestgiverNPCBinder: Set quest '{questToSet?.questName}' on QuestgiverManager.");
        }
        if (questGiverUiManager != null)
        {
            questGiverUiManager.SetQuest(questToSet);
            // Force refresh description immediately if panel is already active
            if (questGiverUiManager.panel_QuestgiverUI != null && questGiverUiManager.panel_QuestgiverUI.activeSelf)
            {
                questGiverUiManager.RefreshQuestDescription();
            }
            Debug.Log($"QuestgiverNPCBinder: Set quest '{questToSet?.questName}' on QuestGiverUIManager. Panel active: {questGiverUiManager.panel_QuestgiverUI?.activeSelf ?? false}");
        }
    }

    private bool TryResolvePanelManagers()
    {
        if (questgiverManager != null || questGiverUiManager != null) return true;

        // Method 1: Try UIManager first (most reliable if player spawns with UI)
        UIManager uiManager = FindObjectOfType<UIManager>(true);
        if (uiManager != null)
        {
            // Check if UIManager has a QuestGiverUIManager reference
            if (uiManager.questgiverUIManager != null)
            {
                questGiverUiManager = uiManager.questgiverUIManager;
                return true;
            }
        }

        // Method 2: Direct FindObjectOfType search (including inactive)
#if UNITY_2023_1_OR_NEWER
        questgiverManager = Object.FindFirstObjectByType<QuestgiverManager>(FindObjectsInactive.Include);
        if (questgiverManager == null)
            questGiverUiManager = Object.FindFirstObjectByType<QuestGiverUIManager>(FindObjectsInactive.Include);
#else
        // Search including inactive objects (UI panels may start disabled)
        questgiverManager = FindObjectOfType<QuestgiverManager>(true);
        if (questgiverManager == null)
            questGiverUiManager = FindObjectOfType<QuestGiverUIManager>(true);
#endif
        if (questgiverManager != null || questGiverUiManager != null) return true;

        // Method 3: Search under any Canvas (UI is usually under a Canvas)
        var canvases = FindObjectsOfType<Canvas>(true);
        foreach (var canvas in canvases)
        {
            var found = canvas.GetComponentInChildren<QuestgiverManager>(true);
            var foundAlt = canvas.GetComponentInChildren<QuestGiverUIManager>(true);
            if (found != null || foundAlt != null)
            {
                if (found != null) questgiverManager = found;
                if (foundAlt != null) questGiverUiManager = foundAlt;
                return true;
            }
        }

        // Method 4: Search in player hierarchy (UI might be under player)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            questgiverManager = player.GetComponentInChildren<QuestgiverManager>(true);
            if (questgiverManager == null)
                questGiverUiManager = player.GetComponentInChildren<QuestGiverUIManager>(true);
            if (questgiverManager != null || questGiverUiManager != null) return true;
        }

        return false;
    }

    // Pending quest passing to handle instantiate-on-open flows
    private static QuestBase pendingQuest;
    public static void SetPendingQuest(QuestBase qb) { pendingQuest = qb; }
    public static bool TryConsumePending(out QuestBase qb)
    {
        qb = pendingQuest;
        pendingQuest = null;
        return qb != null;
    }
}

