using TMPro;
using UnityEngine;

public class JournalEntryUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    public void Setup(string title, string body)
    {
        titleText.text = title;
        bodyText.text = body;
    }

}
