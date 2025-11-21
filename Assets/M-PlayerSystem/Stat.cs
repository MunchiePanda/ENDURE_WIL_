using UnityEngine;

namespace ENDURE
{
    // This is like a special box that holds important numbers for our player, like health or energy!
    [System.Serializable] // This makes it easy to see and change these numbers in Unity!
    public struct Stat
    {
        public float max;         // The biggest this number can ever be (like your maximum health)
        public float min;         // The smallest this number can ever be (like 0 health)
        public float current;     // What the number is right now (your current health)
        public float statModifier; // A special number that makes changes bigger or smaller
        public bool isHidden;      // A secret switch! If true, we don't show this number on the screen.

        // This makes the number go down, like when you get hurt!
        public void ReduceStat(float reduction)
        {
            float modifier = Mathf.Approximately(statModifier, 0f) ? 1f : statModifier;
            // We make sure the reduction respects the statModifier
            current -= reduction * modifier;
            // The number can't go below its smallest value
            if (current < min)
            {
                current = min;
                OnMinReached(); // Something special happens when the number hits its smallest!
            }
        }

        // This makes the number go up, like when you heal or eat food!
        public void IncreaseStat(float increase)
        {
            current += increase;
            // The number can't go above its biggest value
            if (current > max)
            {
                current = max;
            }
        }

        // This is a special moment when the number reaches its smallest point.
        // We don't do anything here for now, but we could make the player faint or something!
        private void OnMinReached()
        {
            Debug.Log($"Stat {current} reached its minimum: {min}!");
            // For example, if health reaches min, the player might "Die()"
            // But we'll handle that in the CharacterManager for now.
        }
    }
}
