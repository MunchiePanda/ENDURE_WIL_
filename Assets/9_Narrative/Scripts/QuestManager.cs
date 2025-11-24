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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (inventory == null) inventory = GetComponentInParent<Inventory>();
        if (inventory == null) Debug.LogWarning("QuestManager Start(): inventory is null.");
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

        currentQuest = new Quest(questBase);
        currentQuest.CheckQuestCompletion(inventory);

        Debug.Log($"QuestManager: Added quest '{questBase.questName}'");

        // Notify UI to update (QuestOverviewUIManager will pick this up in its Update loop)
        // The UI checks currentQuest in Update(), so it should refresh automatically

        //TODO: Impliment multiple quests at one time, so getting a new quest will be added to the ongoingQuests list
    }

    public void UpdateCurrentQuest()
    {
        if (currentQuest.CheckQuestCompletion(inventory))   //CheckQuestCompletion also runs the updates
        {
            CompleteQuest();
        }
    }

    void CompleteQuest()
    {
        Debug.Log("QuestManager CompleteQuest(): QUEST COMPLETE!");
        currentQuest.GrantQuestReward(inventory);
        //TODO: Add logic for if reward can't be granted
        //Add UI logic to click accept reward

        currentQuest = null;
    }
}
