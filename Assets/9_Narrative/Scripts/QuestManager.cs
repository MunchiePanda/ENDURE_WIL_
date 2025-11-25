using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.VolumeComponent;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    public Quest currentQuest;
    //public List<Quest> ongoingQuests;

    public Inventory inventory;

    // Persist quest data between scene loads without forcing the entire player object to stay alive
    private static Quest persistedQuestState;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            // Keep the first instance; optional: DontDestroyOnLoad(Instance);
            // Destroying duplicates avoids ambiguity when searching.
            Destroy(gameObject);
            return;
        }

        // Rehydrate quest state if one was active before this instance was rebuilt
        if (persistedQuestState != null && currentQuest == null)
        {
            currentQuest = persistedQuestState;
        }

        ResolveInventoryReference();
    }

    public static bool TryGet(out QuestManager questManager)
    {
        questManager = Instance;
        if (questManager != null) return true;
#if UNITY_2023_1_OR_NEWER
        questManager = Object.FindFirstObjectByType<QuestManager>(FindObjectsInactive.Include);
#else
        questManager = FindObjectOfType<QuestManager>(true);
#endif
        return questManager != null;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            persistedQuestState = currentQuest;
            Instance = null;
        }
    }

    [Header("Quest Reset")]
    [Tooltip("Reset all quest completion states when game starts. Set to true to reset quests on every play session.")]
    [SerializeField] private bool resetQuestsOnStart = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Reset all quest completion states at game start (only on first instance)
        if (resetQuestsOnStart && Instance == this)
        {
            ResetAllQuestCompletionStates();
        }

        ResolveInventoryReference();

        if (currentQuest != null && inventory != null)
        {
            currentQuest.CheckQuestCompletion(inventory);
        }
    }

    /// <summary>
    /// Resets all quest completion states by finding all QuestgiverNPCBinder instances
    /// and resetting their quest arrays. This prevents quest persistence between play sessions.
    /// </summary>
    private void ResetAllQuestCompletionStates()
    {
        Debug.Log("QuestManager: Resetting all quest completion states...");
        int questsReset = 0;

        // Find all QuestgiverNPCBinder instances in the scene
        QuestgiverNPCBinder[] allQuestgivers = FindObjectsOfType<QuestgiverNPCBinder>(true);
        
        foreach (QuestgiverNPCBinder binder in allQuestgivers)
        {
            if (binder == null) continue;

            // Reset quests in the array
            if (binder.npcQuests != null && binder.npcQuests.Length > 0)
            {
                foreach (QuestBase quest in binder.npcQuests)
                {
                    if (quest != null && quest.isComplete)
                    {
                        quest.isComplete = false;
                        questsReset++;
                    }
                }
            }

            // Reset legacy single quest
            if (binder.npcQuest != null && binder.npcQuest.isComplete)
            {
                binder.npcQuest.isComplete = false;
                questsReset++;
            }
        }

        Debug.Log($"QuestManager: Reset {questsReset} quest completion state(s).");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddQuest(QuestBase questBase)
    {
        if (questBase == null)
        {
            Debug.LogWarning("QuestManager AddQuest(): QuestBase is null!");
            return;
        }

        // Clear any existing quest first
        if (currentQuest != null)
        {
            Debug.Log($"QuestManager: Replacing existing quest '{currentQuest.quest.questName}' with new quest '{questBase.questName}'");
        }

        currentQuest = new Quest(questBase);
        currentQuest.CheckQuestCompletion(inventory);
        persistedQuestState = currentQuest;

        Debug.Log($"QuestManager: Added quest '{questBase.questName}'");

        // Force immediate quest progress update so UI shows correct data
        if (inventory != null)
        {
            currentQuest.UpdateQuestProgress(inventory);
        }

        // Notify UI to update (QuestOverviewUIManager will pick this up in its Update loop)
        // The UI checks currentQuest in Update(), so it should refresh automatically

        //TODO: Impliment multiple quests at one time, so getting a new quest will be added to the ongoingQuests list
    }

    public void UpdateCurrentQuest()
    {
        if (currentQuest == null)
        {
            return;
        }

        // Update quest progress but don't auto-complete
        // Quest completion must be done manually by talking to NPC
        currentQuest.UpdateQuestProgress(inventory);
        currentQuest.CheckQuestCompletion(inventory);  // Updates isQuestComplete flag
        persistedQuestState = currentQuest;
    }

    /// <summary>
    /// Manually completes the current quest. Called by NPC when player returns with completed objectives.
    /// </summary>
    public bool CompleteCurrentQuest()
    {
        if (currentQuest == null || !currentQuest.isQuestComplete)
        {
            return false;
        }

        Debug.Log("QuestManager CompleteCurrentQuest(): QUEST COMPLETE!");
        bool rewardGranted = currentQuest.GrantQuestReward(inventory);
        
        if (rewardGranted)
        {
            // Mark quest as complete in ScriptableObject
            if (currentQuest.quest != null)
            {
                currentQuest.quest.isComplete = true;
            }
        }

        currentQuest = null;
        persistedQuestState = null;
        
        return rewardGranted;
    }

    void ResolveInventoryReference()
    {
        if (inventory != null) return;

        inventory = GetComponent<Inventory>();
        if (inventory == null) inventory = GetComponentInParent<Inventory>();
        if (inventory == null) inventory = FindObjectOfType<Inventory>();

        if (inventory == null)
        {
            Debug.LogWarning("QuestManager ResolveInventoryReference(): inventory is null. Quest progress cannot update until an inventory is found.");
        }
    }
}
