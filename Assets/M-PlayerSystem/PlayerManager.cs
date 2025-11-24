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

        [Header("System Exposure Settings")]
        [Tooltip("Enable the ambient exposure timer (acts as a soft dungeon timer).")]
        [SerializeField] private bool systemExposureEnabled = true;
        [Tooltip("How many seconds it takes for system exposure to drain from full to zero.")]
        [SerializeField] private float exposureDurationSeconds = 180f;
        [Tooltip("Damage per second applied to health once exposure hits zero.")]
        [SerializeField] private float exposureDamagePerSecond = 5f;
        [Tooltip("Refill system exposure to max when the level starts.")]
        [SerializeField] private bool resetExposureOnStart = true;

        // We make these public so other scripts can see them
        public Stat Hunger { get => hungerField; set => hungerField = value; }
        public Stat SystemExposure { get => systemExposureField; set => systemExposureField = value; }

        private float exposureDrainPerSecond = 0f;

        // We want to do special things when applying attributes, so we change the rule from CharacterManager!


        protected override void ApplyAttributeEffect(Attribute attribute)
        {
            // First, let the main CharacterManager brain do its part for general stats.
            base.ApplyAttributeEffect(attribute);

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

        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
        }

        private void Start()
        {
            InitializeSystemExposure();
        }

        private void Update()
        {
            if (!systemExposureEnabled) return;
            HandleSystemExposure(Time.deltaTime);
        }

        private void InitializeSystemExposure()
        {
            exposureDrainPerSecond = 0f;
            if (!systemExposureEnabled)
            {
                return;
            }

            if (resetExposureOnStart)
            {
                systemExposureField.current = systemExposureField.max;
            }

            if (exposureDurationSeconds > 0f && systemExposureField.max > 0f)
            {
                exposureDrainPerSecond = systemExposureField.max / exposureDurationSeconds;
            }
        }

        private void HandleSystemExposure(float deltaTime)
        {
            if (systemExposureField.current > systemExposureField.min && exposureDrainPerSecond > 0f)
            {
                float drainAmount = exposureDrainPerSecond * deltaTime;
                systemExposureField.ReduceStat(drainAmount);
            }
            else if (systemExposureField.current <= systemExposureField.min + 0.001f)
            {
                float damage = Mathf.Max(0f, exposureDamagePerSecond) * deltaTime;
                if (damage > 0f)
                {
                    TakeDamage(damage);
                }
            }
        }

        public bool ApplyItemStat(ItemStatTarget target, float amount)
        {
            switch (target)
            {
                case ItemStatTarget.Health:
                    AdjustStat(ref healthField, amount);
                    return true;
                case ItemStatTarget.Stamina:
                    AdjustStat(ref staminaField, amount);
                    return true;
                case ItemStatTarget.Hunger:
                    AdjustStat(ref hungerField, amount);
                    return true;
                case ItemStatTarget.SystemExposure:
                    AdjustStat(ref systemExposureField, amount);
                    return true;
                default:
                    return false;
            }
        }

        private void AdjustStat(ref Stat stat, float amount)
        {
            if (amount >= 0f)
            {
                stat.IncreaseStat(amount);
            }
            else
            {
                stat.ReduceStat(Mathf.Abs(amount));
            }
        }
    }
}
