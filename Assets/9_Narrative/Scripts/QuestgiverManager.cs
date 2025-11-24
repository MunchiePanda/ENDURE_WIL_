using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        questDescription.text = GetQuestDescription();
        btn_AcceptQuest.onClick.AddListener(OnAcceptQuestButtonClicked);
        btn_RejectQuest.onClick.AddListener(OnRejectQuestButtonClicked);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    string GetQuestDescription()
    {
        string objectivesText = quest.questName + " \n";
        foreach (QuestObjective objective in quest.questObjectives)
        {
            objectivesText += "\n " + objective.GetQuestObjectiveText();
        }

        return objectivesText;
    }

    void OnAcceptQuestButtonClicked()
    {
        QuestManager questManager = GetComponent<QuestManager>();
        if (questManager == null ) questManager = GetComponentInParent<QuestManager>();
        if (questManager == null)
        {
            Debug.LogWarning("QuestgiverManager OnAcceptQuestButtonClicked(): Can't find quest manager.");
            return;
        }

        questManager.AddQuest(quest);
        panel_QuestgiverUI.SetActive(false);    //close
    }

    void OnRejectQuestButtonClicked()
    {
        panel_QuestgiverUI.SetActive(false);    //close
    }
}
