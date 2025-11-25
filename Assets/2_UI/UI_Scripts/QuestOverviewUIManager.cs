using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestOverviewUIManager : MonoBehaviour
{
    public GameObject panel_questOverviewUI;    //The actual questOverview UI
    public GameObject panel_questOverview;      //The panel with the text
    public TMP_Text txt_questObjectives;
    public Button btn_toggleQuesOverview;

    public QuestManager questManager;
    private UIManager uiManager;

    [Header("Quest Completion Visuals")]
    [Tooltip("Text color when quest requirements are met (ready to complete).")]
    public Color completedQuestColor = new Color(0.5f, 0f, 0.5f, 1f); // Purple by default

    private bool isEnabled;
    private Color originalTextColor;
    private bool hasPlayedCompleteSound = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Resolve UIManager reference
        uiManager = GetComponentInParent<UIManager>();
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>(true);
        }

        // Try to find QuestManager using multiple methods
        if (questManager == null)
        {
            if (QuestManager.TryGet(out questManager))
            {
                // Found via singleton or FindObjectOfType
            }
            else
            {
                questManager = GetComponentInParent<QuestManager>();
                if (questManager == null) questManager = FindObjectOfType<QuestManager>();
            }
        }

        if (questManager == null)
        {
            Debug.LogWarning("QuestOverviewUIManager Start(): QuestManager is null. Make sure QuestManager is attached to the player or exists in the scene.");
        }

        if (btn_toggleQuesOverview != null)
        {
            btn_toggleQuesOverview.onClick.AddListener(() => { if (uiManager != null) uiManager.PlayClick(); });
            btn_toggleQuesOverview.onClick.AddListener(ToggleQuestOverviewUI);  //bind button click
        }

        // Store original text color
        if (txt_questObjectives != null)
        {
            originalTextColor = txt_questObjectives.color;
        }

        EnableQuestOverviewUI(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isEnabled || questManager == null) return;
        if (questManager.currentQuest == null)
        {
            // Clear UI when no active quest
            if (txt_questObjectives != null)
            {
                txt_questObjectives.text = string.Empty;
                txt_questObjectives.color = originalTextColor; // Reset color
            }
            hasPlayedCompleteSound = false; // Reset sound flag
            return;
        }

        // TODO: Replace with event-driven refresh from Inventory when items change
        questManager.UpdateCurrentQuest();
        UpdateQuestOverview();
    }

    public void EnableQuestOverviewUI(bool enable)
    {
        if (panel_questOverview != null && panel_questOverview.activeSelf != enable)
        {
            if (enable)
            {
                if (uiManager != null) uiManager.PlayPanelOpen();
            }
            else
            {
                if (uiManager != null) uiManager.PlayPanelClose();
            }
        }
        isEnabled = enable;
        panel_questOverview.SetActive(enable);

        //TODO: change toggle button's image based on if the panel is collaped or not
    }

    void UpdateQuestOverview()
    {
        if (questManager == null || questManager.currentQuest == null || txt_questObjectives == null)
            return;

        string objectivesText = questManager.currentQuest.quest.questName + " ";
        foreach (QuestObjective objective in questManager.currentQuest.quest.questObjectives)
        {
            objectivesText += "\n " + objective.GetQuestObjectiveText();
        }

        txt_questObjectives.text = objectivesText;

        // Check if quest is complete and update visuals/sound
        if (questManager.currentQuest.isQuestComplete)
        {
            // Change text color to purple when quest is ready to complete
            txt_questObjectives.color = completedQuestColor;

            // Play completion sound once when quest becomes complete
            if (!hasPlayedCompleteSound && uiManager != null)
            {
                uiManager.PlayQuestCompleteSound();
                hasPlayedCompleteSound = true;
            }
        }
        else
        {
            // Reset to original color and sound flag when quest is not complete
            txt_questObjectives.color = originalTextColor;
            hasPlayedCompleteSound = false;
        }
    }

    public void ToggleQuestOverviewUI()
    {
        if (uiManager != null) uiManager.PlayClick();
        EnableQuestOverviewUI(!isEnabled);  //inverse enabaled
    }
}
