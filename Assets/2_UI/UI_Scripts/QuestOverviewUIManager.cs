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

    private bool isEnabled;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(questManager == null) questManager = GetComponentInParent<QuestManager>();
        if (questManager == null) Debug.Log("QuestOverviewUIManager Start() QuestManager is null.");
        btn_toggleQuesOverview.onClick.AddListener(ToggleQuestOverviewUI);  //bind button click

        EnableQuestOverviewUI(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isEnabled || questManager == null) return;
        if (questManager.currentQuest == null)
        {
            // Clear UI when no active quest
            if (txt_questObjectives != null) txt_questObjectives.text = string.Empty;
            return;
        }

        // TODO: Replace with event-driven refresh from Inventory when items change
        questManager.UpdateCurrentQuest();
        UpdateQuestOverview();
    }

    public void EnableQuestOverviewUI(bool enable)
    {
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
    }

    public void ToggleQuestOverviewUI()
    {
        EnableQuestOverviewUI(!isEnabled);  //inverse enabaled
    }
}
