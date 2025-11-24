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
        if (questDescription != null)
            questDescription.text = quest != null ? GetQuestDescription() : string.Empty;
        btn_AcceptQuest.onClick.AddListener(OnAcceptQuestButtonClicked);
        btn_RejectQuest.onClick.AddListener(OnRejectQuestButtonClicked);
    }

    // Update is called once per frame
    void Update()
    {

    }

    string GetQuestDescription()
    {
        if (quest == null) return string.Empty;
        string objectivesText = quest.questName + " \n";
        foreach (QuestObjective objective in quest.questObjectives)
        {
            //objectivesText += "\n " + objective.GetQuestObjectiveText();      //this fucked up the currentQuantity, so we change it and do it manually
            objectivesText += "\n " + objective.item.itemName + " x" + objective.quantity.ToString();
        }

        return objectivesText;
    }

    public void SetQuest(QuestBase newQuest)
    {
        quest = newQuest;
        if (questDescription != null)
            questDescription.text = GetQuestDescription();
    }

    void OnEnable()
    {
        // Pick up a pending quest (for instantiate-on-open flow)
        QuestBase pending;
        if (quest == null && QuestgiverNPCBinder.TryConsumePending(out pending))
        {
            SetQuest(pending);
        }
    }

    void OnAcceptQuestButtonClicked()
    {
        QuestManager questManager = QuestManager.Instance;
        if (questManager == null) questManager = GetComponent<QuestManager>();
        if (questManager == null) questManager = GetComponentInParent<QuestManager>();
        if (questManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            questManager = Object.FindFirstObjectByType<QuestManager>(FindObjectsInactive.Include);
#else
            questManager = FindObjectOfType<QuestManager>(true);
#endif
        }
        if (questManager == null)
        {
            Debug.LogWarning("QuestgiverManager OnAcceptQuestButtonClicked(): Can't find quest manager.");
            return;
        }

        questManager.AddQuest(quest);
        panel_QuestgiverUI.SetActive(false);    //close
        UIManager uiManager = GetComponent<UIManager>();
        if (uiManager == null) uiManager = GetComponentInParent<UIManager>();
        if (uiManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
#else
            uiManager = FindObjectOfType<UIManager>(true);
#endif
        }
        if (uiManager != null) uiManager.EnableUI(false);
    }

    void OnRejectQuestButtonClicked()
    {
        panel_QuestgiverUI.SetActive(false);    //close
        UIManager uiManager = GetComponent<UIManager>();
        if(uiManager == null) uiManager = GetComponentInParent<UIManager>();
#if UNITY_2023_1_OR_NEWER
        if (uiManager == null) uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
#else
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>(true);
#endif
        if (uiManager != null) uiManager.EnableUI(false);
    }
}
