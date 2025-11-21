using UnityEngine;

namespace ENDURE
{
    // This is like the main brain for any character in our game (like monsters or our player)!
    // It helps them know how much health, energy, and speed they have, and how to get hurt.
    public class CharacterManager : MonoBehaviour, IDamageable // It's a special Unity script and follows the IDamageable rules!
    {
        // These are the character's main numbers!
        [Header("Character Stats - How strong, fast, and healthy they are!")]
        [SerializeField] protected Stat healthField; // How much health they have!
        [SerializeField] protected Stat staminaField; // How much energy they have for running or jumping!
        [SerializeField] protected Stat speedField;   // How fast they can move!

        // The rules say we need a health stat, so here it is!
        public Stat Health { get => healthField; set => healthField = value; }

        // The rules also say we need a stamina stat, so here it is!
        public Stat Stamina { get => staminaField; set => staminaField = value; }

        // And a speed stat, too!
        public Stat Speed { get => speedField; set => speedField = value; }

        // This is what happens when the character gets hurt!
        public virtual void TakeDamage(float damage) // "virtual" means other brains (like PlayerManager) can change this rule!
        {
            healthField.ReduceStat(damage); // Make health go down!
            Debug.Log($"{gameObject.name} took {damage} damage. Current Health: {healthField.current}");
            if (healthField.current <= healthField.min)
            {
                Die(); // Oh no, if health is too low, they fall down!
            }
        }

        // This is what happens when the character's health goes all the way down!
        public virtual void Die() // "virtual" again, so other brains can change how they fall down!
        {
            Debug.Log($"{gameObject.name} has died!");
            Destroy(gameObject); // Make the character disappear from the game!
        }

        // This special method helps us apply power-up effects to our stats!
        protected virtual void ApplyAttributeEffect(Attribute attribute)
        {
            switch (attribute.type)
            {
                case AttributeType.Vitality:
                    healthField.max += 10 * attribute.value;
                    healthField.current = healthField.max; // Fill up health when max increases
                    Debug.Log($"Applied Vitality. {gameObject.name} Max Health: {healthField.max}");
                    break;
                case AttributeType.Agility:
                    speedField.max += speedField.max * (0.005f * attribute.value); // Increases max Speed by 0.5% every 1
                    speedField.current = speedField.max; // Adjust current speed if needed, or leave to controller
                    Debug.Log($"Applied Agility. {gameObject.name} Max Speed: {speedField.max}");
                    break;
                case AttributeType.SystemResistance:
                    // For CharacterManager, SystemExposure isn't directly here, so we'll just log.
                    // PlayerManager will handle this specifically.
                    Debug.Log($"Applied SystemResistance. This affects SystemExposure. Value: {attribute.value}");
                    break;
                case AttributeType.Fitness:
                    staminaField.max += 10 * attribute.value;
                    staminaField.current = staminaField.max; // Fill up stamina when max increases
                    Debug.Log($"Applied Fitness. {gameObject.name} Max Stamina: {staminaField.max}");
                    break;
                case AttributeType.Metabolism:
                    // For CharacterManager, Hunger isn't directly here, so we'll just log.
                    // PlayerManager will handle this specifically.
                    Debug.Log($"Applied Metabolism. This affects hunger replenishment. Value: {attribute.value}");
                    break;
                default:
                    Debug.LogWarning($"Unknown AttributeType: {attribute.type}");
                    break;
            }
        }

        public void DrainStamina(float amount)
        {
            staminaField.ReduceStat(amount);
        }

        public void RegainStamina(float amount)
        {
            staminaField.IncreaseStat(amount);
        }
    }
}
