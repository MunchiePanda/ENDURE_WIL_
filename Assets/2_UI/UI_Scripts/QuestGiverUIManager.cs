using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        panel_QuestgiverUI.SetActive(false);
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
            //objectivesText += "\n " + objective.GetQuestObjectiveText();      //this fucked up the currentQuantity, so we change it and do it manually
            objectivesText += "\n " + objective.item.itemName + " x" + objective.quantity.ToString();
        }

        return objectivesText;
    }

    void OnAcceptQuestButtonClicked()
    {
        QuestManager questManager = GetComponent<QuestManager>();
        if (questManager == null) questManager = GetComponentInParent<QuestManager>();
        if (questManager == null)
        {
            Debug.LogWarning("QuestgiverManager OnAcceptQuestButtonClicked(): Can't find quest manager.");
            return;
        }

        questManager.AddQuest(quest);
        panel_QuestgiverUI.SetActive(false);    //close
        UIManager uiManager = GetComponent<UIManager>();
        if (uiManager == null) uiManager = GetComponentInParent<UIManager>();
        uiManager.EnableUI(false);
    }

    void OnRejectQuestButtonClicked()
    {
        panel_QuestgiverUI.SetActive(false);    //close
        UIManager uiManager = GetComponent<UIManager>();
        if(uiManager == null) uiManager = GetComponentInParent<UIManager>();
        uiManager.EnableUI(false);
    }
}
