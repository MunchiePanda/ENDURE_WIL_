using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ENDURE;

public class QuestgiverManager : MonoBehaviour
{
    public QuestBase quest;
    public GameObject panel_QuestgiverUI;
    public TMP_Text questDescription;
    public Button btn_AcceptQuest;
    public Button btn_RejectQuest;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Auto-find UI components if not assigned
        FindUIComponents();

        // Clear quest reference on start - quests should be set dynamically via SetQuest()
        // This prevents stale quest data from inspector assignments
        if (quest != null)
        {
            Debug.Log($"QuestgiverManager Start: Clearing pre-assigned quest '{quest.questName}' from inspector. Quest will be set dynamically.");
        }
        quest = null;

		if (questDescription != null)
		{
			questDescription.text = string.Empty;
		}
		
		// Setup button listeners with null checks
		SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        // Check if QuestGiverUIManager exists - if so, let it handle buttons to prevent duplicate listeners
        QuestGiverUIManager questGiverUI = FindObjectOfType<QuestGiverUIManager>(true);
        if (questGiverUI != null)
        {
            Debug.Log("QuestgiverManager: QuestGiverUIManager found. Skipping button listener setup to prevent duplicates.");
            return;
        }

        // Remove existing listeners first to prevent duplicates
        if (btn_AcceptQuest != null)
        {
            btn_AcceptQuest.onClick.RemoveListener(OnAcceptQuestButtonClicked);
            btn_AcceptQuest.onClick.AddListener(OnAcceptQuestButtonClicked);
            Debug.Log("QuestgiverManager: Accept button listener added.");
        }
        else
        {
            Debug.LogWarning("QuestgiverManager: btn_AcceptQuest is null! Button will not work. Make sure it's assigned or auto-found.");
        }

