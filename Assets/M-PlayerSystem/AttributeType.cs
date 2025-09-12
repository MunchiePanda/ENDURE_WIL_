using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ENDURE
{
    // This is like a special label that tells us what kind of power-up an Attribute is!
    public enum AttributeType
    {
        Vitality,         // Makes you super healthy!
        Agility,          // Makes you super fast!
        SystemResistance, // Helps you resist bad stuff like poison!
        Fitness,          // Makes you super strong and have lots of energy!
        Metabolism        // Helps you get full faster when you eat!
        // You can add more special power-up types here later!
    }
}
