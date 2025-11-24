using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class JournalEntryManager : MonoBehaviour
{
    public static JournalEntryManager Instance;

    private void Awake()
    {
        // basic singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI")]
    public Transform contentParent;          // ScrollView Content Game Object
    public GameObject journalEntryPrefab;    // Entry prefab
    public GameObject journal;

    [Header("Journal Entries")]
    public List<JournalEntryData> journalEntries;
    private int id = 0;

    //Add: new entry message pop up.

    public void UnlockEntry()
    {
        JournalEntryData entry = journalEntries.Find(e => e.id == id);

        if (entry == null)
        {
            Debug.LogWarning("No Entry Available");
            return;
        }

        entry.unlocked = true;

        id++;   //Move to next entry next unlock

        CreateEntry(entry);
    }

    private void CreateEntry(JournalEntryData entry)
    {
        GameObject go = Instantiate(journalEntryPrefab, contentParent);
        JournalEntryUI ui = go.GetComponent<JournalEntryUI>();

        if(ui != null)
        {
            ui.Setup(entry.title, entry.body);
        }
        else
        {
            Debug.LogWarning("Journal entry prefab is missing JournalEntryUI component!");
        }
        
    }

    public void CloseJournal()
    {
        journal.SetActive(false);
    }


}

[System.Serializable]
public class JournalEntryData
{
    public int id;
    public string title;
    public string body;

    [HideInInspector]
    public bool unlocked;
}
