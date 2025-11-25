using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    public int waitPeriod = 5;
    private int id = 0;

    //Add: new entry message pop up.

    public void UnlockEntry()
    {
        waitPeriod--;

        if (waitPeriod >= 0) return;

        JournalEntryData entry = journalEntries.Find(e => e.id == id);

        if (entry == null)
        {
            Debug.LogWarning("No Entry Available");
            return;
        }

        //The Rest

        entry.unlocked = true;
        id++;   //Move to next entry next unlock

        CreateEntry(entry);

        //NotifyPlayer();
    }

    private void NotifyPlayer()
    {
        throw new NotImplementedException();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            UnlockEntry();
            
        }
    }


    private void CreateEntry(JournalEntryData entry)
    {
        GameObject go = Instantiate(journalEntryPrefab, contentParent);
        JournalEntryUI ui = go.GetComponent<JournalEntryUI>();

        if(ui != null)
        {
            ui.Setup(entry.title, entry.body);
            
            // Force layout update
            //LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentParent);

            Debug.Log("Added Entry");
        }
        else
        {
            Debug.LogWarning("Journal entry prefab is missing JournalEntryUI component!");
        }
        
    }

    public void ToggleJournal()
    {
        if(journal.activeSelf == true)
        {
            journal.SetActive(false);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            journal.SetActive(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void CloseJournal()
    {
        journal.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OpenJournal()
    {
        journal.SetActive(true);
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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
