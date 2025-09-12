using UnityEngine;

namespace ENDURE
{
    // This is like a special power-up card!
    // It has a value (how strong the power-up is) and a type (what kind of power-up it is).
    [System.Serializable] // This makes it easy to see and change these power-up cards in Unity!
    public struct Attribute
    {
        public int value;             // How much of this power-up we have (e.g., Vitality 5)
        public AttributeType type;    // What kind of power-up this is (e.g., Vitality, Agility)

        // Note: The ApplyAttribute method will be handled by CharacterManager/PlayerManager
        // because structs are value types and cannot directly modify other live Stat structs.
        // Instead, the managers will read this Attribute and apply its effects to their Stats.

        // Example of how it *would* work if it could directly modify a Stat (for understanding):
        // public void ApplyAttribute(ref Stat targetStat)
        // {
        //     switch (type)
        //     {
        //         case AttributeType.Vitality:
        //             targetStat.max += 10 * value;
        //             Debug.Log($"Applied Vitality. New Max Health: {targetStat.max}");
        //             break;
        //         // ... other cases
        //     }
        // }
    }
}
