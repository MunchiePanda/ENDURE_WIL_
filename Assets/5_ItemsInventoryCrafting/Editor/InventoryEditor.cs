using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for the Inventory class. Made by Cursor.
/// </summary>

[CustomEditor(typeof(Inventory))]
public class InventoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var inventory = (Inventory)target;

#if UNITY_EDITOR
        if (inventory.DebugItems != null && inventory.DebugItems.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Items (Editor View)", EditorStyles.boldLabel);

            foreach (var kvp in inventory.DebugItems)
            {
                var item = kvp.Key;
                int qty = kvp.Value;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(item ? item.name : "<null>", item, typeof(ItemBase), false);
                    EditorGUILayout.LabelField($"x{qty}", GUILayout.Width(50));
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Inventory is empty.", MessageType.Info);
        }
#endif
    }
}


