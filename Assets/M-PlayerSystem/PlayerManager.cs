using UnityEngine;

namespace ENDURE
{
    // This is the super special brain just for OUR player!
    // It knows everything the CharacterManager knows, plus extra stuff like hunger and how safe they are from bad things.
    public class PlayerManager : CharacterManager // It's a CharacterManager, but even more special!
    {
        // Our player has extra numbers to keep track of!
        [Header("Player Specific Stats - Extra things only our player cares about!")]
        [SerializeField] private Stat hungerField;        // How hungry our player is!
        [SerializeField] private Stat systemExposureField; // How much danger our player is in from the environment!

        // We make these public so other scripts can see them
        public Stat Hunger { get => hungerField; set => hungerField = value; }
        public Stat SystemExposure { get => systemExposureField; set => systemExposureField = value; }

        // We want to do special things when applying attributes, so we change the rule from CharacterManager!
        protected override void applyAttributeEffect(Attribute attribute)
        {
            // First, let the main CharacterManager brain do its part for general stats.
            base.applyAttributeEffect(attribute);

            // Now, we do our player-specific power-up effects!
            switch (attribute.type)
            {
                case AttributeType.SystemResistance:
                    systemExposureField.max += 10 * attribute.value;
                    systemExposureField.current = systemExposureField.max; // Fill up exposure when max increases (or set to appropriate start)
                    Debug.Log($"Applied SystemResistance. Player Max System Exposure: {systemExposureField.max}");
                    break;
                case AttributeType.Metabolism:
                    // Placeholder: This value will adjust how much hunger is replenished when eating.
                    // For example, an eating function could use this value: hungerField.IncreaseStat(baseHungerReplenish * (1 + attribute.value * 0.1f));
                    Debug.Log($"Applied Metabolism. This affects hunger replenishment. Value: {attribute.value}");
                    break;
                // Other attribute types are handled by the base CharacterManager
            }
        }
    }
}
