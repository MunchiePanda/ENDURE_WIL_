using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ENDURE;

public class QuestGiverUIManager : MonoBehaviour
{
    public QuestBase quest;
    public GameObject panel_QuestgiverUI;
    public TMP_Text questDescription;
    public Button btn_AcceptQuest;
    public Button btn_RejectQuest;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (panel_QuestgiverUI != null)
        {
            panel_QuestgiverUI.SetActive(false);
        }
        
        // Clear quest reference on start - quests should be set dynamically via SetQuest()
        // This prevents stale quest data from inspector assignments
        if (quest != null)
        {
            Debug.Log($"QuestGiverUIManager Start: Clearing pre-assigned quest '{quest.questName}' from inspector. Quest will be set dynamically.");
        }
        quest = null;
        
        if (questDescription != null)
            questDescription.text = string.Empty;
        
        // Setup button listeners with null checks
        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        // Remove existing listeners first to prevent duplicates
        if (btn_AcceptQuest != null)
        {
            btn_AcceptQuest.onClick.RemoveListener(OnAcceptQuestButtonClicked);
            btn_AcceptQuest.onClick.AddListener(OnAcceptQuestButtonClicked);
            Debug.Log("QuestGiverUIManager: Accept button listener added.");
        }
        else
        {
            Debug.LogWarning("QuestGiverUIManager: btn_AcceptQuest is null! Button will not work. Make sure it's assigned in the inspector.");
        }

        if (btn_RejectQuest != null)
        {
            btn_RejectQuest.onClick.RemoveListener(OnRejectQuestButtonClicked);
            btn_RejectQuest.onClick.AddListener(OnRejectQuestButtonClicked);
            Debug.Log("QuestGiverUIManager: Reject button listener added.");
        }
        else
        {
            Debug.LogWarning("QuestGiverUIManager: btn_RejectQuest is null! Button will not work. Make sure it's assigned in the inspector.");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    string GetQuestDescription()
    {
        if (quest == null) return string.Empty;
        string objectivesText = quest.questName + " \n";
        objectivesText += "\n" + quest.questDescription;
        foreach (QuestObjective objective in quest.questObjectives)
        {
            //objectivesText += "\n " + objective.GetQuestObjectiveText();      //this fucked up the currentQuantity, so we change it and do it manually
            objectivesText += "\n " + objective.item.itemName + " x" + objective.quantity.ToString();
        }

        if(quest.rewardRecipie != null)
            objectivesText += "\n Rewards: Crafting Recipe for " + quest.rewardRecipie.CraftedItem.itemName;
        if(quest.rewardItem != null)
        {
            // Prefer readable item name if available
            if (quest.rewardItem is ItemBase rewardItemBase && rewardItemBase != null)
            {
                objectivesText += "\n Rewards: " + rewardItemBase.itemName;
            }
            else
            {
                objectivesText += "\n Rewards: " + quest.rewardItem.ToString();
            }
        }

        return objectivesText;
    }

    public void SetQuest(QuestBase newQuest)
    {
        quest = newQuest;
        // Always update text immediately when quest is set
        if (questDescription != null)
        {
            questDescription.text = quest != null ? GetQuestDescription() : string.Empty;
        }
        Debug.Log($"QuestGiverUIManager: Quest set to '{newQuest?.questName}'");
    }

    void OnEnable()
    {
        // Pick up a pending quest (for instantiate-on-open flow)
        QuestBase pending;
        if (QuestgiverNPCBinder.TryConsumePending(out pending))
        {
            SetQuest(pending);
        }
        
        // Always refresh description when the panel is enabled (important for UI panel reuse)
        RefreshQuestDescription();
    }
    
    public void RefreshQuestDescription()
    {
        if (questDescription != null)
        {
            if (quest != null)
            {
                questDescription.text = GetQuestDescription();
            }
            else
            {
                questDescription.text = string.Empty;
            }
        }
    }

    void OnAcceptQuestButtonClicked()
    {
        Debug.Log("QuestGiverUIManager: Accept button clicked!");
        // UI click SFX
        var ui = FindObjectOfType<UIManager>(true);
        if (ui != null) ui.PlayClick();
        
        if (quest == null)
        {
            Debug.LogWarning("QuestGiverUIManager OnAcceptQuestButtonClicked(): No quest assigned!");
            CloseQuestPanel();
            return;
        }

        QuestManager questManager = null;
        
        // Try singleton instance first
        if (QuestManager.TryGet(out questManager))
        {
            // Found via singleton or FindObjectOfType
        }
        else
        {
            // Fallback: search in hierarchy
            questManager = GetComponent<QuestManager>();
            if (questManager == null) questManager = GetComponentInParent<QuestManager>();
            if (questManager == null) questManager = FindObjectOfType<QuestManager>(true);
        }

        if (questManager == null)
        {
            Debug.LogWarning("QuestGiverUIManager OnAcceptQuestButtonClicked(): Can't find quest manager. Make sure QuestManager is attached to the player or exists in the scene.");
            CloseQuestPanel();
            return;
        }

        // Store the quest to add before closing panel
        QuestBase questToAdd = quest;
        
        // Clear quest reference immediately to prevent showing stale data
        quest = null;
        
        // Close panel first
        CloseQuestPanel();
        
        // Add quest after panel is closed to ensure QuestOverviewUI updates correctly
        questManager.AddQuest(questToAdd);
        
        Debug.Log($"QuestGiverUIManager: Accepted and added quest '{questToAdd?.questName}'");
    }

    void OnRejectQuestButtonClicked()
    {
        Debug.Log("QuestGiverUIManager: Reject button clicked!");
        // UI click SFX
        var ui = FindObjectOfType<UIManager>(true);
        if (ui != null) ui.PlayClick();
        CloseQuestPanel();
    }

    private void CloseQuestPanel()
    {
        if (panel_QuestgiverUI != null)
        {
            panel_QuestgiverUI.SetActive(false);
        }

        // Restore UI state - unlock cursor and restore player movement
        UIManager uiManager = null;
        if (GetComponent<UIManager>() != null)
        {
            uiManager = GetComponent<UIManager>();
        }
        else if (GetComponentInParent<UIManager>() != null)
        {
            uiManager = GetComponentInParent<UIManager>();
        }
        else
        {
#if UNITY_2023_1_OR_NEWER
            uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
#else
            uiManager = FindObjectOfType<UIManager>(true);
#endif
        }

        if (uiManager != null)
        {
            uiManager.EnableUI(false);
        }
        else
        {
            // Fallback: manually restore cursor state if UIManager not found
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            // Try to restore player state
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.SetState(PlayerController.PlayerState.Playing);
                }
            }
        }
    }
}