        if (btn_RejectQuest != null)
        {
            btn_RejectQuest.onClick.RemoveListener(OnRejectQuestButtonClicked);
            btn_RejectQuest.onClick.AddListener(OnRejectQuestButtonClicked);
            Debug.Log("QuestgiverManager: Reject button listener added.");
        }
        else
        {
            Debug.LogWarning("QuestgiverManager: btn_RejectQuest is null! Button will not work. Make sure it's assigned or auto-found.");
        }
    }

    private void FindUIComponents()
    {
        // Try to find from QuestGiverUIManager first (if it exists and has references)
        QuestGiverUIManager questGiverUI = FindObjectOfType<QuestGiverUIManager>(true);
        if (questGiverUI != null)
        {
            if (panel_QuestgiverUI == null && questGiverUI.panel_QuestgiverUI != null)
            {
                panel_QuestgiverUI = questGiverUI.panel_QuestgiverUI;
            }
            if (questDescription == null && questGiverUI.questDescription != null)
            {
                questDescription = questGiverUI.questDescription;
            }
            if (btn_AcceptQuest == null && questGiverUI.btn_AcceptQuest != null)
            {
                btn_AcceptQuest = questGiverUI.btn_AcceptQuest;
            }
            if (btn_RejectQuest == null && questGiverUI.btn_RejectQuest != null)
            {
                btn_RejectQuest = questGiverUI.btn_RejectQuest;
            }
        }

        // Try to find from UIManager
        UIManager uiManager = FindObjectOfType<UIManager>(true);
        if (uiManager != null)
        {
            if (panel_QuestgiverUI == null && uiManager.panel_QuestgiverUI != null)
            {
                panel_QuestgiverUI = uiManager.panel_QuestgiverUI;
            }

            // If we have the panel, try to find text and buttons inside it
            if (panel_QuestgiverUI != null)
            {
                if (questDescription == null)
                {
                    questDescription = panel_QuestgiverUI.GetComponentInChildren<TMP_Text>(true);
                }
                if (btn_AcceptQuest == null)
                {
                    // Look for button with "Accept" in name
                    Button[] buttons = panel_QuestgiverUI.GetComponentsInChildren<Button>(true);
                    foreach (Button btn in buttons)
                    {
                        if (btn.name.ToLower().Contains("accept"))
                        {
                            btn_AcceptQuest = btn;
                            break;
                        }
                    }
                }
                if (btn_RejectQuest == null)
                {
                    // Look for button with "Reject" in name
                    Button[] buttons = panel_QuestgiverUI.GetComponentsInChildren<Button>(true);
                    foreach (Button btn in buttons)
                    {
                        if (btn.name.ToLower().Contains("reject"))
                        {
                            btn_RejectQuest = btn;
                            break;
                        }
                    }
                }
            }
        }

        // Log warnings for missing components
        if (panel_QuestgiverUI == null)
        {
            Debug.LogWarning("QuestgiverManager: Could not find panel_QuestgiverUI. Make sure it's assigned or exists in the scene.");
        }
        if (questDescription == null)
        {
            Debug.LogWarning("QuestgiverManager: Could not find questDescription TMP_Text. Make sure it's assigned or exists in panel_QuestgiverUI.");
        }
        if (btn_AcceptQuest == null)
        {
            Debug.LogWarning("QuestgiverManager: Could not find btn_AcceptQuest. Make sure it's assigned or exists in panel_QuestgiverUI.");
        }
        if (btn_RejectQuest == null)
        {
            Debug.LogWarning("QuestgiverManager: Could not find btn_RejectQuest. Make sure it's assigned or exists in panel_QuestgiverUI.");
        }
    }

	void OnEnable()
	{
		// Pick up a pending quest from binder if available
		QuestBase pending;
		if (quest == null && QuestgiverNPCBinder.TryConsumePending(out pending))
		{
			SetQuest(pending);
		}
		// Always refresh description when panel is enabled (important for UI panel reuse)
		if (questDescription != null && quest != null)
		{
			questDescription.text = GetQuestDescription();
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

    void OnAcceptQuestButtonClicked()
    {
        Debug.Log("QuestgiverManager: Accept button clicked!");
        // UI click SFX
        var ui = FindObjectOfType<UIManager>(true);
        if (ui != null) ui.PlayClick();
        
        if (quest == null)
        {
            Debug.LogWarning("QuestgiverManager OnAcceptQuestButtonClicked(): No quest assigned!");
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
            if (questManager == null) questManager = FindObjectOfType<QuestManager>();
        }

        if (questManager == null)
        {
            Debug.LogWarning("QuestgiverManager OnAcceptQuestButtonClicked(): Can't find quest manager. Make sure QuestManager is attached to the player or exists in the scene.");
            CloseQuestPanel();
            return;
        }

        // Store the quest to add before clearing
        QuestBase questToAdd = quest;
        
        // Clear quest reference immediately to prevent showing stale data
        quest = null;
        
        // Close panel first
        CloseQuestPanel();
        
        // Add quest after panel is closed to ensure QuestOverviewUI updates correctly
        questManager.AddQuest(questToAdd);
        
        Debug.Log($"QuestgiverManager: Accepted and added quest '{questToAdd?.questName}'");
    }

    void OnRejectQuestButtonClicked()
    {
        Debug.Log("QuestgiverManager: Reject button clicked!");
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
        UIManager uiManager = FindObjectOfType<UIManager>(true);
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

    // Allow per-NPC assignment of a quest at open-time
    public void SetQuest(QuestBase newQuest)
    {
        if (newQuest == null)
        {
            Debug.LogWarning("QuestgiverManager SetQuest: newQuest is null!");
            quest = null;
            if (questDescription != null)
            {
                questDescription.text = string.Empty;
            }
            return;
        }
        
        quest = newQuest;
        Debug.Log($"QuestgiverManager SetQuest: Setting quest to '{newQuest.questName}' (ID: {newQuest.GetInstanceID()})");
        
        // Always update text immediately when quest is set
        if (questDescription != null)
        {
            questDescription.text = quest != null ? GetQuestDescription() : string.Empty;
        }
    }
}
