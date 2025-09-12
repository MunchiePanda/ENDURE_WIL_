using UnityEngine;

namespace ENDURE
{
    // This is like a special rulebook for anything in our game that can get hurt or defeated!
    public interface IDamageable
    {
        // Every creature that can get hurt needs to have health!
        Stat Health { get; set; } // How much health it has (can be changed and read)

        // This is what happens when something takes a hit!
        void TakeDamage(float damage); // How much ouchie it takes

        // This is what happens when something's health goes all the way down!
        void Die(); // They fall down and stop playing!
    }
}
