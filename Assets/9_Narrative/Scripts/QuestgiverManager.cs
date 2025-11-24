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
		if (questDescription != null)
		{
			// If no quest is assigned yet (NPC will assign later), show empty text
			questDescription.text = quest != null ? GetQuestDescription() : string.Empty;
		}
		if (btn_AcceptQuest != null) btn_AcceptQuest.onClick.AddListener(OnAcceptQuestButtonClicked);
		if (btn_RejectQuest != null) btn_RejectQuest.onClick.AddListener(OnRejectQuestButtonClicked);
    }

	void OnEnable()
	{
		// Pick up a pending quest from binder if available
		QuestBase pending;
		if (quest == null && QuestgiverNPCBinder.TryConsumePending(out pending))
		{
			SetQuest(pending);
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
		if (quest.questObjectives != null)
		{
			foreach (QuestObjective objective in quest.questObjectives)
			{
				objectivesText += "\n " + objective.GetQuestObjectiveText();
			}
		}
		return objectivesText;
    }

    void OnAcceptQuestButtonClicked()
    {
        QuestManager questManager = QuestManager.Instance;
        if (questManager == null) questManager = GetComponent<QuestManager>();
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

    // Allow per-NPC assignment of a quest at open-time
    public void SetQuest(QuestBase newQuest)
    {
        quest = newQuest;
        if (questDescription != null)
        {
            questDescription.text = GetQuestDescription();
        }
    }
}
