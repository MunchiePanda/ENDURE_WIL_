using UnityEngine;
using UnityEngine.UI;

// Attach to a world NPC. Assign the NPC's QuestBase here.
// Call SetQuestOnPanel() right before opening the quest UI panel.
public class QuestgiverNPCBinder : MonoBehaviour
{
    public QuestBase npcQuest;
    public QuestgiverManager questgiverManager; // Reference to the UI panel's manager (e.g., from UI_Canvas)
    public QuestGiverUIManager questGiverUiManager; // Support older/new UI manager script names

    private void Start()
    {
        // Try to find managers early so they're ready when needed
        TryResolvePanelManagers();
    }

    public void SetQuestOnPanel()
    {
        // Try to resolve managers again in case they weren't available at Start
        if (!TryResolvePanelManagers())
        {
            // Defer assignment: panel may be instantiated on open; store pending quest for pickup in OnEnable
            SetPendingQuest(npcQuest);
            Debug.Log($"QuestgiverNPCBinder: Could not find QuestgiverManager or QuestGiverUIManager. Quest '{npcQuest?.questName}' stored as pending.");
            return; // no warning; this is a valid path when UI instantiates on demand
        }
        if (questgiverManager != null)
        {
            questgiverManager.SetQuest(npcQuest);
            Debug.Log($"QuestgiverNPCBinder: Set quest '{npcQuest?.questName}' on QuestgiverManager.");
        }
        else if (questGiverUiManager != null)
        {
            questGiverUiManager.SetQuest(npcQuest);
            Debug.Log($"QuestgiverNPCBinder: Set quest '{npcQuest?.questName}' on QuestGiverUIManager.");
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

