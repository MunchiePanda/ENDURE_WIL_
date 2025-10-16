using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

///<summary>
/// Custom editor for the CraftingRecipieBase class. Made by Cursor.
/// </summary>

[CustomEditor(typeof(CraftingRecipieBase))]
public class CraftingRecipieBaseEditor : Editor
{
    private SerializedProperty craftedItemProp;
    private SerializedProperty craftQuantityProp;
    private SerializedProperty requiredIngredientsProp;

    private ReorderableList ingredientsList;

    private void OnEnable()
    {
        craftedItemProp = serializedObject.FindProperty("craftedItem");
        craftQuantityProp = serializedObject.FindProperty("craftQuantity");
        requiredIngredientsProp = serializedObject.FindProperty("requiredIngredients");

        ingredientsList = new ReorderableList(serializedObject, requiredIngredientsProp, true, true, true, true);
        ingredientsList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;

        ingredientsList.drawHeaderCallback = rect =>
        {
            const float quantityWidth = 80f;
            Rect left = new Rect(rect.x, rect.y, rect.width - quantityWidth - 6f, rect.height);
            Rect right = new Rect(rect.x + rect.width - quantityWidth, rect.y, quantityWidth, rect.height);

            EditorGUI.LabelField(left, "Required Ingredients (Item)");
            EditorGUI.LabelField(right, "Quantity");
        };

        ingredientsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty element = requiredIngredientsProp.GetArrayElementAtIndex(index);
            SerializedProperty itemProp = element.FindPropertyRelative("item");
            SerializedProperty quantityProp = element.FindPropertyRelative("quantity");

            const float padding = 2f;
            rect.y += padding;
            rect.height = EditorGUIUtility.singleLineHeight;

            const float quantityWidth = 80f;
            Rect itemRect = new Rect(rect.x, rect.y, rect.width - quantityWidth - 6f, rect.height);
            Rect qtyRect = new Rect(rect.x + rect.width - quantityWidth, rect.y, quantityWidth, rect.height);

            EditorGUI.ObjectField(itemRect, itemProp, GUIContent.none);

            int newQty = EditorGUI.IntField(qtyRect, quantityProp.intValue);
            if (newQty < 1) newQty = 1;
            quantityProp.intValue = newQty;
        };

        ingredientsList.onAddCallback = list =>
        {
            Undo.RecordObject(target, "Add Ingredient");
            int index = requiredIngredientsProp.arraySize;
            requiredIngredientsProp.arraySize++;
            SerializedProperty newElement = requiredIngredientsProp.GetArrayElementAtIndex(index);
            newElement.FindPropertyRelative("item").objectReferenceValue = null;
            newElement.FindPropertyRelative("quantity").intValue = 1;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        };

        ingredientsList.onRemoveCallback = list =>
        {
            if (list.index >= 0 && list.index < requiredIngredientsProp.arraySize)
            {
                Undo.RecordObject(target, "Remove Ingredient");
                requiredIngredientsProp.DeleteArrayElementAtIndex(list.index);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Recipe Output", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(craftedItemProp);

        EditorGUI.BeginChangeCheck();
        int qty = Mathf.Max(1, EditorGUILayout.IntField("Craft Quantity", craftQuantityProp.intValue));
        if (EditorGUI.EndChangeCheck())
        {
            craftQuantityProp.intValue = qty;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Required Ingredients", EditorStyles.boldLabel);
        ingredientsList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}


